using System;
using AmbitionControl.Core;

namespace AmbitionControl.Game
{
    /// <summary>
    /// Stage -> preview -> confirm -> apply -> verify (readback) flow with a guarded one-step undo.
    /// The UI only records requests; all game calls happen in Tick(), which runs from OnUpdate on
    /// the main thread. A failed readback or exception disables money writes for the session.
    /// </summary>
    internal sealed class MoneyService
    {
        internal enum Phase { Idle, Staged, Verifying }

        internal sealed class Operation
        {
            internal MoneyAction Action;
            internal long Amount;
            internal float Before;
            internal float Delta;
            internal SaveIdentity Session;
            internal bool IsUndo;
            internal double Expected => (double)Before + Delta;
        }

        private sealed class UndoRecord
        {
            internal SaveIdentity Session;
            internal float Delta;
            internal float After;
        }

        private const int VerifyFrames = 30;

        private Operation staged;
        private Operation inFlight;
        private UndoRecord undo;
        private int verifyFramesLeft;
        private bool confirmRequested;
        private bool undoRequested;
        private bool latchedOff;

        internal Phase CurrentPhase => inFlight != null ? Phase.Verifying : staged != null ? Phase.Staged : Phase.Idle;
        internal Operation Staged => staged;
        internal string Status { get; private set; } = "Back up your save before changing money.";
        internal string LatchReason { get; private set; }
        /// <summary>Why the last Stage attempt was refused (null after a successful stage, cancel or apply).</summary>
        internal string StageError { get; private set; }

        /// <summary>Why writes are blocked right now, or null when they are allowed.</summary>
        internal string WriteBlockReason(GameSnapshot snap, bool writesEnabledPref, bool allowOtherBuilds)
        {
            if (latchedOff) return "Disabled for this session: " + LatchReason;
            if (!writesEnabledPref) return "Money writes are turned off in preferences (MoneyWritesEnabled).";
            int? build = GameBridge.BuildNumber;
            if (!build.HasValue) return "Game build not detected yet.";
            if (build.Value != GameBridge.TargetBuild && !allowOtherBuilds)
                return "Game build " + build.Value + " is not the verified build " + GameBridge.TargetBuild + ".";
            if (snap == null || !snap.Loaded) return snap?.NotLoadedReason ?? "No save loaded.";
            if (snap.Saving) return "Game is saving; wait a moment.";
            if (inFlight != null) return "Verifying previous change...";
            return null;
        }

        internal bool CanUndo(GameSnapshot snap) =>
            undo != null && inFlight == null && staged == null && snap != null && snap.SameSession(undo.Session) &&
            MoneyCommand.BalanceMatches(snap.Money, undo.After);

        internal void Stage(MoneyAction action, long amount, GameSnapshot snap)
        {
            confirmRequested = false;
            if (!MoneyCommand.TryPrepare(action, snap.Money, amount, out float delta, out string error))
            {
                staged = null;
                StageError = error;
                Status = error;
                return;
            }
            staged = new Operation
            {
                Action = action, Amount = amount, Before = snap.Money, Delta = delta, Session = snap.Identity()
            };
            StageError = null;
            Status = "Review the preview, then Confirm.";
        }

        internal void Cancel()
        {
            StageError = null;
            staged = null;
            confirmRequested = false;
            Status = "Cancelled.";
        }

        internal void ClearStageError() { StageError = null; }
        internal void RequestConfirm() { if (staged != null) confirmRequested = true; }
        internal void RequestUndo() { undoRequested = true; }

        internal void ResetForSessionChange()
        {
            staged = null;
            inFlight = null;
            undo = null;
            confirmRequested = false;
            undoRequested = false;
        }

