using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using JetBrains.Annotations;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

// ReSharper disable RedundantDefaultMemberInitializer
namespace MenuCoverColors.Configuration;

[UsedImplicitly]
internal class PluginConfig
{
    public static PluginConfig Instance { get; set; } = null!;
    
    public virtual bool Enabled { get; set; } = true;
    public virtual bool FlipGroundAndSkyColors { get; set; } = false;
    public virtual int DownsampleFactor { get; set; } = 1;
    public virtual float TransitionDuration { get; set; } = 0.4f;
    
    public virtual string TransitionStartEndMethod { get; set; } = "InOut";
    public virtual string TransitionMethod { get; set; } = "Cubic";
    public virtual int KernelSize { get; set; } = 2;
}