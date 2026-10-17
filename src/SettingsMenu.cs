using System;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OTH.ImpulseTriggers;

// Adds "Left Trigger Rumble" and "Right Trigger Rumble" sliders under the game's
// Controller Vibration slider (Settings > Controls > Advanced). Each row is a
// clone of the game's own slider, so it looks and navigates like the rest of
// the menu. The slider's position maps to a gain of 0 (off) .. MaxGain; the
// middle is the default 1.0x.
internal static class SettingsMenu
{
    [HarmonyPatch(typeof(StateControlsSettingsMenu), nameof(StateControlsSettingsMenu.InitContent))]
    private static class InitContentPatch
    {
        private static void Postfix(StateControlsSettingsMenu __instance)
        {
            try
            {
                AddRows(__instance);
            }
            catch (Exception ex)
            {
                Mod.Log.Error("Could not add trigger sliders: " + ex);
            }
        }
    }

    // The game redraws the value text whenever a slider moves; show our gain instead.
    [HarmonyPatch(typeof(UISliderSelector), nameof(UISliderSelector.UpdateText))]
    private static class UpdateTextPatch
    {
        private static void Postfix(UISliderSelector __instance)
        {
            GainRow r = Find(__instance);
            if (r != null)
            {
                r.ShowValue();
            }
        }
    }

    public const float MaxGain = 2f;

    private sealed class GainRow
    {
        public readonly string Name;

        public readonly string Label;

        public readonly Func<float> Get;

        public readonly Action<float> Set;

        public UISliderSelector Selector;

        public Slider Slider;

        public float LastGain = -1f;

        public GainRow(string name, string label, Func<float> get, Action<float> set)
        {
            Name = name;
            Label = label;
            Get = get;
            Set = set;
        }

        public bool Alive => Selector != null && Slider != null;

        public float Gain()
        {
            float span = Slider.maxValue - Slider.minValue;
            float t = span > 0f ? Mathf.Clamp01((Slider.value - Slider.minValue) / span) : 0f;
            // Snap to 0.1 so the stored value matches what the text shows.
            return Mathf.Round(t * MaxGain * 10f) / 10f;
        }

        public void ShowValue()
        {
            if (!Alive || Selector.text == null)
            {
                return;
            }
            float g = Gain();
            Selector.text.Cast<TMP_Text>().text = g <= 0f ? "Off" : $"{g:0.0}x";
        }
    }

    private static readonly GainRow[] rows =
    {
        new("ImpulseTriggers_LeftGain", "Left Trigger Rumble", () => Mod.LeftGain, v => Mod.LeftGain = v),
        new("ImpulseTriggers_RightGain", "Right Trigger Rumble", () => Mod.RightGain, v => Mod.RightGain = v),
    };

    private static float saveAt = -1f;

    private static float nextLabelCheck;

    private static GainRow Find(UISliderSelector s)
    {
        foreach (GainRow r in rows)
        {
            if (r.Selector != null && r.Selector.Pointer == s.Pointer)
            {
                return r;
            }
        }
        return null;
    }

