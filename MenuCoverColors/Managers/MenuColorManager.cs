using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MenuCoverColors.Configuration;
using UnityEngine;
using Zenject;

namespace MenuCoverColors.Managers;

[UsedImplicitly]
internal class MenuColorManager : IInitializable
{
    private static PluginConfig Config => PluginConfig.Instance;
    
    private static MenuLightsManager? _menuLightsManager;

    [Inject]
    public void Construct(MenuLightsManager menuLightsManager)
    {
        _menuLightsManager = menuLightsManager;
    }
    
    public void Initialize()
    {
        if (_menuLightsManager == null)
        {
            Plugin.DebugMessage("MenuLightsManager is null?");
        }
        
        GameObject? menuFogRing = GameObject.Find("MenuFogRing");
        if (menuFogRing?.TryGetComponent(out InstancedMaterialLightWithId instancedMaterialLightWithId) ?? false)
        {
            instancedMaterialLightWithId.SetLightId(2);
        }
    }

    public static void SetColor(Color skyColor, Color groundColor)
    {
        _ = FadeToColors(skyColor, groundColor);
    }
    
    private static async Task Animate(Action<float> transition, CancellationToken cancellationToken, float duration)
    {
        string transitionMethod = $"{Config.TransitionStartEndMethod}{Config.TransitionMethod}";
        
        MethodInfo easingMethod = typeof(Easing).GetMethods().First(x => x.Name == "Linear");
        try
        {
            easingMethod = typeof(Easing).GetMethods().First(x => x.Name == transitionMethod);
        }
        catch (Exception)
        {
            // ignore
        }
        
        float elapsedTime = 0.0f;
        while (elapsedTime <= duration)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            
            float value = (float)easingMethod.Invoke(null, [elapsedTime / duration]);
            transition?.Invoke(value);
            elapsedTime += Time.deltaTime;
            await Task.Yield();
        }

        transition?.Invoke(1f);
    }

    private static CancellationTokenSource? _fadeTokenSource;
    private static async Task FadeToColors(Color skyColor, Color groundColor)
    {
        _fadeTokenSource?.Cancel();
        _fadeTokenSource?.Dispose();
        
        _fadeTokenSource = new CancellationTokenSource();

        if (_menuLightsManager == null)
        {
            return;
        }

        Color initialGroundColor = _menuLightsManager.CurrentColorForID(1);
        Color initialSkyColor = _menuLightsManager.CurrentColorForID(2);
        
        await Animate(time =>
        {
            _menuLightsManager.SetColor(1, Color.LerpUnclamped(initialGroundColor, groundColor, time));
            _menuLightsManager.SetColor(2, Color.LerpUnclamped(initialSkyColor, skyColor, time));
            
#if V1_40_3
            _menuLightsManager.SetColor(0, Color.LerpUnclamped(initialGroundColor, groundColor, time));
            _menuLightsManager.SetColor(4, Color.LerpUnclamped(initialGroundColor, groundColor, time).ColorWithAlpha(0.06f));
#endif
        }, _fadeTokenSource.Token, Config.TransitionDuration);
    }
}