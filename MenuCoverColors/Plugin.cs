using System.Reflection;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using JetBrains.Annotations;
using MenuCoverColors.Configuration;
using MenuCoverColors.Installers;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;
using IPAConfig = IPA.Config.Config;

namespace MenuCoverColors;

[Plugin(RuntimeOptions.SingleStartInit)]
[UsedImplicitly]
internal class Plugin
{
    internal static IPALogger Log { get; private set; } = null!;
    private static Harmony _harmony = null!;

    [Init]
    public Plugin(IPALogger ipaLogger, IPAConfig ipaConfig, Zenjector zenjector)
    {
        Log = ipaLogger;
        zenjector.UseLogger(Log);
        
        PluginConfig c = ipaConfig.Generated<PluginConfig>();
        PluginConfig.Instance = c;
        
        zenjector.Install<MenuInstaller>(Location.Menu);
        
        Log.Info("Plugin loaded");
    }
    
    [OnEnable]
    public void OnEnable()
    {
        _harmony = new Harmony("TheBlackParrot.MenuCoverColors");
        _harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    [OnDisable]
    public void OnDisable()
    {
        _harmony.UnpatchSelf();
    }
    
    public static void DebugMessage(string message)
    {
#if DEBUG
        Log.Info(message);
#endif
    }
}