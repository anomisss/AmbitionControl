using System;
using UnityEngine;

namespace AmbitionControl
{
    // Source-only UI. No game state mutators are connected in v0.2.
    internal sealed class TrainerMenu
    {
        private const int WindowId = 1734282;
        private static readonly string[] Tabs = { "Money", "Player", "Time", "Diagnostics", "Settings" };
        private readonly TrainerSettings settings;
        private Rect bounds;
        private Vector2 scroll;
        private string amount = "100000";
        private string notification = "Game-changing commands are disabled until an adapter is verified.";
        private bool wantsConfirmation;
        private bool reportWritten;

        internal TrainerMenu()
        {
            settings = TrainerSettings.Load();
            bounds = new Rect(settings.windowX, settings.windowY, 580f, 465f);
            settings.selectedTab = Mathf.Clamp(settings.selectedTab, 0, Tabs.Length - 1);
        }

        internal void Render()
        {
            var evt = Event.current;
            if (evt != null && evt.type == EventType.KeyDown && evt.keyCode == KeyCode.F10)
            {
                settings.visible = !settings.visible;
                wantsConfirmation = false;
                Save();
                evt.Use();
            }
            if (!settings.visible) return;

            bounds.width = Mathf.Min(580f, Mathf.Max(180f, Screen.width - 12f));
            bounds.height = Mathf.Min(465f, Mathf.Max(140f, Screen.height - 12f));
            bounds.x = Mathf.Clamp(bounds.x, 0f, Mathf.Max(0f, Screen.width - bounds.width));
            bounds.y = Mathf.Clamp(bounds.y, 0f, Mathf.Max(0f, Screen.height - bounds.height));

            bounds = GUILayout.Window(WindowId, bounds, DrawWindow, "AMBITION CONTROL  |  v0.2.0",
                GUILayout.Width(bounds.width), GUILayout.Height(bounds.height));

            // IMGUI input isolation only: game input systems may require extra integration.
            if (evt != null && evt.isMouse && bounds.Contains(evt.mousePosition) &&
                evt.type != EventType.Used && evt.type != EventType.Repaint && evt.type != EventType.Layout)
            {
                evt.Use();
            }

            if (evt != null && evt.rawType == EventType.MouseUp) Save();
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("F10 to toggle menu");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", GUILayout.Width(30)))
            {
                settings.visible = false;
                wantsConfirmation = false;
                Save();
            }
            GUILayout.EndHorizontal();

            int selected = GUILayout.Toolbar(settings.selectedTab, Tabs);
            if (selected != settings.selectedTab)
            {
                settings.selectedTab = selected;
                wantsConfirmation = false;
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
            GUILayout.Label("Balance: unavailable (game adapter not yet connected).");
            GUILayout.Label("Whole-dollar amount:");
            amount = GUILayout.TextField(amount, 18);
            long parsed;
            string error;
            bool valid = MoneyCommand.TryParse(amount, out parsed, out error);
            if (!valid) GUILayout.Label(error);
            bool old = GUI.enabled;
            GUI.enabled = false;
            GUILayout.BeginHorizontal();
            GUILayout.Button("Add money");
            GUILayout.Button("Set balance");
            GUILayout.EndHorizontal();
            GUI.enabled = old;
            GUILayout.Label("These buttons stay locked until we can read, validate and safely modify the real balance.");
        }

        private void DrawPlayer()
        {
            GUILayout.Label("PLAYER NEEDS", GUI.skin.box);
            GUILayout.Label("Planned: hunger, energy and happiness controls.");
            bool old = GUI.enabled;
            GUI.enabled = false;
            GUILayout.Button("Freeze hunger");
            GUILayout.Button("Freeze energy");
            GUILayout.Button("Freeze happiness");
            GUI.enabled = old;
        }

        private void DrawTime()
        {
            GUILayout.Label("WORLD TIME", GUI.skin.box);
            GUILayout.Label("Simulation speed control needs a verified game-time API.");
            bool old = GUI.enabled;
            GUI.enabled = false;
            GUILayout.BeginHorizontal();
            GUILayout.Button("Pause");
            GUILayout.Button("1x");
            GUILayout.Button("2x");
            GUILayout.Button("4x");
            GUILayout.EndHorizontal();
            GUI.enabled = old;
        }

        private void DrawDiagnostics()
        {
            GUILayout.Label("DIAGNOSTICS", GUI.skin.box);
            GUILayout.Label("Metadata-only candidate report. No player values or game saves are read.");
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
            if (reportWritten) GUILayout.Label("Inspect api-candidates.txt for real integration candidates.");
        }

        private void DrawSettings()
        {
            GUILayout.Label("SETTINGS", GUI.skin.box);
            GUILayout.Label("Shortcut: F10");
            GUILayout.Label("This build is source-only and must be compiled by the official SDK.");
            GUILayout.Label("No real gameplay cheats are active.");
            if (GUILayout.Button("Reset window position"))
            {
                bounds.x = settings.windowX = 80f;
                bounds.y = settings.windowY = 80f;
                Save();
            }
            GUILayout.Label("Source version: 0.2.0");
        }

        internal void Save()
        {
            settings.windowX = bounds.x;
            settings.windowY = bounds.y;
            settings.Save();
        }
    }
}
