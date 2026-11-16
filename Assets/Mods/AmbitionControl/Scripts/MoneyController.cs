using System;
using Entities;
using UnityEngine;

namespace AmbitionControl
{
    // Uses the game's accounting method rather than directly assigning SaveGameManager.Current.Money.
    internal sealed class MoneyController
    {
        private const string AdjustmentKey = "ambitioncontrol:transaction_adjustment";
        private object undoSave;
        private float undoAfter;
        private float undoDelta;
        private bool hasUndo;

        internal static bool TryRead(out float balance, out object saveIdentity, out string error)
        {
            balance = 0f;
            saveIdentity = null;
            error = null;
            try
            {
                var current = SaveGameManager.Current;
                if (current == null || GameManager.Instance == null)
                {
                    error = "Load a playable city save first.";
                    return false;
                }
                var cash = current.Money;
                if (float.IsNaN(cash) || float.IsInfinity(cash))
                {
                    error = "Balance could not be read safely.";
                    return false;
                }
                balance = cash;
                saveIdentity = current;
                return true;
            }
            catch (Exception ex)
            {
                error = "Game state unavailable: " + ex.GetType().Name;
                Debug.LogWarning("[AmbitionControl] Money read failed: " + ex);
                return false;
            }
        }

        internal bool TryStage(MoneyAction action, long amount, out PendingMoneyAction pending, out string error)
        {
            pending = null;
            float balance;
            object identity;
            if (!TryRead(out balance, out identity, out error)) return false;
            float delta;
            if (!MoneyCommand.TryPrepare(action, balance, amount, out delta, out error))
                return false;
            pending = new PendingMoneyAction(action, amount, delta, balance, identity);
            return true;
        }

        internal bool TryApply(PendingMoneyAction pending, out string result)
        {
            result = "Nothing staged.";
            if (pending == null) return false;
            float current;
            object identity;
            string reason;
            if (!TryRead(out current, out identity, out reason))
            {
                result = reason;
                return false;
            }
            if (!ReferenceEquals(identity, pending.SaveIdentity) ||
                Math.Abs(current - pending.Before) > 0.01f)
            {
                result = "Balance or save changed. Stage the command again.";
                return false;
            }

            float updatedDelta;
            if (!MoneyCommand.TryPrepare(pending.Action, current, pending.Amount, out updatedDelta, out reason) ||
                Math.Abs(updatedDelta - pending.Delta) > 0.01f)
            {
                result = reason ?? "Command no longer valid.";
                return false;
            }

            try
            {
                // The game's transaction API records the operation in accounting history.
                bool booked = GameManager.ChangeMoneySafe(
                    updatedDelta, new TransactionInfo(AdjustmentKey), force: true, showNotification: false);
                if (!booked)
                {
                    result = "Game rejected the transaction. No command was marked successful.";
                    return false;
                }
                float after;
                object afterSave;
                if (!TryRead(out after, out afterSave, out reason) ||
                    !ReferenceEquals(afterSave, identity))
                {
                    hasUndo = false;
                    result = "Transaction accepted but could not re-read the save. Verify it manually.";
                    return false;
                }
                float actual = after - current;
                if (Math.Abs(actual) < 0.5f)
                {
                    hasUndo = false;
                    result = "Transaction returned success but the balance did not meaningfully change.";
                    return false;
                }
                undoSave = identity;
                undoAfter = after;
                undoDelta = actual;
                hasUndo = true;
                result = "Applied $" + actual.ToString("N2") + "; balance $" + after.ToString("N2") +
                         ". Undo available until balance/save changes.";
                Debug.Log("[AmbitionControl] " + result);
                return true;
            }
            catch (Exception ex)
            {
                hasUndo = false;
                result = "Transaction failed: " + ex.GetType().Name + ". Check Player.log.";
                Debug.LogException(ex);
                return false;
            }
        }

        internal bool HasUndo => hasUndo;

        internal bool TryUndo(out string result)
        {
            result = "No transaction to undo.";
            if (!hasUndo) return false;
            float balance;
            object identity;
            string reason;
            if (!TryRead(out balance, out identity, out reason))
            {
                result = reason;
                return false;
            }
            if (!ReferenceEquals(identity, undoSave) ||
                Math.Abs(balance - undoAfter) > 0.01f)
            {
                hasUndo = false;
                result = "Undo cancelled: balance or save has changed.";
                return false;
            }
            try
            {
                bool booked = GameManager.ChangeMoneySafe(
                    -undoDelta, new TransactionInfo(AdjustmentKey), force: true, showNotification: false);
                if (!booked)
                {
                    result = "Undo rejected by the game.";
                    return false;
                }
                float after;
                object afterSave;
                bool verified = TryRead(out after, out afterSave, out reason) &&
                                ReferenceEquals(afterSave, identity);
                hasUndo = false;
                result = verified
                    ? "Reverse transaction applied; balance $" + after.ToString("N2") + "."
                    : "Reverse transaction accepted; verify balance in game.";
                Debug.Log("[AmbitionControl] " + result);
                return verified;
            }
            catch (Exception ex)
            {
                hasUndo = false;
                result = "Undo failed: " + ex.GetType().Name + ". Inspect game state.";
                Debug.LogException(ex);
                return false;
            }
        }
    }

    internal sealed class PendingMoneyAction
    {
        internal readonly MoneyAction Action;
        internal readonly long Amount;
        internal readonly float Delta;
        internal readonly float Before;
        internal readonly object SaveIdentity;

        internal PendingMoneyAction(MoneyAction action, long amount, float delta, float before, object identity)
        {
            Action = action;
            Amount = amount;
            Delta = delta;
            Before = before;
            SaveIdentity = identity;
        }
    }
}
