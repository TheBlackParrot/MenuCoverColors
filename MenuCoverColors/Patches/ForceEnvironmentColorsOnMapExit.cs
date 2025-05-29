#if V1_40_3
using HarmonyLib;
using UnityEngine;

namespace MenuCoverColors.Patches;

// this works around a bug in 1.40.3+ where boost lights will effect menu lighting when maps use boost lights at any point
// https://github.com/Meivyn/BeatSaberBugs/issues/20

[HarmonyPatch]
internal class ForceEnvironmentColorsOnMapExitPatch
{
    private static readonly Color DefaultColor = new(0.502f, 0.502f, 0.502f, 1.0f);
    private static ColorScheme PatchColors(ColorSchemeSO schemeObj)
    {
        return new ColorScheme(schemeObj)
        {
            _colorSchemeId = "ActiveColorSchemeEnvironmentPatchedForMenuCoverColors",
            _colorSchemeNameLocalizationKey = "ActiveColorSchemeEnvironmentPatchedForMenuCoverColors",
            _useNonLocalizedName = true,
            _nonLocalizedName = "ActiveColorSchemeEnvironmentPatchedForMenuCoverColors",
            _isEditable = false,
            _environmentColor0 = DefaultColor,
            _environmentColor1 = DefaultColor,
            _environmentColor0Boost = DefaultColor,
            _environmentColor1Boost = DefaultColor,
            _environmentColorWBoost = DefaultColor,
            _environmentColorW = DefaultColor,
            _overrideLights = true,
            _supportsEnvironmentColorBoost = true
        };
    }

    [HarmonyPatch(typeof(GameplayLevelSceneTransitionEvents), "HandleStandardLevelDidFinish")]
    [HarmonyPrefix]
    // ReSharper disable once InconsistentNaming
    public static bool ForceEnvironmentColorsOnMapExit(GameplayLevelSceneTransitionEvents __instance)
    {
        ColorSchemeSO schemeObj = ScriptableObject.CreateInstance<ColorSchemeSO>();
        schemeObj._colorScheme = __instance._standardLevelScenesTransitionSetupData.colorScheme;
        
        ColorScheme patched = PatchColors(schemeObj);
        
        __instance._standardLevelScenesTransitionSetupData.usingOverrideColorScheme = true;
        __instance._standardLevelScenesTransitionSetupData.colorScheme = patched;
        return true;
    }
}
#endif