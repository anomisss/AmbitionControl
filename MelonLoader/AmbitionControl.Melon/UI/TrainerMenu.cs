using AmbitionControl.Core;
using AmbitionControl.Game;
using UnityEngine;

namespace AmbitionControl.UI
{
    /// <summary>
    /// Fixed-rect IMGUI window (GUI.* only, no GUILayout/GUI.Window delegates, to avoid IL2CPP
    /// stripping and delegate-marshalling issues). The amount is built with buttons rather than a
    /// text field: IMGUI cannot stop keystrokes reaching the game's Input System, so typing digits
    /// would also trigger game hotkeys.
    /// </summary>
    internal sealed class TrainerMenu
    {
        private static readonly string[] Tabs = { "Money", "Status", "Diagnostics" };
        private const float W = 460f;
        private const float H = 430f;
        private const float Pad = 10f;
        private const float Row = 24f;

        private readonly MoneyService money;
        private int tab;
        private long amount = 100_000L;
        private float y;
        private bool dragging;
        private float dragDx;
        private float dragDy;

        internal bool Visible;
        internal float X = 20f;
        internal float Y = 80f;

        internal TrainerMenu(MoneyService money) { this.money = money; }

        internal void Draw(GameSnapshot snap, Settings settings)
        {
            if (!Visible) return;

            X = Mathf.Clamp(X, 0f, Mathf.Max(0f, Screen.width - W));
            Y = Mathf.Clamp(Y, 0f, Mathf.Max(0f, Screen.height - H));
            var frame = new Rect(X, Y, W, H);
            GUI.Box(frame, "");
            GUI.Box(frame, "");
            GUI.Box(new Rect(X, Y, W, 26f), "AMBITION CONTROL  " + AmbitionControlMod.VersionLabel + "   [" + settings.ToggleKey + "]   (drag here)");
            if (Click(new Rect(X + W - 28f, Y + 2f, 24f, 22f), "X")) Visible = false;

            // GUI.Toolbar is unusable on this build: it needs GUIContent.Temp(string[]), which is
            // stripped and MelonLoader 0.7.3 fails to unstrip it (runtime NotSupportedException).
            // Plain Click(Rect, string) is confirmed working in-game.
            float tabW = (W - 2 * Pad - (Tabs.Length - 1) * 4f) / Tabs.Length;
            for (int i = 0; i < Tabs.Length; i++)
            {
                string label = i == tab ? "[ " + Tabs[i] + " ]" : Tabs[i];
                if (Click(new Rect(X + Pad + i * (tabW + 4f), Y + 32f, tabW, Row), label) && i != tab)
                {
                    tab = i;
                    if (money.CurrentPhase == MoneyService.Phase.Staged) money.Cancel();
                }
            }

            y = Y + 32f + Row + 8f;
            switch (tab)
            {
                case 0: DrawMoney(snap, settings); break;
                case 1: DrawStatus(snap); break;
                default: DrawDiagnostics(snap, settings); break;
            }

            GUI.Box(new Rect(X + Pad, Y + H - 52f, W - 2 * Pad, 44f), "");
            GUI.Label(new Rect(X + Pad + 4f, Y + H - 50f, W - 2 * Pad - 8f, 42f), money.Status);
        }

        private void Line(string text, float height = Row)
        {
            GUI.Label(new Rect(X + Pad, y, W - 2 * Pad, height), text);
            y += height;
        }

        /// <summary>GUI.Button that also logs clicks, so the log shows whether clicks reach the menu.</summary>
        private static bool Click(Rect r, string text)
        {
            bool clicked = GUI.Button(r, text);
            if (clicked) AmbitionControlMod.Log.Msg("Menu click: " + text);
            return clicked;
        }

        /// <summary>
        /// Title-bar dragging driven from OnUpdate via the Input System mouse, independent of IMGUI
        /// events (GUI.DragWindow needs GUI.Window delegates, which we avoid on IL2CPP).
        /// Returns true when a drag just finished (caller persists the position).
        /// </summary>
        internal bool UpdateDrag(UnityEngine.InputSystem.Mouse mouse)
        {
            if (!Visible || mouse == null) { dragging = false; return false; }
            Vector2 p = mouse.position.ReadValue();
            float gx = p.x;
            float gy = Screen.height - p.y; // Input System is bottom-left origin; IMGUI is top-left
            bool inTitle = gx >= X && gx <= X + W - 32f && gy >= Y && gy <= Y + 26f;

            if (mouse.leftButton.wasPressedThisFrame && gx >= X && gx <= X + W && gy >= Y && gy <= Y + H)
                AmbitionControlMod.Log.Msg("Mouse down inside menu at (" + (int)(gx - X) + "," + (int)(gy - Y) +
                                           ") tab=" + Tabs[tab]);

            if (mouse.leftButton.wasPressedThisFrame && inTitle)
            {
                dragging = true;
                dragDx = gx - X;
                dragDy = gy - Y;
            }
            if (!dragging) return false;
            if (!mouse.leftButton.isPressed)
            {
                dragging = false;
                return true;
            }
            X = gx - dragDx;
            Y = gy - dragDy;
            return false;
        }

