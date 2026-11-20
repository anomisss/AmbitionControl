using System;
using System.Collections.Generic;
using System.Linq;
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
        private Image statusBg;
        // Tabs (index order = button order). New feature groups add a name + page here.
        private static readonly string[] TabNames = { "Money", "Player", "Economy", "World", "Staff", "Rivals", "Quest", "Options" };
        private const int TabMoney = 0, TabPlayer = 1, TabEconomy = 2, TabWorld = 3, TabStaff = 4, TabRivals = 5, TabQuest = 6, TabOptions = 7;
        private readonly TextMeshProUGUI[] rivalRowLabels = new TextMeshProUGUI[RivalTools.Areas.Length];
        private readonly TextMeshProUGUI[] rivalBtnLabels = new TextMeshProUGUI[RivalTools.Areas.Length];
        private TextMeshProUGUI rivalAllLabel;
        private TextMeshProUGUI questText, questObjLabel, questQuestLabel, questGoalsLabel;
        private string lastQuestStatus;
        private List<RivalRow> rivalRows = new List<RivalRow>();
        private string rivalError, lastRivalStatus;
        private float nextRivalRead;
        private TextMeshProUGUI staffSummary, staffList, staffMaxLabel;
        private int staffPage;
        private float nextStaffRead;
        private List<EmployeeRow> staffRows = new List<EmployeeRow>();
        private string staffError, lastStaffStatus;
        private TextMeshProUGUI pauseLabel, trafficLabel, saveLabel, vehicleText;
        private string lastWorldStatus;
        private readonly TextMeshProUGUI[] econLabels = new TextMeshProUGUI[GameEconomy.All.Length];
        private readonly TextMeshProUGUI[] presetLabels = new TextMeshProUGUI[GameEconomy.Presets.Length];
        private string lastEconStatus;
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
        private static readonly Color TabActive = new Color(1f, 0.80f, 0.30f, 1f);
        private static readonly Color StatusNeutral = new Color(0.16f, 0.18f, 0.21f, 1f);
        private static readonly Color StatusOk = new Color(0.12f, 0.36f, 0.22f, 1f);
        private static readonly Color StatusWarn = new Color(0.45f, 0.33f, 0.08f, 1f);
        private static readonly Color StatusBad = new Color(0.45f, 0.14f, 0.14f, 1f);

        /// <summary>Colour for a status message: green verified, amber awaiting confirm/warning, red failed/refused.</summary>
        private static Color StatusColor(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return StatusNeutral;
            string m = msg.ToLowerInvariant();
            if (m.Contains("failed") || m.Contains("did not") || (m.Contains(" not ") && m.Contains(":")) || m.StartsWith("can't") || m.Contains("error"))
                return StatusBad;
            if (m.Contains("confirm") || m.Contains("?") || m.Contains("permanent") || m.Contains("..."))
                return StatusWarn;
            if (m.Contains("verified") || m.Contains("saved") || m.Contains("returned true"))
                return StatusOk;
            return StatusNeutral;
        }

        /// <summary>Small uppercase section label with a thin rule; returns the y below it.</summary>
        private float Header(Transform parent, string label, float x, float y, float w)
        {
            TextMeshProUGUI t = Text(parent, label.ToUpperInvariant(), x, y, w, 22f, 15f, TabActive, TextAlignmentOptions.BottomLeft);
            t.characterSpacing = 2f;
            var rule = new GameObject("Rule");
            rule.transform.SetParent(parent, false);
            Place(rule.AddComponent<RectTransform>(), x, y + 24f, w, 1f);
            Image img = rule.AddComponent<Image>();
            img.color = BtnNeutral;
            img.raycastTarget = false;
            return y + 32f;
        }

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

            Text(panel.transform, Title, x0, y, full - 160f, 44f, 30f, TextMain, TextAlignmentOptions.Left);
            Text(panel.transform, AmbitionControlMod.VersionLabel, x0 + full - 160f, y + 8f, 160f, 32f, 18f, TextDim, TextAlignmentOptions.Right);
            y += 54f;

            // Tabs. Pages are full-size children so their coordinates match the panel's.
            float hw = (full - 8f) / 2f;
            float tw = (full - 2 * 8f) / 3f;
            float tabW = (full - (TabNames.Length - 1) * 8f) / TabNames.Length;
            for (int i = 0; i < TabNames.Length; i++)
            {
                int index = i;
                pages[i] = Page(TabNames[i].Replace(" ", "") + "Page");
                tabLabels[i] = MakeButton(panel.transform, TabNames[i], x0 + i * (tabW + 8f), y, tabW, 46f, BtnNeutral,
                    () => ShowTab(index), null);
                tabLabels[i].fontSize = 15f;
            }
            y += 58f;
            float contentTop = y;
            const float sharedTop = 566f; // pages end here; status bar, hint and footer follow

            // Money page.
            Transform page = pages[TabMoney].transform;
            balanceText = Text(page, "", x0, y, full, 36f, 26f, TextMain, TextAlignmentOptions.Left);
            y += 42f;
            y = Header(page, "Amount", x0, y, full);
            amountText = Text(page, "", x0, y, full, 30f, 22f, TextDim, TextAlignmentOptions.Left);
            y += 34f;
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
            y += 56f;
            y = Header(page, "Change", x0, y, full);
            MakeButton(page, "Stage Add", x0, y, tw, 50f, BtnNeutral,
                () => money.Stage(MoneyAction.Add, amount, lastSnap), s => CanStage(s) && amount > 0);
            MakeButton(page, "Stage Remove", x0 + tw + 8f, y, tw, 50f, BtnNeutral,
                () => money.Stage(MoneyAction.Remove, amount, lastSnap), s => CanStage(s) && amount > 0);
            MakeButton(page, "Stage Set", x0 + 2 * (tw + 8f), y, tw, 50f, BtnNeutral,
                () => money.Stage(MoneyAction.Set, amount, lastSnap), CanStage);
            y += 58f;
            previewText = Text(page, "", x0, y, full, 32f, 20f, TextMain, TextAlignmentOptions.Left);
            y += 36f;
            MakeButton(page, "Confirm change", x0, y, tw, 50f, BtnGo,
                () => money.RequestConfirm(), s => money.Staged != null && Block(s) == null);
            MakeButton(page, "Cancel", x0 + tw + 8f, y, tw, 50f, BtnStop,
                () => money.Cancel(), s => money.Staged != null);
            MakeButton(page, "Undo last change", x0 + 2 * (tw + 8f), y, tw, 50f, BtnNeutral,
                () => money.RequestUndo(), s => Block(s) == null && money.CanUndo(s));

            // Player page.
            float py = contentTop;
            Transform ppage = pages[TabPlayer].transform;
            Func<GameSnapshot, bool> playerOk = s => settings != null && PlayerTools.BlockReason(s, settings) == null;
            py = Header(ppage, "Needs", x0, py, full);
            MakeButton(ppage, "Refill energy", x0, py, hw, 50f, BtnNeutral,
                () => PlayerTools.Refill(Need.Energy, lastSnap, settings), playerOk);
            MakeButton(ppage, "Refill hunger", x0 + hw + 8f, py, hw, 50f, BtnNeutral,
                () => PlayerTools.Refill(Need.Hunger, lastSnap, settings), playerOk);
            py += 58f;
            MakeButton(ppage, "Refill happiness", x0, py, hw, 50f, BtnNeutral,
                () => PlayerTools.Refill(Need.Happiness, lastSnap, settings), playerOk);
            MakeButton(ppage, "Refill all needs", x0 + hw + 8f, py, hw, 50f, BtnGo,
                () => PlayerTools.RefillAll(lastSnap, settings), playerOk);
            py += 62f;
            py = Header(ppage, "This session only", x0, py, full);
            invincibleLabel = MakeButton(ppage, "Invincibility", x0, py, hw, 50f, BtnNeutral,
                () => PlayerTools.ToggleInvincibility(lastSnap, settings), playerOk);
            holdHungerLabel = MakeButton(ppage, "Hold hunger full", x0 + hw + 8f, py, hw, 50f, BtnNeutral,
                () => PlayerTools.SetHoldHunger(!PlayerTools.HoldHunger, lastSnap, settings), playerOk);

            // Economy page.
            float ey = contentTop;
            Transform epage = pages[TabEconomy].transform;
            Func<GameSnapshot, bool> econOk = s => settings != null && GameEconomy.BlockReason(s, settings) == null;
            ey = Header(epage, "Difficulty presets", x0, ey, full);
            for (int i = 0; i < GameEconomy.Presets.Length; i++)
            {
                Difficulty preset = GameEconomy.Presets[i];
                presetLabels[i] = MakeButton(epage, preset + " preset", x0 + i * (tw + 8f), ey, tw, 50f, BtnNeutral,
                    () => GameEconomy.PresetPressed(preset, lastSnap, settings), econOk);
            }
            ey += 62f;
            ey = Header(epage, "Settings (tap to cycle)", x0, ey, full);
            for (int i = 0; i < GameEconomy.All.Length; i++)
            {
                EconSetting setting = GameEconomy.All[i];
                econLabels[i] = MakeButton(epage, GameEconomy.Label(setting), x0 + (i % 2) * (hw + 8f), ey + (i / 2) * 56f, hw, 48f,
                    BtnNeutral, () => GameEconomy.Cycle(setting, lastSnap, settings), econOk);
            }

            // World page.
            float wy = contentTop;
            Transform wpage = pages[TabWorld].transform;
            Func<GameSnapshot, bool> worldOk = s => settings != null && WorldTools.BlockReason(s, settings) == null;
            Func<GameSnapshot, bool> vehicleOk = s => worldOk(s) && s.InVehicle && s.VehicleOwned;
            wy = Header(wpage, "Your vehicle", x0, wy, full);
            MakeButton(wpage, "Repair", x0, wy, tw, 50f, BtnNeutral, () => WorldTools.RepairVehicle(lastSnap, settings), vehicleOk);
            MakeButton(wpage, "Refuel", x0 + tw + 8f, wy, tw, 50f, BtnNeutral, () => WorldTools.RefuelVehicle(lastSnap, settings), vehicleOk);
            MakeButton(wpage, "Clean", x0 + 2 * (tw + 8f), wy, tw, 50f, BtnNeutral, () => WorldTools.CleanVehicle(lastSnap, settings), vehicleOk);
            wy += 54f;
            vehicleText = Text(wpage, "", x0, wy, full, 26f, 18f, TextDim, TextAlignmentOptions.Left);
            wy += 32f;
            wy = Header(wpage, "Time", x0, wy, full);
            pauseLabel = MakeButton(wpage, "Pause", x0, wy, hw, 50f, BtnNeutral, () => WorldTools.TogglePause(lastSnap, settings),
                s => worldOk(s) && s.PauseKnown);
            trafficLabel = MakeButton(wpage, "Traffic", x0 + hw + 8f, wy, hw, 50f, BtnNeutral, () => WorldTools.ToggleTraffic(lastSnap, settings),
                s => worldOk(s) && s.TrafficKnown);
            wy += 58f;
            float sw = (full - (WorldTools.SpeedPercents.Length - 1) * 8f) / WorldTools.SpeedPercents.Length;
            for (int i = 0; i < WorldTools.SpeedPercents.Length; i++)
            {
                int pct = WorldTools.SpeedPercents[i];
                MakeButton(wpage, (pct / 100f).ToString("0.#") + "x", x0 + i * (sw + 8f), wy, sw, 50f, BtnNeutral,
                    () => WorldTools.SetSpeed(pct, lastSnap, settings), worldOk);
            }
            wy += 62f;
            wy = Header(wpage, "Game", x0, wy, full);
            saveLabel = MakeButton(wpage, "Save game", x0, wy, full, 50f, BtnGo, () => WorldTools.SaveGame(lastSnap, settings), worldOk);

            // Staff page.
            float sy = contentTop;
            Transform spage = pages[TabStaff].transform;
            sy = Header(spage, "Employees", x0, sy, full);
            staffSummary = Text(spage, "", x0, sy, full, 30f, 20f, TextMain, TextAlignmentOptions.Left);
            sy += 34f;
            staffList = Text(spage, "", x0, sy, full, EmployeeTools.PageSize * 30f, 18f, TextDim, TextAlignmentOptions.TopLeft);
            sy += EmployeeTools.PageSize * 30f + 8f;
            Func<GameSnapshot, bool> staffOk = s => settings != null && EmployeeTools.BlockReason(s, settings) == null;
            MakeButton(spage, "< Prev", x0, sy, tw, 50f, BtnNeutral, () => { staffPage = Math.Max(0, staffPage - 1); nextStaffRead = 0f; }, null);
            MakeButton(spage, "Next >", x0 + tw + 8f, sy, tw, 50f, BtnNeutral, () => { staffPage++; nextStaffRead = 0f; }, null);
            staffMaxLabel = MakeButton(spage, "Max satisfaction", x0 + 2 * (tw + 8f), sy, tw, 50f, BtnNeutral,
                () => { EmployeeTools.MaxSatisfaction(lastSnap, settings); nextStaffRead = 0f; }, staffOk);

            // Rivals page.
            float ry = contentTop;
            Transform rpage = pages[TabRivals].transform;
            Func<GameSnapshot, bool> rivalsOk = s => settings != null && RivalTools.BlockReason(s, settings) == null;
            ry = Header(rpage, "Rivals by neighbourhood", x0, ry, full);
            for (int i = 0; i < RivalTools.Areas.Length; i++)
            {
                int index = i;
                rivalRowLabels[i] = Text(rpage, RivalTools.AreaName(RivalTools.Areas[i]), x0, ry, full - 168f, 44f, 18f, TextMain, TextAlignmentOptions.Left);
                rivalBtnLabels[i] = MakeButton(rpage, "Defeat", x0 + full - 160f, ry, 160f, 44f, BtnStop,
                    () => { RivalTools.DefeatPressed(RivalTools.Areas[index], lastSnap, settings); nextRivalRead = 0f; },
                    s => rivalsOk(s) && RivalEligible(index));
                ry += 50f;
            }
            rivalAllLabel = MakeButton(rpage, "Defeat all active rivals", x0, ry + 4f, full, 46f, BtnStop,
                () => { RivalTools.DefeatAllPressed(lastSnap, settings); nextRivalRead = 0f; },
                s => rivalsOk(s) && rivalRows.Any(r => r.Met && r.Active && !r.Defeated));

            // Quest page.
            float qy = contentTop;
            Transform qpage = pages[TabQuest].transform;
            Func<GameSnapshot, bool> questOk = s => settings != null && QuestTools.BlockReason(s, settings) == null;
            qy = Header(qpage, "Current quest", x0, qy, full);
            questText = Text(qpage, "", x0, qy, full, 52f, 19f, TextMain, TextAlignmentOptions.TopLeft);
            qy += 58f;
            qy = Header(qpage, "Teleport (on foot, outdoors)", x0, qy, full);
            MakeButton(qpage, "To quest target", x0, qy, hw, 50f, BtnNeutral,
                () => QuestTools.Teleport(TeleportTarget.QuestTarget, lastSnap, settings),
                s => questOk(s) && QuestTools.TeleportBlock(TeleportTarget.QuestTarget) == null);
            MakeButton(qpage, "To GPS destination", x0 + hw + 8f, qy, hw, 50f, BtnNeutral,
                () => QuestTools.Teleport(TeleportTarget.GpsDestination, lastSnap, settings),
                s => questOk(s) && QuestTools.TeleportBlock(TeleportTarget.GpsDestination) == null);
            qy += 62f;
            qy = Header(qpage, "Complete (cannot be undone)", x0, qy, full);
            questObjLabel = MakeButton(qpage, "Complete objective", x0, qy, hw, 50f, BtnStop,
                () => QuestTools.CompleteObjectivePressed(lastSnap, settings), s => questOk(s) && QuestTools.QuestBlock() == null);
            questQuestLabel = MakeButton(qpage, "Complete whole quest", x0 + hw + 8f, qy, hw, 50f, BtnStop,
                () => QuestTools.CompleteQuestPressed(lastSnap, settings), s => questOk(s) && QuestTools.QuestBlock() == null);
            qy += 58f;
            questGoalsLabel = MakeButton(qpage, "Complete all personal goals", x0, qy, full, 50f, BtnStop,
                () => QuestTools.CompleteGoalsPressed(lastSnap, settings), questOk);

            // Game options page.
            float oy = contentTop;
            Transform opage = pages[TabOptions].transform;
            oy = Header(opage, "Game options (saved with your save)", x0, oy, full);
            for (int i = 0; i < GameOptions.All.Length; i++)
            {
                GameOption option = GameOptions.All[i];
                float ox = x0 + (i % 2) * (hw + 8f);
                optionLabels[i] = MakeButton(opage, GameOptions.Label(option), ox, oy + (i / 2) * 58f, hw, 50f, BtnNeutral,
                    () => GameOptions.Toggle(option, lastSnap, settings),
                    s => GameOptions.BlockReason(s, settings) == null);
            }

            // Shared bottom: colored status bar, one-line hint, compact footer.
            y = sharedTop;
            var barGo = new GameObject("StatusBar");
            barGo.transform.SetParent(panel.transform, false);
            Place(barGo.AddComponent<RectTransform>(), x0, y, full, 66f);
            statusBg = barGo.AddComponent<Image>();
            statusBg.color = StatusNeutral;
            statusBg.raycastTarget = false;
            statusText = Text(barGo.transform, "", 12f, 6f, full - 24f, 54f, 19f, TextMain, TextAlignmentOptions.MidlineLeft);
            y += 74f;
            blockText = Text(panel.transform, "", x0, y, full, 48f, 17f, TextDim, TextAlignmentOptions.TopLeft);
            y += 52f;
            var line = new GameObject("FooterLine");
            line.transform.SetParent(panel.transform, false);
            Place(line.AddComponent<RectTransform>(), x0, y, full, 2f);
            Image lineImg = line.AddComponent<Image>();
            lineImg.color = BtnNeutral;
            lineImg.raycastTarget = false;
            y += 8f;
            infoText = Text(panel.transform, "", x0, y, full, 30f, 18f, TextDim, TextAlignmentOptions.Left);

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
            (tab == TabPlayer ? settings.PlayerToolsEnabled : tab == TabOptions ? settings.PlayerTogglesEnabled :
             tab == TabEconomy ? settings.EconomyControlsEnabled : tab == TabWorld ? settings.WorldToolsEnabled :
             tab == TabStaff ? settings.EmployeeToolsEnabled : tab == TabRivals ? settings.RivalToolsEnabled :
             tab == TabQuest ? settings.QuestToolsEnabled : true);

        private void ShowTab(int tab)
        {
            currentTab = TabEnabled(tab) ? tab : TabMoney;
            GameEconomy.ClearPending();
            WorldTools.ClearPending();
            EmployeeTools.ClearPending();
            RivalTools.ClearPending();
            QuestTools.ClearPending();
            nextRivalRead = 0f;
            nextStaffRead = 0f;
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].SetActive(i == currentTab);
                tabLabels[i].text = TabNames[i];
                tabLabels[i].color = i == currentTab ? TabActive : TextMain;
            }
            nextRefresh = 0f;
        }

        /// <summary>Re-read the employee list at most once per second while the Staff tab is open.</summary>
        private void RefreshStaff(GameSnapshot s)
        {
            if (Time.unscaledTime >= nextStaffRead)
            {
                nextStaffRead = Time.unscaledTime + 1f;
                staffRows = s.Loaded ? EmployeeTools.ReadRows(s, out staffError) : new List<EmployeeRow>();
                if (!s.Loaded) staffError = s.NotLoadedReason;
            }

            int pages = Math.Max(1, (staffRows.Count + EmployeeTools.PageSize - 1) / EmployeeTools.PageSize);
            staffPage = Math.Min(staffPage, pages - 1);
            if (staffError != null)
            {
                staffSummary.text = staffError;
                staffList.text = "";
            }
            else if (staffRows.Count == 0)
            {
                staffSummary.text = "No hired employees.";
                staffList.text = "";
            }
            else
            {
                staffSummary.text = staffRows.Count + " employees   avg satisfaction " +
                    staffRows.Average(r => r.Satisfaction).ToString("0") + "   wages $" +
                    staffRows.Sum(r => r.Wage).ToString("N0") + "/h   page " + (staffPage + 1) + "/" + pages;
                var sb = new System.Text.StringBuilder();
                foreach (EmployeeRow r in staffRows.Skip(staffPage * EmployeeTools.PageSize).Take(EmployeeTools.PageSize))
                {
                    string name = r.Name.Length > 34 ? r.Name.Substring(0, 33) + "…" : r.Name;
                    sb.Append(name).Append("  $").Append(r.Wage.ToString("0.##")).Append("/h  sat ")
                      .Append(r.Satisfaction.ToString("0"));
                    if (r.Absent) sb.Append("  (absent)");
                    if (!r.Assigned) sb.Append("  (unassigned)");
                    sb.Append('\n');
                }
                staffList.text = sb.ToString();
            }
            if (staffMaxLabel != null) staffMaxLabel.text = EmployeeTools.ConfirmPending ? "Confirm" : "Max satisfaction";
        }

        private bool RivalEligible(int index)
        {
            RivalRow r = rivalRows.FirstOrDefault(x => x.Neighbourhood == RivalTools.Areas[index]);
            return r != null && r.Met && r.Active && !r.Defeated;
        }

        /// <summary>Re-read rivals at most once per second while the Rivals tab is open.</summary>
        private void RefreshRivals(GameSnapshot s)
        {
            if (Time.unscaledTime >= nextRivalRead)
            {
                nextRivalRead = Time.unscaledTime + 1f;
                rivalRows = s.Loaded ? RivalTools.ReadRows(s, out rivalError) : new List<RivalRow>();
            }
            for (int i = 0; i < RivalTools.Areas.Length; i++)
            {
                Il2CppBigAmbitions.Neighborhoods.Neighbourhood area = RivalTools.Areas[i];
                RivalRow r = rivalRows.FirstOrDefault(x => x.Neighbourhood == area);
                string state = r == null ? "no rival" : r.Defeated ? "defeated" : !r.Met || !r.Active ? "not active" : "active";
                rivalRowLabels[i].text = r == null
                    ? RivalTools.AreaName(area) + ": no rival"
                    : r.Name + "  (" + RivalTools.AreaName(area) + ")  " + state +
                      (r.Met && !r.Defeated ? "  $" + r.WeeklyIncome.ToString("N0") + "/wk" : "");
                rivalBtnLabels[i].text = r != null && r.Defeated ? "Defeated" :
                    RivalTools.PendingArea == area ? "Confirm" : "Defeat";
            }
            rivalAllLabel.text = RivalTools.PendingAll ? "Confirm: defeat ALL active rivals" : "Defeat all active rivals";
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
            if (GameEconomy.Status != lastEconStatus) { lastEconStatus = GameEconomy.Status; shownStatus = GameEconomy.Status; }
            if (WorldTools.Status != lastWorldStatus) { lastWorldStatus = WorldTools.Status; shownStatus = WorldTools.Status; }
            if (EmployeeTools.Status != lastStaffStatus) { lastStaffStatus = EmployeeTools.Status; shownStatus = EmployeeTools.Status; }
            if (RivalTools.Status != lastRivalStatus) { lastRivalStatus = RivalTools.Status; shownStatus = RivalTools.Status; }
            if (QuestTools.Status != lastQuestStatus) { lastQuestStatus = QuestTools.Status; shownStatus = QuestTools.Status; }
            statusText.text = shownStatus ?? "Ready.";
            if (statusBg != null) statusBg.color = StatusColor(shownStatus);

            for (int i = 0; i < optionLabels.Length; i++)
                if (optionLabels[i] != null)
                    optionLabels[i].text = GameOptions.Label(GameOptions.All[i]) + ": " +
                        (s.OptionsKnown ? (GameOptions.DisplayOn(GameOptions.All[i], s.OptionDisabled[i]) ? "ON" : "OFF") : "?");
            for (int i = 0; i < econLabels.Length; i++)
                if (econLabels[i] != null)
                {
                    EconSetting e = GameEconomy.All[i];
                    string from = s.OptionsKnown ? GameEconomy.PresetNames(e, s.Econ[i]) : null;
                    econLabels[i].text = GameEconomy.Label(e) + ": " + (s.OptionsKnown ? GameEconomy.Format(e, s.Econ[i]) : "?") +
                                         (from != null ? " (" + from + ")" : s.OptionsKnown ? " (custom)" : "");
                }
            for (int i = 0; i < presetLabels.Length; i++)
                if (presetLabels[i] != null)
                {
                    Difficulty d = GameEconomy.Presets[i];
                    presetLabels[i].text = GameEconomy.PendingPreset == d ? "Confirm " + d :
                        d + (s.OptionsKnown && s.CurrentDifficulty == d ? " (current)" : " preset");
                }
            if (currentTab == TabStaff && staffList != null) RefreshStaff(s);
            if (currentTab == TabRivals && rivalAllLabel != null) RefreshRivals(s);
            if (currentTab == TabQuest && questText != null)
            {
                questText.text = s.Loaded ? QuestTools.CurrentObjectiveText() : "";
                questObjLabel.text = QuestTools.PendingObjective ? "Confirm objective" : "Complete objective";
                questQuestLabel.text = QuestTools.PendingQuest ? "Confirm quest" : "Complete whole quest";
                questGoalsLabel.text = QuestTools.PendingGoals ? "Confirm: complete all personal goals" :
                    QuestTools.GoalCounts(out int gd, out int gt) ? "Complete all personal goals (" + gd + "/" + gt + " done)" : "Complete all personal goals";
            }
            if (pauseLabel != null) pauseLabel.text = s.PauseKnown && s.Paused ? "Resume game" : "Pause game";
            if (trafficLabel != null) trafficLabel.text = "Traffic: " + (s.TrafficKnown ? (s.Traffic ? "ON" : "OFF") : "?");
            if (saveLabel != null) saveLabel.text = WorldTools.SavePending ? "Confirm save" : "Save game";
            if (vehicleText != null)
                vehicleText.text = !s.InVehicle ? "Not in a vehicle." :
                    !s.VehicleOwned ? "Not your vehicle; vehicle tools disabled." :
                    "Fuel " + s.VehicleFuel.ToString("0") + "/" + s.VehicleMaxFuel.ToString("0") +
                    "   Damage " + s.VehicleDamage.ToString("0.##") + "   Dirt " + s.VehicleDirt.ToString("0.##");
            if (invincibleLabel != null) invincibleLabel.text = "Invincibility: " + (s.Invincible ? "ON" : "OFF");
            if (holdHungerLabel != null) holdHungerLabel.text = "Hold hunger full: " + (PlayerTools.HoldHunger ? "ON" : "OFF");

            // One short hint per tab; when the tab's tools are unavailable, the reason replaces the hint.
            string tabBlock = null, hint = "";
            if (settings == null) tabBlock = "Starting...";
            else if (currentTab == TabOptions) { tabBlock = GameOptions.BlockReason(s, settings); hint = "Unlocks and tutorial may need a save reload."; }
            else if (currentTab == TabEconomy) { tabBlock = GameEconomy.BlockReason(s, settings); hint = "Values cycle through the game's Easy / Normal / Hard values."; }
            else if (currentTab == TabRivals) { tabBlock = RivalTools.BlockReason(s, settings); hint = rivalError ?? "Defeat is permanent: the game closes their businesses."; }
            else if (currentTab == TabStaff) { tabBlock = EmployeeTools.BlockReason(s, settings); hint = "Max satisfaction is temporary; the game recalculates it."; }
            else if (currentTab == TabWorld) { tabBlock = WorldTools.BlockReason(s, settings); hint = "Vehicle tools work only in your own vehicle. Speeds above 2x are experimental."; }
            else if (currentTab == TabPlayer) { tabBlock = PlayerTools.BlockReason(s, settings); hint = "Refills are checked by reading the value back."; }
            else if (currentTab == TabQuest)
            {
                tabBlock = QuestTools.BlockReason(s, settings);
                hint = QuestTools.TeleportBlock(TeleportTarget.QuestTarget) ?? QuestTools.QuestBlock() ?? "Set a GPS destination in the game before using the GPS teleport.";
            }
            else { tabBlock = Block(s); hint = "Limits: $10M per change, balance $0-$100M."; }
            blockText.text = tabBlock != null ? "<color=#ffb04a>Unavailable: " + tabBlock + "</color>" : hint;

            infoText.text = s.Loaded
                ? "$" + s.Money.ToString("N0") + "   |   Day " + s.Day + "  " + s.Hour.ToString("00") + ":" + ((int)s.Minute).ToString("00") +
                  "   |   Energy " + s.Energy.ToString("0") + "  Hunger " + s.Hunger.ToString("0") + "  Happy " + s.Happiness.ToString("0")
                : s.NotLoadedReason;

            foreach (var kv in gated)
                if (kv.Key != null) kv.Key.interactable = kv.Value(s);
        }
    }
}
