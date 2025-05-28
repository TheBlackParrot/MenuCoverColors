#if V1_40_3
using HarmonyLib;
using UnityEngine;

namespace MenuCoverColors.Patches;

// this works around a bug in 1.40.3+ where boost lights will effect menu lighting when maps use boost lights at any point
// the game's default "light color" in the menu is #808080FF (unity's color.gray is #7F7F7F but whatever)
// https://github.com/Meivyn/BeatSaberBugs/issues/20

[HarmonyPatch]
internal class ForceEnvironmentColorsOnMapExitPatch
{
    private static ColorScheme PatchColors(ColorSchemeSO schemeObj)
    {
        return new ColorScheme(schemeObj)
        {
            _colorSchemeId = "ActiveColorSchemeEnvironmentPatchedForMenuCoverColors",
            _colorSchemeNameLocalizationKey = "ActiveColorSchemeEnvironmentPatchedForMenuCoverColors",
            _useNonLocalizedName = true,
            _nonLocalizedName = "ActiveColorSchemeEnvironmentPatchedForMenuCoverColors",
            _isEditable = false,
            _environmentColor0 = Color.gray,
            _environmentColor1 = Color.gray,
            _supportsEnvironmentColorBoost = false
        };
    }

    [HarmonyPatch(typeof(GameplayLevelSceneTransitionEvents), "InvokeAnyGameplayLevelDidFinish")]
    [HarmonyPostfix]
    // ReSharper disable once InconsistentNaming
    public static void ForceEnvironmentColorsOnMapExit(GameplayLevelSceneTransitionEvents __instance)
    {
        Plugin.DebugMessage("InvokeAnyGameplayLevelDidFinish called");
        
        ColorSchemeSO schemeObj = ScriptableObject.CreateInstance<ColorSchemeSO>();
        schemeObj._colorScheme = __instance._standardLevelScenesTransitionSetupData.colorScheme;
        
        ColorScheme patched = PatchColors(schemeObj);
        
        __instance._standardLevelScenesTransitionSetupData.usingOverrideColorScheme = true;
        __instance._standardLevelScenesTransitionSetupData.colorScheme = patched;
    }
}
#endif