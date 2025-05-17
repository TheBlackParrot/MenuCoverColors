using System;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using JetBrains.Annotations;
using MenuCoverColors.Configuration;
using Zenject;

namespace MenuCoverColors.UI;

[UsedImplicitly]
internal class SettingsMenuManager : IInitializable, IDisposable
{
    private static PluginConfig Config => PluginConfig.Instance;

    // ReSharper disable UnusedMember.Local
    // ReSharper disable UnusedMember.Global
    protected bool Enabled
    {
        get => Config.Enabled;
        set => Config.Enabled = value;
    }
    protected bool FlipGroundAndSkyColors
    {
        get => Config.FlipGroundAndSkyColors;
        set => Config.FlipGroundAndSkyColors = value;
    }
    protected int DownsampleFactor
    {
        get => Config.DownsampleFactor;
        set => Config.DownsampleFactor = value;
    }
    protected string TransitionStartEndMethod
    {
        get => Config.TransitionStartEndMethod;
        set => Config.TransitionStartEndMethod = value;
    }
    protected string TransitionMethod
    {
        get => Config.TransitionMethod;
        set => Config.TransitionMethod = value;
    }
    protected float TransitionDuration
    {
        get => Config.TransitionDuration;
        set => Config.TransitionDuration = value;
    }
    protected int KernelSize
    {
        get => Config.KernelSize;
        set => Config.KernelSize = value;
    }
    
    private string SecondsFormatter(float value) => $"{value:0.00}s";
    private string KernelSizeFormatter(int value) => _blurChoices[value].ToString();
    
    [UIValue("startEndChoices")] private readonly List<object> _startEndChoices = ["In", "InOut", "Out"];
    [UIValue("methodChoices")] private readonly List<object> _methodChoices =
#if V1_29_1
        ["Linear", "Sine", "Quad", "Cubic", "Quart", "Quint"];
#else
        ["Linear", "Sine", "Quad", "Cubic", "Quart", "Quint", "Expo", "Circ", "Back", "Elastic", "Bounce"];
#endif
    [UIValue("blurChoices")] private readonly List<int> _blurChoices = [0, 7, 15, 23, 35, 63, 127, 135, 143];
    // ReSharper restore UnusedMember.Global
    // ReSharper restore UnusedMember.Local
    
    public void Initialize()
    {
#if PRE_V1_39_1
        BeatSaberMarkupLanguage.Settings.BSMLSettings.instance.AddSettingsMenu("MenuCoverColors",
            "MenuCoverColors.UI.BSML.Settings.bsml", this);
#else
        BeatSaberMarkupLanguage.Settings.BSMLSettings.Instance.AddSettingsMenu("MenuCoverColors",
            "MenuCoverColors.UI.BSML.Settings.bsml", this);
#endif
    }

    public void Dispose()
    {
#if PRE_V1_39_1
        BeatSaberMarkupLanguage.Settings.BSMLSettings.instance?.RemoveSettingsMenu(this);
#else
        BeatSaberMarkupLanguage.Settings.BSMLSettings.Instance?.RemoveSettingsMenu(this);
#endif
    }
}