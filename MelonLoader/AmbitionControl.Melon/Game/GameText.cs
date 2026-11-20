using System.Text;
using Il2Cpp;
using Il2CppBigAmbitions.DayNightCycle;

namespace AmbitionControl.Game
{
    /// <summary>
    /// Readable text for game objects. Il2Cpp objects concatenated into a string print their type name
    /// (e.g. "Il2Cpp.Address"), so always format through these helpers.
    /// </summary>
    internal static class GameText
    {
        /// <summary>"ThirdStreet" → "Third Street".</summary>
        internal static string Words(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(s[i - 1])) sb.Append(' ');
                else if (i > 0 && char.IsDigit(c) && !char.IsDigit(s[i - 1])) sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>"45 Third Street".</summary>
        internal static string Of(Address a) => a == null ? "" : a.streetNumber + " " + Words(a.streetName.ToString());

        /// <summary>"Day 11 01:12".</summary>
        internal static string Of(Timestamp t) =>
            t == null ? "?" : "Day " + t.Day + " " + t.Hour.ToString("00") + ":" + ((int)t.Minute).ToString("00");
    }
}
