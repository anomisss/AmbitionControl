using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AmbitionControl.Melon
{
    // The EA 0.9 game DLLs are not distributed. Inspect only the proxy assemblies
    // MelonLoader generated from the user's *own* installed build.
    internal sealed class GameBridge
    {
        private const BindingFlags Members = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        private Type saveManagerType;
        private Type gameManagerType;
        private MethodInfo moneyMethod;
        private ConstructorInfo transactionConstructor;
        private bool resolved;
        private string mutationUnavailable = "Not inspected";
        private IntPtr undoSavePointer;
        private float undoAfter;
        private float undoDelta;
        private bool undoAvailable;

        internal string MutationStatus => mutationUnavailable;
        internal bool HasUndo => undoAvailable;

        internal void OnSceneChanged()
        {
            // An undo must not survive loading another game.
            undoAvailable = false;
        }

        private static Type FindGameType(string simpleName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name ?? "";
                if (!name.StartsWith("BigAmbitions", StringComparison.OrdinalIgnoreCase) &&
                    !name.Equals("Assembly-CSharp", StringComparison.OrdinalIgnoreCase))
                    continue;
                try
                {
                    Type candidate = assembly.GetTypes().FirstOrDefault(t => t != null && t.Name == simpleName);
                    if (candidate != null) return candidate;
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Type candidate = ex.Types.FirstOrDefault(t => t != null && t.Name == simpleName);
                    if (candidate != null) return candidate;
                }
                catch { /* Inspect the next generated assembly. */ }
            }
            return null;
        }

        private void Resolve()
        {
            if (resolved) return;
            resolved = true;
            saveManagerType = FindGameType("SaveGameManager");
            gameManagerType = FindGameType("GameManager");
            if (saveManagerType == null || gameManagerType == null)
            {
                mutationUnavailable = "EA 0.9 game types not found; game may still be loading.";
                return;
            }

            // Do not guess overloads: accepted method must match the complete signature.
            MethodInfo[] matches = gameManagerType.GetMethods(Members)
                .Where(m => m.Name == "ChangeMoneySafe" && m.ReturnType == typeof(bool))
                .Where(m =>
                {
                    ParameterInfo[] p = m.GetParameters();
                    return p.Length == 4 && p[0].ParameterType == typeof(float) &&
                           p[1].ParameterType.Name == "TransactionInfo" &&
                           p[2].ParameterType == typeof(bool) && p[3].ParameterType == typeof(bool);
                }).ToArray();
            if (matches.Length != 1)
            {
                mutationUnavailable = "ChangeMoneySafe(float, TransactionInfo, bool, bool) not uniquely available.";
                return;
            }
            MethodInfo candidateMethod = matches[0];
            Type transactionType = candidateMethod.GetParameters()[1].ParameterType;
            ConstructorInfo ctor = transactionType.GetConstructor(new[] { typeof(string) });
            if (ctor == null)
            {
                mutationUnavailable = "TransactionInfo(string) constructor not found.";
                return;
            }
            moneyMethod = candidateMethod;
            transactionConstructor = ctor;
            mutationUnavailable = "Matched exact public money API signature (runtime behavior not yet verified).";
        }

        private static object ReadMember(Type type, object instance, string name, bool isStatic)
        {
            BindingFlags flags = BindingFlags.Public |
                (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            PropertyInfo property = type.GetProperty(name, flags);
            if (property != null && property.GetIndexParameters().Length == 0)
                return property.GetValue(instance);
            FieldInfo field = type.GetField(name, flags);
            return field != null ? field.GetValue(instance) : null;
        }

        internal bool TryRead(out SaveView view, out string error)
        {
            view = null;
            error = null;
            try
            {
                Resolve();
                if (saveManagerType == null)
                {
                    // A later scene can load additional assemblies; retry on demand.
                    resolved = false;
                    error = "SaveGameManager not found. Load a playable save.";
                    return false;
                }
                object save = ReadMember(saveManagerType, null, "Current", true);
                if (save == null)
                {
                    error = "Load a playable save before using the trainer.";
                    return false;
                }
                object rawMoney = ReadMember(save.GetType(), save, "Money", false);
                if (rawMoney == null)
                {
                    error = "Money property not found for this build; write actions disabled.";
                    return false;
                }
                float balance = Convert.ToSingle(rawMoney, CultureInfo.InvariantCulture);
                if (float.IsNaN(balance) || float.IsInfinity(balance))
                {
                    error = "Invalid money value.";
                    return false;
                }
                // The proxy object can be recreated per read. Never use ReferenceEquals
                // as save identity for IL2CPP wrappers.
                IntPtr ptr = IntPtr.Zero;
                try
                {
                    object rawPointer = ReadMember(save.GetType(), save, "Pointer", false);
                    if (rawPointer is IntPtr p) ptr = p;
                }
                catch { /* Read-only display remains available. */ }

                view = new SaveView
                {
                    Save = save,
                    Pointer = ptr,
                    Balance = balance,
                    Energy = ReadOptional(save, "Energy"),
                    Hunger = ReadOptional(save, "Hunger"),
                    Happiness = ReadOptional(save, "Happiness"),
                    Day = ReadOptional(save, "Day"),
                    Hour = ReadOptional(save, "Hour"),
                    Minute = ReadOptional(save, "Minute")
                };
                return true;
            }
            catch (Exception ex)
            {
                error = "Game read failed: " + ex.GetBaseException().Message;
                return false;
            }
        }

        private static string ReadOptional(object save, string member)
        {
            try
            {
                object result = ReadMember(save.GetType(), save, member, false);
                return result == null ? "unavailable" : Convert.ToString(result, CultureInfo.InvariantCulture);
            }
            catch { return "unavailable"; }
        }

        internal bool TryApply(MoneyAction action, long amount, float expectedBefore, IntPtr expectedSave,
            out string status)
        {
            status = "Not applied.";
            if (expectedSave == IntPtr.Zero)
            {
                status = "Safe save identity unavailable; money writes disabled.";
                return false;
            }
            if (moneyMethod == null || transactionConstructor == null)
            {
                status = mutationUnavailable;
                return false;
            }
            if (!TryRead(out SaveView before, out status)) return false;
            if (before.Pointer != expectedSave || Math.Abs(before.Balance - expectedBefore) > 0.01f)
            {
                status = "Save or balance changed. Stage again.";
                return false;
            }
            if (!MoneyCommand.TryPrepare(action, before.Balance, amount, out float delta, out status))
                return false;

            try
            {
                object manager = moneyMethod.IsStatic ? null : ReadMember(gameManagerType, null, "Instance", true);
                if (!moneyMethod.IsStatic && manager == null)
                {
                    status = "GameManager.Instance unavailable.";
                    return false;
                }
                object transaction = transactionConstructor.Invoke(new object[] { "ambitioncontrol:adjustment" });
                object accepted = moneyMethod.Invoke(manager, new object[] { delta, transaction, true, false });
                if (!(accepted is bool booked) || !booked)
                {
                    status = "The game declined this transaction.";
                    return false;
                }
                if (!TryRead(out SaveView after, out string readError) || after.Pointer != before.Pointer)
                {
                    undoAvailable = false;
                    status = "Game accepted the request, but balance verification failed: " + readError;
                    return false;
                }
                float change = after.Balance - before.Balance;
                if (Math.Abs(change) < 0.5f)
                {
                    undoAvailable = false;
                    status = "API returned success without an observable balance change.";
                    return false;
                }
                undoSavePointer = after.Pointer;
                undoAfter = after.Balance;
                undoDelta = change;
                undoAvailable = true;
                status = "Balance updated to $" + after.Balance.ToString("N2", CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex)
            {
                undoAvailable = false;
                status = "Game rejected the call: " + ex.GetBaseException().Message;
                return false;
            }
        }

        internal bool TryUndo(out string status)
        {
            status = "No valid undo.";
            if (!undoAvailable) return false;
            if (!TryRead(out SaveView view, out status)) return false;
            if (view.Pointer != undoSavePointer || Math.Abs(view.Balance - undoAfter) > 0.01f)
            {
                undoAvailable = false;
                status = "Undo expired: loaded save or balance changed.";
                return false;
            }
            // Undo is a single verified reverse operation; it is not a save rollback.
            // Never use an unguarded set-money field write.
            try
            {
                object manager = moneyMethod.IsStatic ? null : ReadMember(gameManagerType, null, "Instance", true);
                if (!moneyMethod.IsStatic && manager == null)
                {
                    status = "GameManager unavailable.";
                    return false;
                }
                object transaction = transactionConstructor.Invoke(new object[] { "ambitioncontrol:adjustment" });
                bool ok = (bool)moneyMethod.Invoke(manager, new object[] { -undoDelta, transaction, true, false });
                undoAvailable = false;
                if (!ok) { status = "Undo rejected by the game."; return false; }
                if (!TryRead(out SaveView after, out status) || after.Pointer != view.Pointer)
                {
                    status = "Undo accepted, but save readback unavailable. Inspect balance.";
                    return false;
                }
                bool restored = Math.Abs(after.Balance - (undoAfter - undoDelta)) < 0.5f;
                status = restored ? "Undo restored balance to $" + after.Balance.ToString("N2", CultureInfo.InvariantCulture)
                                  : "Undo executed, but original balance not restored. Inspect save.";
                return restored;
            }
            catch (Exception ex)
            {
                undoAvailable = false;
                status = "Undo exception: " + ex.GetBaseException().Message;
                return false;
            }
        }

        internal string WriteMetadataReport()
        {
            var report = new StringBuilder();
            report.AppendLine("Ambition Control 0.4.0-alpha.1 | Target EA 0.9 build 3233");
            report.AppendLine("UTC: " + DateTime.UtcNow.ToString("u"));
            report.AppendLine("Only type names and member signatures are collected, no game values.");
            foreach (string simple in new[] { "SaveGameManager", "GameManager", "TransactionInfo" })
            {
                Type type = FindGameType(simple);
                report.AppendLine("TYPE: " + simple + " => " + (type?.FullName ?? "not found"));
                if (type == null) continue;
                foreach (MemberInfo member in type.GetMembers(Members)
                    .Where(m => m.Name.Contains("Money") || m.Name.Contains("Current") ||
                                m.Name.Contains("Change") || m.Name.Contains("Instance"))
                    .Take(40))
                    report.AppendLine("  " + member);
                if (simple == "TransactionInfo")
                    foreach (ConstructorInfo ctor in type.GetConstructors()) report.AppendLine("  " + ctor);
            }
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UserData", "AmbitionControl");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "ea-0.9.3233-api-report.txt");
            File.WriteAllText(path, report.ToString());
            return path;
        }
    }

    internal sealed class SaveView
    {
        internal object Save;
        internal IntPtr Pointer;
        internal float Balance;
        internal string Energy;
        internal string Hunger;
        internal string Happiness;
        internal string Day;
        internal string Hour;
        internal string Minute;
    }
}
