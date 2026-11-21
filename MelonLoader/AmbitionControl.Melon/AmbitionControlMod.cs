using System;
using AmbitionControl.Game;
using AmbitionControl.UI;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(AmbitionControl.AmbitionControlMod), "Ambition Control", "0.7.0-alpha.15", "anomisss")]
[assembly: MelonGame("Hovgaard Games", "Big Ambitions")]

namespace AmbitionControl
{
    internal sealed class Settings
    {
        internal string ToggleKey = "F10";
        internal bool MoneyWritesEnabled = true;
        internal bool AllowOtherBuilds;
        internal bool PlayerTogglesEnabled = true;
        internal bool PlayerToolsEnabled = true;
        internal bool EconomyControlsEnabled = true;
        internal bool WorldToolsEnabled = true;
        internal bool EmployeeToolsEnabled = true;
        internal bool RivalToolsEnabled = true;
        internal bool QuestToolsEnabled = true;
        internal bool BusinessToolsEnabled = true;
    }

    public sealed class AmbitionControlMod : MelonMod
    {
        internal const string VersionLabel = "v0.7.0-alpha.15";
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
        private PhoneApp phone;
        private GameSnapshot snapshot = new GameSnapshot { NotLoadedReason = "Starting..." };
        private float nextSnapshot;
        private Key toggleKey = Key.F10;
        private bool loggedBuild;
        private bool dragBroken;
        private bool blockClickThrough;
        private string lastStateSummary;
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

            MelonPreferences_Entry<bool> prefBlock = prefs.CreateEntry("BlockClickThrough", true,
                description: "Stop mouse clicks/scroll on the menu from also acting on the game world.");
            MelonPreferences_Entry<bool> prefPhone = prefs.CreateEntry("PhoneApp", true,
                description: "Add an Ambition Control app to the in-game smartphone.");
            if (prefPhone.Value) phone = new PhoneApp(money);
            settings.PlayerTogglesEnabled = prefs.CreateEntry("PlayerTogglesEnabled", true,
                description: "Show the game option switches (energy/happiness drain, aging, vehicle damage/fuel, wholesale limits) in the phone app.").Value;
            settings.PlayerToolsEnabled = prefs.CreateEntry("PlayerToolsEnabled", true,
                description: "Show the Player tab (need refills, invincibility, hold hunger).").Value;
            settings.EconomyControlsEnabled = prefs.CreateEntry("EconomyControlsEnabled", true,
                description: "Show the Economy tab (tax, prices, salaries, rival difficulty, presets).").Value;
            settings.WorldToolsEnabled = prefs.CreateEntry("WorldToolsEnabled", true,
                description: "Show the World tab (vehicle repair/refuel, pause, speed, traffic, save).").Value;
            settings.EmployeeToolsEnabled = prefs.CreateEntry("EmployeeToolsEnabled", true,
                description: "Show the Staff tab (employee list, temporary max satisfaction).").Value;
            settings.RivalToolsEnabled = prefs.CreateEntry("RivalToolsEnabled", true,
                description: "Show the Rivals tab (view rivals; defeat with confirmation).").Value;
            settings.BusinessToolsEnabled = prefs.CreateEntry("BusinessToolsEnabled", true,
                description: "Show the Business tab (your businesses, customer satisfaction, teleport to a business).").Value;
            settings.QuestToolsEnabled = prefs.CreateEntry("QuestToolsEnabled", true,
                description: "Show the Quest tab (teleport to quest target / GPS destination; complete objective or quest with confirmation).").Value;

            RevenueBoost.Init(prefs, HarmonyInstance, settings);
            SatisfactionHold.Apply(HarmonyInstance);

