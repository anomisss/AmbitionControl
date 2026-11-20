using System;
using AmbitionControl.Game;
using AmbitionControl.UI;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(AmbitionControl.AmbitionControlMod), "Ambition Control", "0.4.0-alpha.1", "anomisss")]
[assembly: MelonGame("Hovgaard Games", "Big Ambitions")]

namespace AmbitionControl
{
    internal sealed class Settings
    {
        internal string ToggleKey = "F10";
        internal bool MoneyWritesEnabled = true;
        internal bool AllowOtherBuilds;
    }

    public sealed class AmbitionControlMod : MelonMod
    {
        internal const string VersionLabel = "v0.4.0-alpha.1";
        internal static MelonLogger.Instance Log;

        private const float SnapshotInterval = 0.25f;

        private MelonPreferences_Category prefs;
        private MelonPreferences_Entry<string> prefKey;
        private MelonPreferences_Entry<bool> prefWrites;
        private MelonPreferences_Entry<bool> prefOtherBuilds;
        private MelonPreferences_Entry<float> prefX;
        private MelonPreferences_Entry<float> prefY;

        private readonly Settings settings = new Settings();
        private readonly MoneyService money = new MoneyService();
        private TrainerMenu menu;
        private GameSnapshot snapshot = new GameSnapshot { NotLoadedReason = "Starting..." };
        private float nextSnapshot;
        private Key toggleKey = Key.F10;
        private bool loggedBuild;
        private bool dragBroken;
        private string lastUpdateError;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            prefs = MelonPreferences.CreateCategory("AmbitionControl", "Ambition Control");
            prefKey = prefs.CreateEntry("ToggleKey", "F10", description: "Input System key name that opens the menu (e.g. F10, F8, Insert).");
            prefWrites = prefs.CreateEntry("MoneyWritesEnabled", true, description: "Allow money changes. Set false for a read-only menu.");
            prefOtherBuilds = prefs.CreateEntry("AllowOtherBuilds", false, description: "Allow money changes on game builds other than 3233 (unverified, at your own risk).");
            prefX = prefs.CreateEntry("WindowX", 20f, is_hidden: true);
            prefY = prefs.CreateEntry("WindowY", 80f, is_hidden: true);

            settings.ToggleKey = prefKey.Value;
            settings.MoneyWritesEnabled = prefWrites.Value;
            settings.AllowOtherBuilds = prefOtherBuilds.Value;
            if (!Enum.TryParse(prefKey.Value, true, out toggleKey))
            {
                Log.Warning("Unknown ToggleKey '" + prefKey.Value + "'; using F10.");
                toggleKey = Key.F10;
                settings.ToggleKey = "F10";
            }

            menu = new TrainerMenu(money) { X = prefX.Value, Y = prefY.Value };
            Log.Msg(VersionLabel + " loaded. Press " + settings.ToggleKey + " in-game to open the menu.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            money.ResetForSessionChange();
            snapshot = new GameSnapshot { NotLoadedReason = "Scene loading..." };
            nextSnapshot = 0f;
        }

        public override void OnUpdate()
        {
            try
            {
                if (!loggedBuild)
                {
                    GameBridge.TryResolveBuild();
                    if (GameBridge.BuildNumber.HasValue)
                    {
                        loggedBuild = true;
                        int b = GameBridge.BuildNumber.Value;
                        if (b == GameBridge.TargetBuild) Log.Msg("Game build " + b + " matches the verified target.");
                        else Log.Warning("Game build " + b + " differs from verified build " + GameBridge.TargetBuild +
                                         "; money writes are blocked unless AllowOtherBuilds=true.");
                    }
                }

                Keyboard kb = Keyboard.current;
                if (kb != null && kb[toggleKey].wasPressedThisFrame)
                {
                    menu.Visible = !menu.Visible;
                    if (!menu.Visible) SaveWindow();
                    nextSnapshot = 0f;
                }

                if (menu.Visible && !dragBroken)
                {
                    try
                    {
                        if (menu.UpdateDrag(Mouse.current)) SaveWindow();
                    }
                    catch (Exception ex)
                    {
                        dragBroken = true;
                        Log.Warning("Window dragging disabled: " + ex.GetBaseException().Message);
                    }
                }

                // Read game state only while the menu is open or an operation needs it.
                bool needsState = menu.Visible || money.CurrentPhase != MoneyService.Phase.Idle;
                if (needsState && Time.unscaledTime >= nextSnapshot)
                {
                    GameSnapshot fresh = GameBridge.Capture();
                    if (snapshot.Loaded && !fresh.SameSession(snapshot.Identity()))
                        money.ResetForSessionChange();
                    snapshot = fresh;
                    nextSnapshot = Time.unscaledTime + SnapshotInterval;
                }

                money.Tick(snapshot, settings.MoneyWritesEnabled, settings.AllowOtherBuilds);
                if (money.CurrentPhase == MoneyService.Phase.Verifying) nextSnapshot = 0f;
                lastUpdateError = null;
            }
            catch (Exception ex)
            {
                // Log each distinct error once instead of every frame.
                string msg = ex.GetBaseException().ToString();
                if (msg != lastUpdateError)
                {
                    lastUpdateError = msg;
                    Log.Error("OnUpdate: " + msg);
                }
            }
        }

        public override void OnGUI()
        {
            if (menu == null || !menu.Visible) return;
            try
            {
                menu.Draw(snapshot, settings);
            }
            catch (Exception ex)
            {
                menu.Visible = false;
                Log.Error("Menu closed after a drawing error: " + ex.GetBaseException());
            }
        }

        public override void OnDeinitializeMelon() => SaveWindow();

        private void SaveWindow()
        {
            if (menu == null || prefX == null) return;
            prefX.Value = menu.X;
            prefY.Value = menu.Y;
            MelonPreferences.Save();
        }
    }
}
