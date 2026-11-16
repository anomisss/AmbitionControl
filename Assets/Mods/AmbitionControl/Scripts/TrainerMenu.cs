using System;
using UnityEngine;

namespace AmbitionControl
{
    internal sealed class TrainerMenu
    {
        private const int WindowId = 1734282;
        private static readonly string[] Tabs = { "Money", "Player", "Time", "Diagnostics", "Settings" };
        private readonly TrainerSettings settings;
        private readonly MoneyController money = new MoneyController();
        private Rect bounds;
        private Vector2 scroll;
        private string amount = "100000";
        private string notification = "Use a backup save before money modifications.";
        private PendingMoneyAction pending;
        private bool reportWritten;

        internal TrainerMenu()
        {
            settings = TrainerSettings.Load();
            bounds = new Rect(settings.windowX, settings.windowY, 600f, 475f);
            settings.selectedTab = Mathf.Clamp(settings.selectedTab, 0, Tabs.Length - 1);
        }

        internal void Render()
        {
            var evt = Event.current;
            if (evt != null && evt.type == EventType.KeyDown && evt.keyCode == KeyCode.F10)
            {
                settings.visible = !settings.visible;
                pending = null;
                Save();
                evt.Use();
            }
            if (!settings.visible) return;

            bounds.width = Mathf.Min(600f, Mathf.Max(180f, Screen.width - 12f));
            bounds.height = Mathf.Min(475f, Mathf.Max(140f, Screen.height - 12f));
            bounds.x = Mathf.Clamp(bounds.x, 0f, Mathf.Max(0f, Screen.width - bounds.width));
            bounds.y = Mathf.Clamp(bounds.y, 0f, Mathf.Max(0f, Screen.height - bounds.height));

            bounds = GUILayout.Window(WindowId, bounds, DrawWindow, "AMBITION CONTROL  |  v0.3.0",
                GUILayout.Width(bounds.width), GUILayout.Height(bounds.height));

            // Unity IMGUI input consumption does not guarantee blocking other game input systems.
            if (evt != null && evt.isMouse && bounds.Contains(evt.mousePosition) &&
                evt.type != EventType.Used && evt.type != EventType.Repaint && evt.type != EventType.Layout)
                evt.Use();

            if (evt != null && evt.rawType == EventType.MouseUp) Save();
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("F10 to toggle");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", GUILayout.Width(30)))
            {
                settings.visible = false;
                pending = null;
                Save();
            }
            GUILayout.EndHorizontal();

            int selected = GUILayout.Toolbar(settings.selectedTab, Tabs);
            if (selected != settings.selectedTab)
            {
                settings.selectedTab = selected;
                pending = null;
                Save();
            }

            scroll = GUILayout.BeginScrollView(scroll);
            switch (settings.selectedTab)
            {
                case 0: DrawMoney(); break;
                case 1: DrawPlayer(); break;
                case 2: DrawTime(); break;
                case 3: DrawDiagnostics(); break;
                default: DrawSettings(); break;
            }
            GUILayout.EndScrollView();
            GUILayout.Label(notification, GUI.skin.box);
            GUI.DragWindow(new Rect(0f, 0f, bounds.width - 45f, 24f));
        }

