using System;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using Il2CppBigAmbitions.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmbitionControl.Game
{
    /// <summary>
    /// Stops mouse clicks/scroll on the Ambition Control menu from also acting on the game world.
    ///
    /// The game (build 3233) reads mouse input through the static helper
    /// Il2CppBigAmbitions.InputSystem.InputActionHelper (confirmed in the generated interop):
    ///   WasLeftClickPressedThisFrame / WasLeftClickReleasedThisFrame / WasRightClickPressedThisFrame /
    ///   IsRightClickBeingPressedThisFrame / WasRightClickReleasedThisFrame, GetScrollValue (getter),
    ///   Pressed / Pressing / Released / Value(PlayerAction).
    /// Harmony prefixes make those report "no input" while the cursor is over the open menu, but only
    /// for mouse actions (Click, RightClick, Zoom, CursorScroll); keyboard actions are untouched.
    /// Each patch is applied independently; failures are logged and the mod keeps working.
    /// Limitation: game code that reads the mouse without this helper (or where IL2CPP inlined the
    /// helper) is not filtered.
    /// </summary>
    internal static class InputBlocker
    {
        // Menu rectangle in IMGUI (top-left origin) coordinates, published by the menu each frame.
        internal static bool MenuVisible;
        internal static float MenuX, MenuY, MenuW, MenuH;
        internal static bool Dragging;

        internal static int Applied { get; private set; }
        internal static int Failed { get; private set; }

        internal static bool PointerOverMenu()
        {
            if (!MenuVisible) return false;
            if (Dragging) return true;
            try
            {
                Mouse mouse = Mouse.current;
                if (mouse == null) return false;
                Vector2 p = mouse.position.ReadValue();
                float gx = p.x;
                float gy = Screen.height - p.y;
                return gx >= MenuX && gx <= MenuX + MenuW && gy >= MenuY && gy <= MenuY + MenuH;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsMouseAction(PlayerAction action) =>
            action == PlayerAction.Click || action == PlayerAction.RightClick ||
            action == PlayerAction.Zoom || action == PlayerAction.CursorScroll;

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            Type helper = typeof(InputActionHelper);
            Type self = typeof(InputBlocker);
            const BindingFlags S = BindingFlags.Public | BindingFlags.Static;

            foreach (string name in new[]
                     {
                         nameof(InputActionHelper.WasLeftClickPressedThisFrame),
                         nameof(InputActionHelper.WasLeftClickReleasedThisFrame),
                         nameof(InputActionHelper.WasRightClickPressedThisFrame),
                         nameof(InputActionHelper.IsRightClickBeingPressedThisFrame),
                         nameof(InputActionHelper.WasRightClickReleasedThisFrame)
                     })
                Patch(harmony, helper.GetMethod(name, S, null, Type.EmptyTypes, null), self, nameof(BoolPrefix), name);

            Patch(harmony, helper.GetProperty(nameof(InputActionHelper.GetScrollValue), S)?.GetGetMethod(),
                self, nameof(FloatPrefix), "GetScrollValue");

            Type[] actionArg = { typeof(PlayerAction) };
            foreach (string name in new[] { "Pressed", "Pressing", "Released" })
                Patch(harmony, helper.GetMethod(name, S, null, actionArg, null), self, nameof(ActionBoolPrefix), name + "(PlayerAction)");
            Patch(harmony, helper.GetMethod("Value", S, null, actionArg, null), self, nameof(ActionFloatPrefix), "Value(PlayerAction)");

            AmbitionControlMod.Log.Msg("Click-through blocking: " + Applied + " patches applied" +
                                       (Failed > 0 ? ", " + Failed + " failed (see warnings)" : "") + ".");
        }

        private static void Patch(HarmonyLib.Harmony harmony, MethodInfo target, Type self, string prefix, string label)
        {
            try
            {
                if (target == null) throw new MissingMethodException("InputActionHelper", label);
                harmony.Patch(target, prefix: new HarmonyMethod(self.GetMethod(prefix, BindingFlags.NonPublic | BindingFlags.Static)));
                Applied++;
            }
            catch (Exception ex)
            {
                Failed++;
                AmbitionControlMod.Log.Warning("Could not patch InputActionHelper." + label + ": " + ex.GetBaseException().Message);
            }
        }

        // Harmony prefixes: return false to skip the original and use our __result.
        private static bool BoolPrefix(ref bool __result)
        {
            if (!PointerOverMenu()) return true;
            __result = false;
            return false;
        }

        private static bool FloatPrefix(ref float __result)
        {
            if (!PointerOverMenu()) return true;
            __result = 0f;
            return false;
        }

        private static bool ActionBoolPrefix(PlayerAction playerAction, ref bool __result)
        {
            if (!IsMouseAction(playerAction) || !PointerOverMenu()) return true;
            __result = false;
            return false;
        }

        private static bool ActionFloatPrefix(PlayerAction playerAction, ref float __result)
        {
            if (!IsMouseAction(playerAction) || !PointerOverMenu()) return true;
            __result = 0f;
            return false;
        }
    }
}
