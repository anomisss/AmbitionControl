using System;
using System.Collections.Generic;
using System.Text;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmbitionControl.UI
{
    /// <summary>
    /// Read-only diagnostic: logs how the game's own phone app screens are built (images/sprites, colours,
    /// text styles, buttons) so Ambition Control can be restyled to match them. Each app screen is dumped
    /// once per session. Writes only to the MelonLoader log; changes nothing in the game.
    /// </summary>
    internal static class StyleProbe
    {
        private const int MaxDepth = 7;
        private const int MaxLinesPerRoot = 160;
        private static readonly HashSet<string> dumped = new HashSet<string>();
        private static bool spritesListed;

        internal static void Reset() => dumped.Clear();

        /// <summary>Dump a root (e.g. an app screen) once per session, keyed by name.</summary>
        internal static void DumpOnce(string key, Transform root)
        {
            if (root == null || dumped.Contains(key)) return;
            dumped.Add(key);
            try
            {
                var sb = new StringBuilder();
                int lines = 0;
                Walk(root, 0, sb, ref lines);
                AmbitionControlMod.Log.Msg("[StyleProbe] " + key + " (" + lines + " nodes)\n" + sb);
            }
            catch (Exception ex)
            {
                AmbitionControlMod.Log.Warning("[StyleProbe] " + key + " failed: " + ex.GetBaseException().Message);
            }
        }

        private static void Walk(Transform t, int depth, StringBuilder sb, ref int lines)
        {
            if (lines >= MaxLinesPerRoot || depth > MaxDepth) return;
            lines++;
            sb.Append(' ', depth * 2).Append(t.name);
            if (!t.gameObject.activeSelf) sb.Append(" [off]");
            try {
            RectTransform rt = t.TryCast<RectTransform>();
            if (rt != null) sb.Append(" size=").Append(rt.rect.width.ToString("0")).Append('x').Append(rt.rect.height.ToString("0"));

            } catch (Exception ex) { sb.Append(" !rect:").Append(ex.GetBaseException().Message); }
            try {
            Image img = t.GetComponent<Image>();
            if (img != null)
            {
                sb.Append(" | Image sprite=").Append(img.sprite != null ? img.sprite.name : "none")
                  .Append(" type=").Append(img.type).Append(" color=").Append(Hex(img.color));
                if (img.sprite != null && img.sprite.border != Vector4.zero) sb.Append(" border=").Append(img.sprite.border);
                if (img.pixelsPerUnitMultiplier != 1f) sb.Append(" ppuMul=").Append(img.pixelsPerUnitMultiplier.ToString("0.##"));
            }
            } catch (Exception ex) { sb.Append(" !img:").Append(ex.GetBaseException().Message); }
            try {
            TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                string text = tmp.text ?? "";
                if (text.Length > 24) text = text.Substring(0, 24) + "…";
                sb.Append(" | TMP \"").Append(text.Replace("\n", " ")).Append("\" font=").Append(tmp.font != null ? tmp.font.name : "?")
                  .Append(" size=").Append(tmp.fontSize.ToString("0.#")).Append(" color=").Append(Hex(tmp.color))
                  .Append(" style=").Append(tmp.fontStyle).Append(" align=").Append(tmp.alignment);
                if (tmp.enableAutoSizing) sb.Append(" auto=").Append(tmp.fontSizeMin.ToString("0")).Append('-').Append(tmp.fontSizeMax.ToString("0"));
            }
            } catch (Exception ex) { sb.Append(" !tmp:").Append(ex.GetBaseException().Message); }
            try {
            Button b = t.GetComponent<Button>();
            if (b != null)
            {
                ColorBlock c = b.colors;
                sb.Append(" | Button normal=").Append(Hex(c.normalColor)).Append(" hi=").Append(Hex(c.highlightedColor))
                  .Append(" press=").Append(Hex(c.pressedColor)).Append(" off=").Append(Hex(c.disabledColor))
                  .Append(" transition=").Append(b.transition);
            }
            } catch (Exception ex) { sb.Append(" !btn:").Append(ex.GetBaseException().Message); }
            try {
            if (t.GetComponent<VerticalLayoutGroup>() != null) sb.Append(" | VLayout");
            if (t.GetComponent<HorizontalLayoutGroup>() != null) sb.Append(" | HLayout");
            if (t.GetComponent<GridLayoutGroup>() != null) sb.Append(" | Grid");
            if (t.GetComponent<ScrollRect>() != null) sb.Append(" | Scroll");
            if (t.GetComponent<Shadow>() != null) sb.Append(" | Shadow");
            } catch (Exception ex) { sb.Append(" !layout:").Append(ex.GetBaseException().Message); }
            sb.Append('\n');

            for (int i = 0; i < t.childCount; i++)
            {
                if (lines >= MaxLinesPerRoot) { sb.Append(' ', (depth + 1) * 2).Append("… (truncated)\n"); return; }
                Walk(t.GetChild(i), depth + 1, sb, ref lines);
            }
        }

        /// <summary>Once per session: sprites whose names suggest reusable UI shapes.</summary>
        internal static void ListUiSprites()
        {
            if (spritesListed) return;
            spritesListed = true;
            try
            {
                var names = new SortedSet<string>();
                foreach (Sprite s in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    if (s == null) continue;
                    string n = s.name;
                    string l = n.ToLowerInvariant();
                    if (l.Contains("round") || l.Contains("button") || l.Contains("btn") || l.Contains("panel") ||
                        l.Contains("card") || l.Contains("frame") || l.Contains("bg") || l.Contains("background") ||
                        l.Contains("tab") || l.Contains("toggle") || l.Contains("pill") || l.Contains("rect"))
                        names.Add(n + (s.border != Vector4.zero ? " (9-slice " + s.border + ")" : ""));
                    if (names.Count >= 120) break;
                }
                AmbitionControlMod.Log.Msg("[StyleProbe] UI-like sprites (" + names.Count + "): " + string.Join(", ", names));
            }
            catch (Exception ex)
            {
                AmbitionControlMod.Log.Warning("[StyleProbe] sprite list failed: " + ex.GetBaseException().Message);
            }
        }

        private static string Hex(Color c) =>
            "#" + ((int)(c.r * 255)).ToString("X2") + ((int)(c.g * 255)).ToString("X2") + ((int)(c.b * 255)).ToString("X2") +
            ((int)(c.a * 255)).ToString("X2");
    }
}
