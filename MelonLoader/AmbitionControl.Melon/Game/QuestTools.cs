using System;
using Il2Cpp;
using Il2CppHelpers;
using Il2CppTutorial;
using UnityEngine;

namespace AmbitionControl.Game
{
    internal enum TeleportTarget { QuestTarget, GpsDestination }

    /// <summary>
    /// Quest + teleport tools (Milestone F part 2). Build-3233 interop evidence (Il2CppBigAmbitions.dll):
    ///   TutorialHelper.IsTutorialEnabled(), IsTutorialFinished(), currentQuest : Quest {Id}, currentQuestEntry : QuestEntry {Id, target}
    ///   TutorialHelper.Command_CompleteObjective(), Command_CompleteQuest()   — the game's own debug commands
    ///   QuestEntry.IsAlreadyCompleted(bool sideQuest)                         — readback
    ///   GameManager.Command_TeleportPlayerToQuestTarget(), Command_TeleportPlayerToDestination()
    ///   PlayerHelper.GetPosition() : Vector3, PlayerHelper.IsUsingVehicle, BuildingManager.IsInsideBuilding — guard + readback
    /// Teleport is verified a moment later by checking the player actually moved.
    /// Completing objectives/quests can't be undone, so both need a second press.
    /// </summary>
    internal static class QuestTools
    {
        internal static string Status { get; private set; }
        internal static bool PendingObjective { get; private set; }
        internal static bool PendingQuest { get; private set; }

        private const int TeleportVerifyFrames = 90;
        private const float MinMoveMeters = 2f;
        private static int teleportFramesLeft;
        private static Vector3 teleportFrom;
        private static TeleportTarget teleportKind;

        internal static string BlockReason(GameSnapshot snap, Settings settings)
        {
            if (!settings.QuestToolsEnabled) return "Quest tools are turned off in preferences (QuestToolsEnabled).";
            int? build = GameBridge.BuildNumber;
            if (!build.HasValue) return "Game build not detected yet.";
            if (build.Value != GameBridge.TargetBuild && !settings.AllowOtherBuilds)
                return "Game build " + build.Value + " is not the verified build " + GameBridge.TargetBuild + ".";
            if (snap == null || !snap.Loaded) return snap?.NotLoadedReason ?? "No save loaded.";
            if (snap.Saving) return "Game is saving; wait a moment.";
            return null;
        }

        /// <summary>Why quest completion is unavailable right now (null = available).</summary>
        internal static string QuestBlock()
        {
            try
            {
                if (!TutorialHelper.IsTutorialEnabled()) return "The tutorial/quest line is turned off in this save.";
                if (TutorialHelper.IsTutorialFinished()) return "All quests are already finished.";
                if (TutorialHelper.currentQuestEntry == null) return "No current objective.";
                return null;
            }
            catch (Exception ex) { return "Quest system unavailable: " + ex.GetBaseException().Message; }
        }

        /// <summary>Why teleport is unavailable right now (null = available).</summary>
        internal static string TeleportBlock(TeleportTarget target)
        {
            try
            {
                if (teleportFramesLeft > 0) return "Previous teleport is still being checked.";
                if (PlayerHelper.IsUsingVehicle) return "Get out of the vehicle first.";
                if (BuildingManager.IsInsideBuilding) return "Go outside first (teleport from inside a building is not supported).";
                if (target == TeleportTarget.QuestTarget)
                {
                    string q = QuestBlock();
                    if (q != null) return q;
                    if (TutorialHelper.currentQuestEntry.target == null) return "The current objective has no location.";
                }
                return null;
            }
            catch (Exception ex) { return "Teleport unavailable: " + ex.GetBaseException().Message; }
        }

        internal static string CurrentObjectiveText()
        {
            try
            {
                if (!TutorialHelper.IsTutorialEnabled()) return "Quests: tutorial is off in this save.";
                if (TutorialHelper.IsTutorialFinished()) return "Quests: all finished.";
                Quest q = TutorialHelper.currentQuest;
                QuestEntry e = TutorialHelper.currentQuestEntry;
                return "Quest: " + (q != null ? q.Id : "none") + "\nObjective: " + (e != null ? e.Id : "none");
            }
            catch (Exception ex) { return "Quests: unavailable (" + ex.GetBaseException().Message + ")"; }
        }

