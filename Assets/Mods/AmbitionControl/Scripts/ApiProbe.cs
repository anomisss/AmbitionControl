using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AmbitionControl
{
    internal static class ApiProbe
    {
        private static readonly string[] Terms =
        {
            "Money", "Balance", "Finance", "Wallet", "Player", "Hunger",
            "Energy", "Happiness", "Time", "Clock", "Simulation"
        };

        internal static string WriteReport()
        {
            var output = new StringBuilder();
            output.AppendLine("Ambition Control v0.2.0 - public metadata discovery");
            output.AppendLine("No instance values or game saves are read.");
            output.AppendLine("UTC: " + DateTime.UtcNow.ToString("u"));
            int totalTypes = 0;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()
                .OrderBy(a => a.GetName().Name))
            {
                string name = assembly.GetName().Name;
                if (name == null || !(name.StartsWith("BigAmbitions", StringComparison.OrdinalIgnoreCase) ||
                                       name.Equals("Assembly-CSharp", StringComparison.OrdinalIgnoreCase)))
                    continue;
                output.AppendLine("ASSEMBLY: " + name);
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }
                catch (Exception ex) { output.AppendLine("Unable to inspect: " + ex.GetType().Name); continue; }

                foreach (Type type in types.OrderBy(t => t.FullName))
                {
                    if (totalTypes >= 150) break;
                    string typeName = type.FullName ?? type.Name;
                    if (!Terms.Any(t => typeName.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0))
                        continue;
                    totalTypes++;
                    output.AppendLine("TYPE: " + typeName);
                    try
                    {
                        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static |
                                                   BindingFlags.Instance | BindingFlags.DeclaredOnly;
                        foreach (var member in type.GetMembers(flags).Take(25))
                            output.AppendLine("  " + member.MemberType + " " + member.Name);
                    }
                    catch (Exception ex) { output.AppendLine("  Metadata error: " + ex.GetType().Name); }
                }
                if (totalTypes >= 150) break;
            }
            output.AppendLine("Matched types: " + totalTypes);
            Directory.CreateDirectory(TrainerSettings.DirectoryPath);
            string path = Path.Combine(TrainerSettings.DirectoryPath, "api-candidates.txt");
            File.WriteAllText(path, output.ToString());
            return path;
        }
    }
}