            blockClickThrough = prefBlock.Value;
            if (blockClickThrough) InputBlocker.Apply(HarmonyInstance);
            else Log.Msg("Click-through blocking is off (BlockClickThrough=false).");
            Log.Msg(VersionLabel + " loaded. Open the \"Ambition Control\" app on the in-game phone" +
                    (phone == null ? " (disabled)" : "") + ", or press " + settings.ToggleKey + " for the overlay.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            money.ResetForSessionChange();
            InputBlocker.RestoreActions();
            phone?.Reset();
            WorldTools.ResetScene();
            EmployeeTools.ResetSession();
            RivalTools.ClearPending();
            TimeSkip.ResetScene();
            AgeTools.ClearPending();
            BusinessTools.ResetSession();
            HiringTools.ClearPending();
            VehicleSpawn.ClearPending();
            SatisfactionHold.ResetSession();
            RevenueBoost.ResetSession();
            QuestTools.ResetSession();
            PlayerTools.ResetSession();
            Log.Msg("Scene loaded: " + sceneName + " (staged action and undo cleared).");
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
                        if (menu.UpdateMouse(Mouse.current)) SaveWindow();
                    }
                    catch (Exception ex)
                    {
                        dragBroken = true;
                        Log.Warning("Menu mouse input disabled (clicks and dragging): " + ex.GetBaseException().Message);
                    }
                }

                InputBlocker.MenuVisible = menu.Visible;
                InputBlocker.MenuX = menu.X;
                InputBlocker.MenuY = menu.Y;
                InputBlocker.MenuW = TrainerMenu.W;
                InputBlocker.MenuH = TrainerMenu.H;
                InputBlocker.Dragging = menu.IsDragging;
                InputBlocker.Tick(blockClickThrough);

                // Read game state only while the menu is open or an operation needs it.
                bool needsState = menu.Visible || phone != null || money.CurrentPhase != MoneyService.Phase.Idle;
                if (needsState && Time.unscaledTime >= nextSnapshot)
                {
                    GameSnapshot fresh = GameBridge.Capture();
                    if (snapshot.Loaded && !fresh.SameSession(snapshot.Identity()))
                    {
                        PlayerTools.ResetSession();
                        SatisfactionHold.ResetSession();
                        RevenueBoost.ResetSession();
                        money.ResetForSessionChange();
                        Log.Msg("Session ended (save closed or switched); staged action and undo cleared.");
                    }
                    if (fresh.Loaded && !(snapshot.Loaded && fresh.SameSession(snapshot.Identity())))
                        Log.Msg("Session: save loaded, balance $" + fresh.Money.ToString("N2") + ", day " + fresh.Day + ".");
                    snapshot = fresh;
                    nextSnapshot = Time.unscaledTime + SnapshotInterval;
                }

                // Log state transitions (not every frame) so in-game reports can be diagnosed from the log.
                string state = snapshot.Loaded
                    ? "save loaded, balance " + snapshot.Money.ToString("N2")
                    : "not loaded: " + snapshot.NotLoadedReason;
                string block = money.WriteBlockReason(snapshot, settings.MoneyWritesEnabled, settings.AllowOtherBuilds);
                string summary = (snapshot.Loaded ? "save loaded" : state) + " | writes " + (block == null ? "allowed" : "blocked: " + block);
                if (menu.Visible && summary != lastStateSummary)
                {
                    lastStateSummary = summary;
                    Log.Msg("State: " + summary);
                }

                money.Tick(snapshot, settings.MoneyWritesEnabled, settings.AllowOtherBuilds);
                phone?.Update(snapshot, settings);
                PlayerTools.Tick(snapshot, settings);
                TimeSkip.Tick();
                RevenueBoost.Tick();
                SatisfactionHold.Tick(snapshot, settings);
                QuestTools.Tick();
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

        public override void OnDeinitializeMelon()
        {
            InputBlocker.RestoreActions();
            SaveWindow();
        }

        private void SaveWindow()
        {
            if (menu == null || prefX == null) return;
            prefX.Value = menu.X;
            prefY.Value = menu.Y;
            MelonPreferences.Save();
        }
    }
}