        internal static void Teleport(TeleportTarget target, GameSnapshot snap, Settings settings)
        {
            ClearPending();
            string block = BlockReason(snap, settings) ?? TeleportBlock(target);
            if (block != null) { Status = "Not teleported: " + block; return; }
            try
            {
                teleportFrom = PlayerHelper.GetPosition();
                teleportKind = target;
                if (target == TeleportTarget.QuestTarget) GameManager.Command_TeleportPlayerToQuestTarget();
                else GameManager.Command_TeleportPlayerToDestination();
                teleportFramesLeft = TeleportVerifyFrames;
                Status = "Teleporting to " + Label(target) + "...";
                AmbitionControlMod.Log.Msg("Teleport to " + Label(target) + " requested from " + teleportFrom);
            }
            catch (Exception ex)
            {
                teleportFramesLeft = 0;
                Status = "Teleport failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        /// <summary>Called every frame; verifies a requested teleport by the distance moved.</summary>
        internal static void Tick()
        {
            if (teleportFramesLeft <= 0) return;
            try
            {
                Vector3 now = PlayerHelper.GetPosition();
                float moved = Vector3.Distance(teleportFrom, now);
                if (moved >= MinMoveMeters)
                {
                    teleportFramesLeft = 0;
                    Status = "Teleported to " + Label(teleportKind) + " (verified, moved " + moved.ToString("0") + " m).";
                    AmbitionControlMod.Log.Msg("Teleport verified: " + Label(teleportKind) + ", moved " + moved.ToString("0.0") + " m to " + now);
                    return;
                }
                if (--teleportFramesLeft == 0)
                {
                    Status = "Teleport to " + Label(teleportKind) + ": position did not change" +
                             (teleportKind == TeleportTarget.GpsDestination ? " (is a GPS destination set?)." : ".");
                    AmbitionControlMod.Log.Warning("Teleport not confirmed: " + Label(teleportKind) + ", moved " + moved.ToString("0.0") + " m");
                }
            }
            catch (Exception ex)
            {
                teleportFramesLeft = 0;
                Status = "Teleport check failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        internal static void CompleteObjectivePressed(GameSnapshot snap, Settings settings)
        {
            PendingQuest = false;
            string block = BlockReason(snap, settings) ?? QuestBlock();
            if (block != null) { PendingObjective = false; Status = "Objective not completed: " + block; return; }
            QuestEntry entry = TutorialHelper.currentQuestEntry;
            if (!PendingObjective)
            {
                PendingObjective = true;
                Status = "Complete objective '" + entry.Id + "'? The game runs its completion actions (messages, unlocks). " +
                         "Cannot be undone. Press Confirm.";
                return;
            }
            PendingObjective = false;
            try
            {
                string before = entry.Id;
                TutorialHelper.Command_CompleteObjective();
                bool done = entry.IsAlreadyCompleted(false);
                QuestEntry next = TutorialHelper.currentQuestEntry;
                bool moved = next == null || next.Pointer != entry.Pointer;
                Status = done || moved
                    ? "Objective '" + before + "' completed (verified)." + (next != null ? " Next: " + next.Id : "")
                    : "The game did not report objective '" + before + "' as completed.";
                if (done || moved) AmbitionControlMod.Log.Msg("Objective completed (verified): " + before + " -> " + (next != null ? next.Id : "none"));
                else AmbitionControlMod.Log.Warning("Objective completion not confirmed: " + before);
            }
            catch (Exception ex)
            {
                Status = "Completing objective failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        internal static void CompleteQuestPressed(GameSnapshot snap, Settings settings)
        {
            PendingObjective = false;
            string block = BlockReason(snap, settings) ?? QuestBlock();
            if (block != null) { PendingQuest = false; Status = "Quest not completed: " + block; return; }
            Quest quest = TutorialHelper.currentQuest;
            string qid = quest != null ? quest.Id : "current";
            if (!PendingQuest)
            {
                PendingQuest = true;
                Status = "Complete the whole quest '" + qid + "' (all remaining objectives)? Cannot be undone. Press Confirm.";
                return;
            }
            PendingQuest = false;
            try
            {
                IntPtr before = quest != null ? quest.Pointer : IntPtr.Zero;
                TutorialHelper.Command_CompleteQuest();
                Quest now = TutorialHelper.currentQuest;
                bool advanced = TutorialHelper.IsTutorialFinished() || now == null || now.Pointer != before;
                Status = advanced
                    ? "Quest '" + qid + "' completed (verified)." + (now != null && now.Pointer != before ? " Next quest: " + now.Id : "")
                    : "The game did not move past quest '" + qid + "'.";
                if (advanced) AmbitionControlMod.Log.Msg("Quest completed (verified): " + qid + " -> " + (now != null ? now.Id : "none"));
                else AmbitionControlMod.Log.Warning("Quest completion not confirmed: " + qid);
            }
            catch (Exception ex)
            {
                Status = "Completing quest failed: " + ex.GetBaseException().Message;
                AmbitionControlMod.Log.Error(Status);
            }
        }

        internal static string Label(TeleportTarget t) => t == TeleportTarget.QuestTarget ? "quest target" : "GPS destination";

        internal static void ClearPending()
        {
            PendingObjective = false;
            PendingQuest = false;
        }

        internal static void ResetSession()
        {
            ClearPending();
            teleportFramesLeft = 0;
        }
    }
}
