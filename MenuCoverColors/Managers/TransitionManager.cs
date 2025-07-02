using System;
using System.Collections.Generic;
#if PRE_V1_39_1
using System.Threading;
#endif
using System.Threading.Tasks;
using IPA.Utilities.Async;
using JetBrains.Annotations;
using MenuCoverColors.Configuration;
using MenuCoverColors.Extensions;
using UnityEngine;
using Zenject;
using QuantizedColor = MenuCoverColors.ColorThief.QuantizedColor;

namespace MenuCoverColors.Managers;

[UsedImplicitly]
internal class TransitionManager : IInitializable, IDisposable
{
    private static PluginConfig Config => PluginConfig.Instance;
    
    private StandardLevelDetailViewController? _standardLevelDetailViewController;
    private static LevelPackDetailViewController? _levelPackDetailViewController;
    
    [Inject]
    public void Construct(StandardLevelDetailViewController standardLevelDetailViewController,
        LevelPackDetailViewController levelPackDetailViewController)
    {
        _standardLevelDetailViewController = standardLevelDetailViewController;
        _levelPackDetailViewController = levelPackDetailViewController;
    }

    public void Initialize()
    {
        if (_standardLevelDetailViewController == null)
        {
            Plugin.DebugMessage("_standardLevelDetailViewController is null");
            return;
        }
        
        _standardLevelDetailViewController.didChangeContentEvent += BeatmapDidUpdateContent;
    }

    public void Dispose()
    {
        if (_standardLevelDetailViewController == null)
        {
            return;
        }
        
        _standardLevelDetailViewController.didChangeContentEvent -= BeatmapDidUpdateContent;
    }
    
#if PRE_V1_37_1
    private static async Task<IPreviewBeatmapLevel> WaitForBeatmapLoaded(StandardLevelDetailViewController standardLevelDetailViewController)
    {
        while (standardLevelDetailViewController.beatmapLevel == null)
        {
            await Task.Yield();
        }

        return standardLevelDetailViewController.beatmapLevel;
    }
#else
    private static async Task<BeatmapLevel> WaitForBeatmapLoaded(StandardLevelDetailViewController standardLevelDetailViewController)
    {
        while (standardLevelDetailViewController._beatmapLevel == null)
        {
            await Task.Yield();
        }

        return standardLevelDetailViewController._beatmapLevel;
    }
#endif

    private class QuantizedColorComparer : IComparer<QuantizedColor>
    {
        public int Compare(QuantizedColor x, QuantizedColor y)
        {
            float xMin = x.UnityColor.MinColorComponent();
            float xMax = Mathf.Max(x.UnityColor.maxColorComponent, 0.001f);
            float yMin = y.UnityColor.MinColorComponent();
            float yMax = Mathf.Max(y.UnityColor.maxColorComponent, 0.001f);
            
            float xVibrancy = ((xMax + xMin) * (xMax - xMin)) / xMax;
            float yVibrancy = ((yMax + yMin) * (yMax - yMin)) / yMax;
            
            return xVibrancy > yVibrancy ? -1 : xVibrancy < yVibrancy ? 1 : 0;
        }
    }

    private static async Task RefreshColors(StandardLevelDetailViewController viewController)
    {
        if (_levelPackDetailViewController == null)
        {
            return;
        }
        
#if PRE_V1_37_1
        IPreviewBeatmapLevel beatmapLevel = await WaitForBeatmapLoaded(viewController);
#else
        BeatmapLevel beatmapLevel = await WaitForBeatmapLoaded(viewController);
#endif
            
#if PRE_V1_37_1
        Sprite? coverSprite = await beatmapLevel.GetCoverImageAsync(CancellationToken.None);
#elif PRE_V1_39_1
        Sprite? coverSprite = await beatmapLevel.previewMediaData.GetCoverSpriteAsync(CancellationToken.None);
#else
        Sprite? coverSprite = await beatmapLevel.previewMediaData.GetCoverSpriteAsync();
#endif
            
        RenderTexture? activeRenderTexture = RenderTexture.active; 
        RenderTexture temporary = RenderTexture.GetTemporary(Config.TextureSize, Config.TextureSize, 0,
            RenderTextureFormat.Default, RenderTextureReadWrite.Default);
        
        Texture2D readableTexture = new(Config.TextureSize, Config.TextureSize);
        
        try
        {
            RenderTexture.active = temporary;
            Graphics.Blit(coverSprite.texture, temporary);

            try
            {
                readableTexture.ReadPixels(new Rect(0, 0, temporary.width, temporary.height), 0, 0);
                readableTexture.Apply();
                
                if (Config.KernelSize > 0)
                {
                    readableTexture = _levelPackDetailViewController._kawaseBlurRenderer.Blur(readableTexture, 
                        (KawaseBlurRendererSO.KernelSize)Config.KernelSize - 1);
                }
            }
            finally
            {
                RenderTexture.active = activeRenderTexture;
            }
        }
        finally
        {
            RenderTexture.ReleaseTemporary(temporary);
        }
        
        List<QuantizedColor> colors = [];
        try
        {
            colors = ColorThief.ColorThief.GetPalette(readableTexture);
            colors.Sort(new QuantizedColorComparer());
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e);
        }
        
        MenuColorManager.SetColor(colors[Config.FlipGroundAndSkyColors ? 0 : 1].UnityColor,
            colors[Config.FlipGroundAndSkyColors ? 1 : 0].UnityColor);        
    }

    // https://github.com/WentTheFox/BSDataPuller/blob/0e5349e59a39a28be26e4bb6027d72948fff6eac/Core/MapEvents.cs#L395
    private static void BeatmapDidUpdateContent(StandardLevelDetailViewController viewController,
        StandardLevelDetailViewController.ContentType contentType)
    {
        if (!Config.Enabled)
        {
            return;
        }
        
        if (_levelPackDetailViewController == null)
        {
            return;
        }
        
        if (contentType != StandardLevelDetailViewController.ContentType.OwnedAndReady)
        {
            return;
        }
        
        _ = RefreshColors(viewController);
    }
}