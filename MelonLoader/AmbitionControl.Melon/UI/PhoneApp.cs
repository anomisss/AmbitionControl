using System;
using System.Collections.Generic;
using AmbitionControl.Core;
using AmbitionControl.Game;
using Il2Cpp;
using Il2CppTMPro;
using Il2CppUI.Smartphone;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AmbitionControl.UI
{
    /// <summary>
    /// Ambition Control as an app inside the game's own smartphone (FullMenu), built from the game's
    /// uGUI so the game's UI owns the clicks. In-game testing showed the IMGUI overlay leaks clicks into
    /// the world even with input patches; real uGUI elements block raycasts the way the game expects.
    ///
    /// Verified on build 3233 interop: FullMenu.appsContainer, FullMenu._appButtons, FullMenu.timeLabel,
    /// FullMenu.appNameLabel, AppButton.appTitle (TextLocalizationComponent.SetValue), FullMenuAppButton
    /// Show/HideSelectedIcon, Button.onClick setter, ButtonClickedEvent(), UnityAction op_Implicit(Action).
    /// </summary>
    internal sealed class PhoneApp
    {
        private const string ButtonName = "AmbitionControlAppButton";
        private const string PanelName = "AmbitionControlPanel";
        private const string Title = "Ambition Control";

        private readonly MoneyService money;
        private FullMenu fullMenu;
        private GameObject button;
        private FullMenuAppButton appButton;
        private GameObject panel;
        private TMP_FontAsset font;

        private TextMeshProUGUI balanceText, amountText, previewText, statusText, infoText, blockText;
        // Tabs (index order = button order). New feature groups add a name + page here.
        private static readonly string[] TabNames = { "Money", "Player", "Game options" };
        private const int TabMoney = 0, TabPlayer = 1, TabOptions = 2;
        private readonly GameObject[] pages = new GameObject[TabNames.Length];
        private readonly TextMeshProUGUI[] tabLabels = new TextMeshProUGUI[TabNames.Length];
        private int currentTab;
        private TextMeshProUGUI invincibleLabel, holdHungerLabel;
        private string lastPlayerStatus;
        private readonly TextMeshProUGUI[] optionLabels = new TextMeshProUGUI[GameOptions.All.Length];
        private string lastMoneyStatus, lastToggleStatus, shownStatus;
        private readonly List<KeyValuePair<Button, Func<GameSnapshot, bool>>> gated =
            new List<KeyValuePair<Button, Func<GameSnapshot, bool>>>();
        private GameSnapshot lastSnap;
        private Settings settings;

        private long amount = 100_000L;
        private float nextInjectAttempt;
        private float nextRefresh;
        private int errors;
        private bool disabled;
        private bool loggedLayout;

        internal PhoneApp(MoneyService money) { this.money = money; }

        internal bool PanelVisible => panel != null && panel.activeSelf;

        /// <summary>Drop references after a scene change (scene objects are destroyed with the scene).</summary>
        internal void Reset()
        {
            fullMenu = null;
            button = null;
            appButton = null;
            panel = null;
            gated.Clear();
            nextInjectAttempt = 0f;
        }

        /// <summary>Main-thread step from OnUpdate.</summary>
        internal void Update(GameSnapshot snap, Settings currentSettings)
        {
            if (disabled) return;
            lastSnap = snap;
            settings = currentSettings;
            try
            {
                if (button == null || panel == null || fullMenu == null)
                {
                    if (snap != null && snap.Loaded && Time.unscaledTime >= nextInjectAttempt)
                    {
                        nextInjectAttempt = Time.unscaledTime + 2f;
                        TryInject();
                    }
                    return;
                }

                if (!PanelVisible) return;
                if (OtherAppVisible())
                {
                    Hide();
                    AmbitionControlMod.Log.Msg("Phone app hidden: another phone app was opened.");
                    return;
                }
                if (Time.unscaledTime >= nextRefresh)
                {
                    nextRefresh = Time.unscaledTime + 0.2f;
                    Refresh();
                }
            }
            catch (Exception ex)
            {
                Fail("update", ex);
            }
        }

        private void Fail(string where, Exception ex)
        {
            errors++;
            AmbitionControlMod.Log.Error("Phone app " + where + " error: " + ex.GetBaseException());
            if (errors >= 5)
            {
                disabled = true;
                AmbitionControlMod.Log.Error("Phone app disabled after repeated errors; the F10 overlay still works.");
                try { if (panel != null) panel.SetActive(false); } catch { /* best effort */ }
            }
        }

        // ---------------------------------------------------------------- injection

        private void TryInject()
        {
            fullMenu = Object.FindObjectOfType<FullMenu>();
            if (fullMenu == null)
            {
                foreach (FullMenu fm in Resources.FindObjectsOfTypeAll<FullMenu>())
                    if (fm != null && fm.appsContainer != null) { fullMenu = fm; break; }
            }
            if (fullMenu == null || fullMenu.appsContainer == null) { fullMenu = null; return; }

            FullMenuAppButton source = null;
            var buttons = fullMenu._appButtons;
            if (buttons != null)
                foreach (var kv in buttons)
                    if (kv.Value != null) { source = kv.Value; break; }
            if (source == null) { fullMenu = null; return; }

            Transform bar = source.transform.parent;
            Transform existing = bar.Find(ButtonName);
            button = existing != null ? existing.gameObject : Object.Instantiate(source.gameObject, bar);
            button.name = ButtonName;
            button.transform.SetAsLastSibling();
            button.SetActive(true);

            appButton = button.GetComponent<FullMenuAppButton>();
            if (appButton != null)
            {
                if (appButton.appTitle != null) appButton.appTitle.SetValue(Title);
                appButton.HideSelectedIcon();
            }

            Button uiButton = button.GetComponent<Button>() ?? button.GetComponentInChildren<Button>(true);
            if (uiButton != null)
            {
                // A fresh event drops the cloned app's handlers (including editor-assigned ones).
                uiButton.onClick = new Button.ButtonClickedEvent();
                uiButton.onClick.AddListener((UnityAction)(Action)OnAppButtonClicked);
            }

            font = fullMenu.timeLabel != null ? fullMenu.timeLabel.font : null;
            BuildPanel(fullMenu.appsContainer);

            AmbitionControlMod.Log.Msg("Phone app injected (cloned '" + source.name + "', button component " +
                                       (uiButton != null ? "found" : "NOT found") + ", font " +
                                       (font != null ? font.name : "default") + ").");
        }

        private void OnAppButtonClicked()
        {
            try
            {
                AmbitionControlMod.Log.Msg("Phone app opened.");
                Show();
            }
            catch (Exception ex)
            {
                Fail("open", ex);
            }
        }

        // ---------------------------------------------------------------- show / hide

        private void Show()
        {
            Transform container = fullMenu.appsContainer;
            var hidden = new List<string>();
            for (int i = 0; i < container.childCount; i++)
            {
                GameObject child = container.GetChild(i).gameObject;
                if (child == panel || !child.activeSelf) continue;
                child.SetActive(false);
                hidden.Add(child.name);
            }
            if (!loggedLayout)
            {
                loggedLayout = true;
                AmbitionControlMod.Log.Msg("Phone app: hid app screens [" + string.Join(", ", hidden) + "]");
            }

            var buttons = fullMenu._appButtons;
            if (buttons != null)
                foreach (var kv in buttons)
                    if (kv.Value != null) kv.Value.HideSelectedIcon();
            if (appButton != null) appButton.ShowSelectedIcon();
            if (fullMenu.appNameLabel != null) fullMenu.appNameLabel.SetValue(Title);

            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            nextRefresh = 0f;
            Refresh();
        }

        private void Hide()
        {
            if (panel != null) panel.SetActive(false);
            if (appButton != null) appButton.HideSelectedIcon();
            if (money.CurrentPhase == MoneyService.Phase.Staged) money.Cancel();
        }

        private bool OtherAppVisible()
        {
            Transform container = fullMenu.appsContainer;
            for (int i = 0; i < container.childCount; i++)
            {
                GameObject child = container.GetChild(i).gameObject;
                if (child != panel && child.activeSelf) return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- panel construction

        private static readonly Color Bg = new Color(0.09f, 0.10f, 0.12f, 0.98f);
        private static readonly Color BtnNeutral = new Color(0.22f, 0.24f, 0.28f, 1f);
        private static readonly Color BtnGo = new Color(0.16f, 0.50f, 0.30f, 1f);
        private static readonly Color BtnStop = new Color(0.55f, 0.20f, 0.20f, 1f);
        private static readonly Color TextMain = new Color(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color TextDim = new Color(0.70f, 0.72f, 0.76f, 1f);

        private void BuildPanel(Transform container)
        {
            Transform old = container.Find(PanelName);
            if (old != null) Object.Destroy(old.gameObject);
            gated.Clear();

            panel = new GameObject(PanelName);
            panel.transform.SetParent(container, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image bg = panel.AddComponent<Image>();
            bg.color = Bg;
            bg.raycastTarget = true; // swallow clicks on empty panel space

            const float x0 = 40f;
            const float full = 700f;
            float y = 24f;

            Text(panel.transform, Title + "  " + AmbitionControlMod.VersionLabel, x0, y, full, 44f, 32f, TextMain, TextAlignmentOptions.Left);
            y += 54f;

            // Tabs. Pages are full-size children so their coordinates match the panel's.
            float hw = (full - 8f) / 2f;
            float tabW = (full - (TabNames.Length - 1) * 8f) / TabNames.Length;
            for (int i = 0; i < TabNames.Length; i++)
            {
                int index = i;
                pages[i] = Page(TabNames[i].Replace(" ", "") + "Page");
                tabLabels[i] = MakeButton(panel.transform, TabNames[i], x0 + i * (tabW + 8f), y, tabW, 46f, BtnNeutral,
                    () => ShowTab(index), null);
            }
            y += 58f;
            float contentTop = y;

            Transform page = pages[TabMoney].transform;
            balanceText = Text(page, "", x0, y, full, 36f, 26f, TextMain, TextAlignmentOptions.Left);
            y += 42f;
            amountText = Text(page, "", x0, y, full, 32f, 24f, TextDim, TextAlignmentOptions.Left);
            y += 40f;

            long[] steps = { 1_000L, 10_000L, 100_000L, 1_000_000L, 10_000_000L };
            string[] labels = { "+1K", "+10K", "+100K", "+1M", "+10M" };
            float bw = (full - 5 * 8f) / 6f;
            for (int i = 0; i < steps.Length; i++)
            {
                long step = steps[i];
                MakeButton(page, labels[i], x0 + i * (bw + 8f), y, bw, 46f, BtnNeutral,
                    () => { amount = MoneyCommand.ClampAmount(amount + step); money.ClearStageError(); }, null);
            }
            MakeButton(page, "Clear", x0 + 5 * (bw + 8f), y, bw, 46f, BtnNeutral, () => { amount = 0L; money.ClearStageError(); }, null);
            y += 58f;

            float tw = (full - 2 * 8f) / 3f;
            MakeButton(page, "Stage Add", x0, y, tw, 50f, BtnNeutral,
                () => money.Stage(MoneyAction.Add, amount, lastSnap), s => CanStage(s) && amount > 0);
            MakeButton(page, "Stage Remove", x0 + tw + 8f, y, tw, 50f, BtnNeutral,
                () => money.Stage(MoneyAction.Remove, amount, lastSnap), s => CanStage(s) && amount > 0);
            MakeButton(page, "Stage Set", x0 + 2 * (tw + 8f), y, tw, 50f, BtnNeutral,
                () => money.Stage(MoneyAction.Set, amount, lastSnap), CanStage);
            y += 62f;

            previewText = Text(page, "", x0, y, full, 36f, 22f, TextMain, TextAlignmentOptions.Left);
            y += 42f;
            MakeButton(page, "Confirm change", x0, y, hw, 54f, BtnGo,
                () => money.RequestConfirm(), s => money.Staged != null && Block(s) == null);
            MakeButton(page, "Cancel", x0 + hw + 8f, y, hw, 54f, BtnStop,
                () => money.Cancel(), s => money.Staged != null);
            y += 66f;
            MakeButton(page, "Undo last Ambition Control change", x0, y, full, 50f, BtnNeutral,
                () => money.RequestUndo(), s => Block(s) == null && money.CanUndo(s));
            y += 62f;
            float sharedTop = y;

            // Player page: need refills, invincibility, experimental hunger hold.
            float py = contentTop;
            Transform ppage = pages[TabPlayer].transform;
            Func<GameSnapshot, bool> playerOk = s => settings != null && PlayerTools.BlockReason(s, settings) == null;
            MakeButton(ppage, "Refill energy", x0, py, hw, 50f, BtnNeutral,
                () => PlayerTools.Refill(Need.Energy, lastSnap, settings), playerOk);
            MakeButton(ppage, "Refill hunger", x0 + hw + 8f, py, hw, 50f, BtnNeutral,
                () => PlayerTools.Refill(Need.Hunger, lastSnap, settings), playerOk);
            py += 62f;
            MakeButton(ppage, "Refill happiness", x0, py, hw, 50f, BtnNeutral,
                () => PlayerTools.Refill(Need.Happiness, lastSnap, settings), playerOk);
            MakeButton(ppage, "Refill all needs", x0 + hw + 8f, py, hw, 50f, BtnGo,
                () => PlayerTools.RefillAll(lastSnap, settings), playerOk);
            py += 62f;
            invincibleLabel = MakeButton(ppage, "Invincibility", x0, py, hw, 50f, BtnNeutral,
                () => PlayerTools.ToggleInvincibility(lastSnap, settings), playerOk);
            holdHungerLabel = MakeButton(ppage, "Hold hunger full", x0 + hw + 8f, py, hw, 50f, BtnNeutral,
                () => PlayerTools.SetHoldHunger(!PlayerTools.HoldHunger, lastSnap, settings), playerOk);
            py += 62f + 6f;
            Text(ppage, "Refills use the game's own need setters and are verified by reading the value back. " +
                        "Invincibility and Hold hunger last for this session only and are not saved. Hold hunger is " +
                        "experimental: the game has no hunger option, so it tops hunger up when it drops.",
                x0, py, full, sharedTop - py - 8f, 20f, TextDim, TextAlignmentOptions.TopLeft);

            // Game options page: 2 columns x 3 rows of switches, then a note.
            float oy = contentTop;
            Transform opage = pages[TabOptions].transform;
            for (int i = 0; i < GameOptions.All.Length; i++)
            {
                GameOption option = GameOptions.All[i];
                float ox = x0 + (i % 2) * (hw + 8f);
                optionLabels[i] = MakeButton(opage, GameOptions.Label(option), ox, oy + (i / 2) * 62f, hw, 50f, BtnNeutral,
                    () => GameOptions.Toggle(option, lastSnap, settings),
                    s => GameOptions.BlockReason(s, settings) == null);
            }
            oy += ((GameOptions.All.Length + 1) / 2) * 62f + 6f;
            Text(opage, "These are the game's own custom-game options. ON = normal game behaviour. A change is stored in " +
                        "your save when the game saves; switch back ON to restore normal behaviour.",
                x0, oy, full, sharedTop - oy - 8f, 20f, TextDim, TextAlignmentOptions.TopLeft);

            y = sharedTop;
            statusText = Text(panel.transform, "", x0, y, full, 70f, 22f, TextMain, TextAlignmentOptions.TopLeft);
            y += 76f;
            blockText = Text(panel.transform, "", x0, y, full, 56f, 20f, TextDim, TextAlignmentOptions.TopLeft);
            y += 62f;
            infoText = Text(panel.transform, "", x0, y, full, 130f, 20f, TextDim, TextAlignmentOptions.TopLeft);

            ShowTab(TabMoney);
            panel.SetActive(false);
        }

        private GameObject Page(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(panel.transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        private bool TabEnabled(int tab) =>
            settings == null ||
            (tab == TabPlayer ? settings.PlayerToolsEnabled : tab == TabOptions ? settings.PlayerTogglesEnabled : true);

        private void ShowTab(int tab)
        {
            currentTab = TabEnabled(tab) ? tab : TabMoney;
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].SetActive(i == currentTab);
                tabLabels[i].text = i == currentTab ? "[ " + TabNames[i] + " ]" : TabNames[i];
            }
            nextRefresh = 0f;
        }

        private string Block(GameSnapshot s) =>
            settings == null ? "Starting..." : money.WriteBlockReason(s, settings.MoneyWritesEnabled, settings.AllowOtherBuilds);

        private bool CanStage(GameSnapshot s) => Block(s) == null && money.CurrentPhase == MoneyService.Phase.Idle;

        private TextMeshProUGUI Text(Transform parent, string text, float x, float y, float w, float h,
            float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            Place(go.AddComponent<RectTransform>(), x, y, w, h);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        private TextMeshProUGUI MakeButton(Transform parent, string label, float x, float y, float w, float h, Color color,
            Action onClick, Func<GameSnapshot, bool> enabledWhen)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            Place(go.AddComponent<RectTransform>(), x, y, w, h);
            Image img = go.AddComponent<Image>();
            img.color = color;
            Button b = go.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener((UnityAction)(Action)(() =>
            {
                try
                {
                    AmbitionControlMod.Log.Msg("Phone click: " + label);
                    onClick();
                    nextRefresh = 0f;
                }
                catch (Exception ex)
                {
                    Fail("button '" + label + "'", ex);
                }
            }));
            TextMeshProUGUI caption = Text(go.transform, label, 0f, 0f, w, h, 20f, TextMain, TextAlignmentOptions.Center);
            if (enabledWhen != null) gated.Add(new KeyValuePair<Button, Func<GameSnapshot, bool>>(b, enabledWhen));
            return caption;
        }

        /// <summary>Top-left anchored placement in panel pixels (y grows downward).</summary>
        private static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        // ---------------------------------------------------------------- refresh

        private void Refresh()
        {
            GameSnapshot s = lastSnap;
            if (s == null) return;
            balanceText.text = s.Loaded ? "Balance: $" + s.Money.ToString("N2") : s.NotLoadedReason;
            amountText.text = "Amount: $" + amount.ToString("N0");

            MoneyService.Operation op = money.Staged;
            previewText.text = op == null
                ? (money.StageError != null ? "<color=#ffb04a>Can't stage: " + money.StageError + "</color>" : "")
                :
                "Preview: " + op.Action + " $" + op.Amount.ToString("N0") + "   $" + op.Before.ToString("N2") +
                "  ->  $" + op.Expected.ToString("N2");
            // Show whichever message changed most recently (money or needs).
            if (money.Status != lastMoneyStatus) { lastMoneyStatus = money.Status; shownStatus = money.Status; }
            if (GameOptions.Status != lastToggleStatus) { lastToggleStatus = GameOptions.Status; shownStatus = GameOptions.Status; }
            if (PlayerTools.Status != lastPlayerStatus) { lastPlayerStatus = PlayerTools.Status; shownStatus = PlayerTools.Status; }
            statusText.text = shownStatus ?? "";

            for (int i = 0; i < optionLabels.Length; i++)
                if (optionLabels[i] != null)
                    optionLabels[i].text = GameOptions.Label(GameOptions.All[i]) + ": " +
                        (s.OptionsKnown ? (s.OptionDisabled[i] ? "OFF" : "ON") : "?");
            if (invincibleLabel != null) invincibleLabel.text = "Invincibility: " + (s.Invincible ? "ON" : "OFF");
            if (holdHungerLabel != null) holdHungerLabel.text = "Hold hunger full: " + (PlayerTools.HoldHunger ? "ON" : "OFF");

            if (currentTab == TabOptions)
            {
                string optBlock = settings == null ? "Starting..." : GameOptions.BlockReason(s, settings);
                blockText.text = optBlock == null ? "" : "Switches unavailable: " + optBlock;
            }
            else if (currentTab == TabPlayer)
            {
                string pBlock = settings == null ? "Starting..." : PlayerTools.BlockReason(s, settings);
                blockText.text = pBlock == null ? "" : "Player tools unavailable: " + pBlock;
            }
            else
            {
                string block = Block(s);
                blockText.text = block == null
                    ? "Limits: $10M per change, balance $0-$100M. Logged by the game as 'Cheat' transactions."
                    : "Changes unavailable: " + block;
            }

            infoText.text = s.Loaded
                ? "Day " + s.Day + "  " + s.Hour.ToString("00") + ":" + ((int)s.Minute).ToString("00") +
                  "    Net worth $" + s.NetWorth.ToString("N0") + "\nEnergy " + s.Energy.ToString("0.#") +
                  "    Hunger " + s.Hunger.ToString("0.#") + "    Happiness " + s.Happiness.ToString("0.#")
                : "";

            foreach (var kv in gated)
                if (kv.Key != null) kv.Key.interactable = kv.Value(s);
        }
    }
}