        private bool Button(Rect r, string text, bool enabled)
        {
            bool prior = GUI.enabled;
            GUI.enabled = prior && enabled;
            bool clicked = Click(r, text);
            GUI.enabled = prior;
            return clicked && enabled;
        }

        private void DrawMoney(GameSnapshot snap, Settings settings)
        {
            Line(snap.Loaded ? "Balance: $" + snap.Money.ToString("N2") : snap.NotLoadedReason);
            string block = money.WriteBlockReason(snap, settings.MoneyWritesEnabled, settings.AllowOtherBuilds);
            bool canWrite = block == null;

            Line("Amount: $" + amount.ToString("N0"));
            float bw = (W - 2 * Pad - 5 * 4f) / 6f;
            long[] steps = { 1_000L, 10_000L, 100_000L, 1_000_000L, 10_000_000L };
            string[] labels = { "+1K", "+10K", "+100K", "+1M", "+10M" };
            for (int i = 0; i < steps.Length; i++)
                if (Click(new Rect(X + Pad + i * (bw + 4f), y, bw, Row), labels[i]))
                    amount = MoneyCommand.ClampAmount(amount + steps[i]);
            if (Click(new Rect(X + Pad + 5 * (bw + 4f), y, bw, Row), "Clear")) amount = 0L;
            y += Row + 6f;

            bool idle = money.CurrentPhase == MoneyService.Phase.Idle;
            float tw = (W - 2 * Pad - 2 * 6f) / 3f;
            if (Button(new Rect(X + Pad, y, tw, Row), "Stage Add", canWrite && idle && amount > 0))
                money.Stage(MoneyAction.Add, amount, snap);
            if (Button(new Rect(X + Pad + tw + 6f, y, tw, Row), "Stage Remove", canWrite && idle && amount > 0))
                money.Stage(MoneyAction.Remove, amount, snap);
            if (Button(new Rect(X + Pad + 2 * (tw + 6f), y, tw, Row), "Stage Set", canWrite && idle))
                money.Stage(MoneyAction.Set, amount, snap);
            y += Row + 6f;

            MoneyService.Operation op = money.Staged;
            if (op != null)
            {
                Line("PREVIEW: " + op.Action + " $" + op.Amount.ToString("N0") + "   $" + op.Before.ToString("N2") +
                     "  ->  $" + op.Expected.ToString("N2"));
                float hw = (W - 2 * Pad - 6f) / 2f;
                if (Button(new Rect(X + Pad, y, hw, 28f), "Confirm change", canWrite)) money.RequestConfirm();
                if (Click(new Rect(X + Pad + hw + 6f, y, hw, 28f), "Cancel")) money.Cancel();
                y += 34f;
            }

            if (Button(new Rect(X + Pad, y, W - 2 * Pad, Row), "Undo last Ambition Control change",
                    canWrite && money.CanUndo(snap)))
                money.RequestUndo();
            y += Row + 6f;

            if (!canWrite) Line("Writes unavailable: " + block, 40f);
            Line("Limits: $10M per change; balance $0-$100M. Changes are logged by the game as 'Cheat' transactions.", 40f);
        }

        private void DrawStatus(GameSnapshot snap)
        {
            if (!snap.Loaded) { Line(snap.NotLoadedReason); return; }
            Line("Save: " + snap.SaveName);
            Line("Day " + snap.Day + "   " + snap.Hour.ToString("00") + ":" + ((int)snap.Minute).ToString("00"));
            Line("Money: $" + snap.Money.ToString("N2") + "    Net worth: $" + snap.NetWorth.ToString("N0"));
            Line("Energy: " + snap.Energy.ToString("0.#"));
            Line("Hunger: " + snap.Hunger.ToString("0.#"));
            Line("Happiness: " + snap.Happiness.ToString("0.#"));
            y += 6f;
            Line("Read-only. Values refresh 4x per second.");
        }

        private void DrawDiagnostics(GameSnapshot snap, Settings settings)
        {
            int? build = GameBridge.BuildNumber;
            Line("Ambition Control " + AmbitionControlMod.VersionLabel + " (MelonLoader build)");
            Line("Game build: " + (build.HasValue ? build.Value.ToString() : "unknown" +
                 (GameBridge.BuildError != null ? " (" + GameBridge.BuildError + ")" : "")) +
                 "   verified target: " + GameBridge.TargetBuild);
            Line("Save loaded: " + (snap.Loaded ? "yes" : "no - " + snap.NotLoadedReason));
            Line("Saving in progress: " + (snap.Loaded && snap.Saving ? "yes" : "no"));
            Line("Money writes pref: " + (settings.MoneyWritesEnabled ? "on" : "off") +
                 "   allow other builds: " + (settings.AllowOtherBuilds ? "yes" : "no"));
            Line("Session latch: " + (money.LatchReason ?? "none"), 40f);
            Line("Settings: UserData\\MelonPreferences.cfg [AmbitionControl]", 40f);
        }
    }
}