        private void DrawMoney()
        {
            GUILayout.Label("MONEY & ECONOMY", GUI.skin.box);
            float balance;
            object identity;
            string error;
            bool loaded = MoneyController.TryRead(out balance, out identity, out error);
            GUILayout.Label(loaded ? "Current balance: $" + balance.ToString("N2") : error);
            GUILayout.Label("Amount (whole dollars):");
            amount = GUILayout.TextField(amount, 18);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+ $10K")) { amount = "10000"; pending = null; }
            if (GUILayout.Button("+ $100K")) { amount = "100000"; pending = null; }
            if (GUILayout.Button("+ $1M")) { amount = "1000000"; pending = null; }
            GUILayout.EndHorizontal();

            long parsed;
            bool valid = MoneyCommand.TryParse(amount, out parsed, out error);
            if (!valid) GUILayout.Label(error);

            bool prior = GUI.enabled;
            GUI.enabled = loaded && valid;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Stage Add")) Stage(MoneyAction.Add, parsed);
            if (GUILayout.Button("Stage Remove")) Stage(MoneyAction.Remove, parsed);
            if (GUILayout.Button("Stage Set")) Stage(MoneyAction.Set, parsed);
            GUILayout.EndHorizontal();
            GUI.enabled = prior;

            if (pending != null)
            {
                GUILayout.Label("CONFIRM: " + pending.Action + " $" + pending.Amount.ToString("N0") +
                                "  |  Before $" + pending.Before.ToString("N2") +
                                "  |  Expected $" + (pending.Before + pending.Delta).ToString("N2"));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Confirm change", GUILayout.Height(30)))
                {
                    notification = money.TryApply(pending, out string result) ? result : result;
                    pending = null;
                }
                if (GUILayout.Button("Cancel", GUILayout.Height(30)))
                {
                    pending = null;
                    notification = "Money command cancelled.";
                }
                GUILayout.EndHorizontal();
            }

            prior = GUI.enabled;
            GUI.enabled = loaded && money.HasUndo && pending == null;
            if (GUILayout.Button("Undo last Ambition Control money action"))
                money.TryUndo(out notification);
            GUI.enabled = prior;
            GUILayout.Label("Limits: $10M change per action, supported total balance $0 to $100M.");
            GUILayout.Label("Only one-step undo; it expires when the balance or loaded save changes.");
        }

        private void Stage(MoneyAction action, long value)
        {
            PendingMoneyAction candidate;
            string reason;
            if (money.TryStage(action, value, out candidate, out reason))
            {
                pending = candidate;
                notification = "Review the proposed change, then confirm.";
            }
            else
            {
                pending = null;
                notification = reason;
            }
        }

        private void DrawPlayer()
        {
            GUILayout.Label("PLAYER STATUS (READ-ONLY)", GUI.skin.box);
            var save = SaveGameManager.Current;
            if (save == null) { GUILayout.Label("Load a city save first."); return; }
            GUILayout.Label("Energy: " + save.Energy.ToString("N1"));
            GUILayout.Label("Hunger: " + save.Hunger.ToString("N1"));
            GUILayout.Label("Happiness: " + save.Happiness.ToString("N1"));
            GUILayout.Label("Needs-changing controls are planned for a later verified release.");
        }

        private void DrawTime()
        {
            GUILayout.Label("WORLD TIME (READ-ONLY)", GUI.skin.box);
            var save = SaveGameManager.Current;
            if (save == null) { GUILayout.Label("Load a city save first."); return; }
            GUILayout.Label("Day: " + save.Day);
            GUILayout.Label("Time: " + save.Hour.ToString("00") + ":" + Mathf.FloorToInt(save.Minute).ToString("00"));
            GUILayout.Label("Simulation speed changes are not implemented in this release.");
        }

        private void DrawDiagnostics()
        {
            GUILayout.Label("DIAGNOSTICS", GUI.skin.box);
            GUILayout.Label("Opt-in type/member metadata report. Does not read save values.");
            if (GUILayout.Button("Write API candidate report", GUILayout.Height(34)))
            {
                try
                {
                    notification = "Saved: " + ApiProbe.WriteReport();
                    reportWritten = true;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    notification = "Report failed: " + ex.GetType().Name;
                }
            }
            GUILayout.Label("Output folder: " + TrainerSettings.DirectoryPath);
            if (reportWritten) GUILayout.Label("Inspect api-candidates.txt for game API candidates.");
        }

        private void DrawSettings()
        {
            GUILayout.Label("SETTINGS", GUI.skin.box);
            GUILayout.Label("Shortcut: F10");
            GUILayout.Label("Safety: test with a backup save; transaction changes persist with the game save.");
            if (GUILayout.Button("Reset window position"))
            {
                bounds.x = settings.windowX = 80f;
                bounds.y = settings.windowY = 80f;
                Save();
            }
            GUILayout.Label("Source version: 0.3.0 (runtime unverified)");
        }

        internal void Save()
        {
            settings.windowX = bounds.x;
            settings.windowY = bounds.y;
            settings.Save();
        }
    }
}
