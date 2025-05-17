using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using MenuCoverColors.Configuration;

namespace MenuCoverColors.Patches;

[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class DisableDefaultTweening
{
    private static PluginConfig Config => PluginConfig.Instance;
    
#if PRE_V1_39_1
    [HarmonyPatch(typeof(MenuLightsManager), "Update")]
#else
    [HarmonyPatch(typeof(MenuLightsManager), "StartLightAnimation")]
#endif
    [HarmonyPrefix]
    private static bool Prefix(MenuLightsManager __instance)
    {
        return !Config.Enabled;
    }
}