        /// <summary>Main-thread step. Call once per frame with a fresh snapshot.</summary>
        internal void Tick(GameSnapshot snap, bool writesEnabledPref, bool allowOtherBuilds)
        {
            if (inFlight != null)
            {
                Verify();
                return;
            }

            if (staged != null && (snap == null || !snap.SameSession(staged.Session)))
            {
                staged = null;
                confirmRequested = false;
                Status = "Save changed; staged action discarded.";
            }

            if (confirmRequested && staged != null)
            {
                confirmRequested = false;
                Operation op = staged;
                staged = null;
                string block = WriteBlockReason(snap, writesEnabledPref, allowOtherBuilds);
                if (block != null) { Status = "Not applied: " + block; return; }
                if (!MoneyCommand.BalanceMatches(snap.Money, op.Before))
                {
                    Status = "Balance changed since preview ($" + snap.Money.ToString("N2") + "). Stage again.";
                    return;
                }
                Apply(op);
                return;
            }

            if (undoRequested)
            {
                undoRequested = false;
                string block = WriteBlockReason(snap, writesEnabledPref, allowOtherBuilds);
                if (block != null) { Status = "Undo not applied: " + block; return; }
                if (!CanUndo(snap))
                {
                    undo = null;
                    Status = "Undo expired (balance or save changed since the last action).";
                    return;
                }
                Apply(new Operation
                {
                    Action = undo.Delta >= 0 ? MoneyAction.Remove : MoneyAction.Add,
                    Amount = (long)Math.Round(Math.Abs(undo.Delta)),
                    Before = snap.Money,
                    Delta = -undo.Delta,
                    Session = snap.Identity(),
                    IsUndo = true
                });
            }
        }

        private void Apply(Operation op)
        {
            bool accepted;
            try
            {
                accepted = GameBridge.ChangeMoney(op.Delta);
            }
            catch (Exception ex)
            {
                Exception root = ex.GetBaseException();
                Latch("game call threw " + root.GetType().Name + ": " + root.Message);
                return;
            }
            if (!accepted)
            {
                Status = "Rejected by the game (ChangeMoneySafe returned false). Balance unchanged.";
                AmbitionControlMod.Log.Msg("ChangeMoneySafe rejected delta " + op.Delta.ToString("F2"));
                return;
            }
            inFlight = op;
            verifyFramesLeft = VerifyFrames;
            Status = "Applied; verifying balance...";
            Verify();
        }

        private void Verify()
        {
            Operation op = inFlight;
            float money;
            bool read;
            try
            {
                read = GameBridge.TryReadMoney(op.Session, out money);
            }
            catch (Exception ex)
            {
                inFlight = null;
                Latch("readback threw " + ex.GetBaseException().Message);
                return;
            }

            if (read && MoneyCommand.BalanceMatches(money, op.Expected))
            {
                inFlight = null;
                if (op.IsUndo)
                {
                    undo = null;
                    Status = "Undo verified. Balance $" + money.ToString("N2") + ".";
                }
                else
                {
                    undo = new UndoRecord { Session = op.Session, Delta = op.Delta, After = money };
                    Status = "Verified: balance $" + op.Before.ToString("N2") + " -> $" + money.ToString("N2") + ".";
                }
                AmbitionControlMod.Log.Msg((op.IsUndo ? "Undo" : op.Action.ToString()) + " verified: " +
                    op.Before.ToString("F2") + " -> " + money.ToString("F2"));
                return;
            }

            if (!read)
            {
                inFlight = null;
                Status = "Save unloaded during verification; result unknown. Check the in-game balance.";
                return;
            }

            if (--verifyFramesLeft <= 0)
            {
                inFlight = null;
                Latch("readback mismatch (expected $" + op.Expected.ToString("N2") + ", read $" + money.ToString("N2") + ")");
            }
        }

        private void Latch(string reason)
        {
            latchedOff = true;
            LatchReason = reason;
            undo = null;
            Status = "Money writes disabled: " + reason;
            AmbitionControlMod.Log.Error("Money writes disabled for this session: " + reason);
        }
    }
}
