using UnityEngine;

namespace MenuCoverColors.Extensions;

public static class Color
{
    public static float MinColorComponent(this UnityEngine.Color color) => Mathf.Max(0.001f, Mathf.Min(Mathf.Min(color.r, color.g), color.b));
}