    private static void AddRows(StateControlsSettingsMenu state)
    {
        UISliderSelector orig = state.controlsSettingsMenuUIContext?.ControllerVibrationSliderSelector;
        if (orig == null)
        {
            return;
        }
        Transform parent = orig.transform.parent;

        UISliderSelector above = orig;
        foreach (GainRow r in rows)
        {
            Transform stale = parent.Find(r.Name);
            if (stale != null)
            {
                UnityEngine.Object.DestroyImmediate(stale.gameObject);
            }
            GameObject go = UnityEngine.Object.Instantiate(orig.gameObject, parent);
            go.name = r.Name;
            go.transform.SetSiblingIndex(above.transform.GetSiblingIndex() + 1);
            StripLocalization(go);

            r.Selector = go.GetComponent<UISliderSelector>();
            r.Slider = r.Selector.slider;
            SetLabel(r.Selector, r.Label);

            MinMax<float> range = orig.valueRange;
            float min = range.Min, max = range.Max;
            if (max <= min)
            {
                min = 0f;
                max = 1f;
            }
            r.Selector.Initialize(range, min + Mathf.Clamp(r.Get(), 0f, MaxGain) / MaxGain * (max - min));
            r.LastGain = r.Gain();
            r.ShowValue();

            InsertNavigation(above, r.Selector);
            above = r.Selector;
        }

        // Make the rows part of the "Advanced" controller group, so they show and
        // hide with the game's own advanced controller options.
        Il2CppSystem.Collections.Generic.List<Selectable> advanced = state.advancedControllerSelectables;
        if (advanced != null)
        {
            int at = advanced.IndexOf(orig);
            if (at >= 0)
            {
                foreach (GainRow r in rows)
                {
                    if (!advanced.Contains(r.Selector))
                    {
                        advanced.Insert(++at, r.Selector);
                    }
                }
            }
        }
        Mod.Log.Msg("Added trigger rumble sliders to Controls settings");
    }

    // Cloned rows would otherwise be re-labelled "Controller Vibration" by the
    // localization system.
    private static void StripLocalization(GameObject go)
    {
        foreach (Component c in go.GetComponentsInChildren<Component>(true))
        {
            string type = c.GetIl2CppType().Name;
            if (type.Contains("Locali") || type == "TextContent" || type == "LocalizedStatement")
            {
                UnityEngine.Object.DestroyImmediate(c);
            }
        }
    }

    // Set every text in the row except the value readout.
    private static void SetLabel(UISliderSelector selector, string label)
    {
        IntPtr valueText = selector.text != null ? selector.text.Pointer : IntPtr.Zero;
        foreach (TMP_Text t in selector.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.Pointer == valueText)
            {
                continue;
            }
            // The game's setter throws once localization is already cleared, so
            // only clear it while it's set, and never let it abort the label.
            try
            {
                FunTextTMP fun = t.TryCast<FunTextTMP>();
                if (fun != null && !ReferenceEquals(fun.TextLocalization, null))
                {
                    fun.TextLocalization = null;
                }
            }
            catch (Exception ex)
            {
                Mod.Log.Warning("Could not clear label localization: " + ex.Message);
            }
            if (t.text != label)
            {
                t.text = label;
            }
        }
    }

    // Splice `row` into explicit up/down navigation directly below `above`.
    private static void InsertNavigation(Selectable above, Selectable row)
    {
        Navigation nav = above.navigation;
        if (nav.mode != Navigation.Mode.Explicit)
        {
            return;
        }
        Selectable below = nav.selectOnDown;
        Navigation rowNav = nav;
        rowNav.selectOnUp = above;
        rowNav.selectOnDown = below;
        row.navigation = rowNav;
        nav.selectOnDown = row;
        above.navigation = nav;
        if (below != null)
        {
            Navigation belowNav = below.navigation;
            if (belowNav.mode == Navigation.Mode.Explicit)
            {
                belowNav.selectOnUp = row;
                below.navigation = belowNav;
            }
        }
    }

    public static void Tick(float now)
    {
        foreach (GainRow r in rows)
        {
            if (!r.Alive || !r.Selector.gameObject.activeInHierarchy)
            {
                continue;
            }
            // Store the value first, so a cosmetic failure below can't lose it.
            float g = r.Gain();
            if (Mathf.Abs(g - r.LastGain) > 0.001f)
            {
                r.LastGain = g;
                r.Set(g);
                Mod.Preview(left: r == rows[0], now);
                saveAt = now + 1f;
                r.ShowValue();
            }
            // Something (localization, menu refresh) may re-apply the original label.
            if (now > nextLabelCheck)
            {
                SetLabel(r.Selector, r.Label);
            }
        }
        if (now > nextLabelCheck)
        {
            nextLabelCheck = now + 0.5f;
        }
        if (saveAt > 0f && now > saveAt)
        {
            saveAt = -1f;
            MelonLoader.MelonPreferences.Save();
        }
    }
}
