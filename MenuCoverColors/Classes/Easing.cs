#if V1_29_1
using System;
using JetBrains.Annotations;

namespace MenuCoverColors.Classes;

// https://gist.github.com/liaoguipeng13/717f83f4971230e70d7e

// only necessary for 1.29.1
internal abstract class Easing
{
    private const float Pi = (float)Math.PI; 
    private const float HalfPi = Pi / 2f;

    public static float Linear(float progress) { return progress; } 
    public static float InQuad(float progress) { return EaseInPower(progress, 2); } 
    public static float OutQuad(float progress) { return EaseOutPower(progress, 2); }
    public static float InOutQuad(float progress) { return EaseInOutPower(progress, 2); }
    public static float InCubic(float progress) { return EaseInPower(progress, 3); }
    public static float OutCubic(float progress) { return EaseOutPower(progress, 3); }
    public static float InOutCubic(float progress) { return EaseInOutPower(progress, 3); }
    public static float InQuart(float progress) { return EaseInPower(progress, 4); }
    public static float OutQuart(float progress) { return EaseOutPower(progress, 4); }
    public static float InOutQuart(float progress) { return EaseInOutPower(progress, 4); }
    public static float InQuint(float progress) { return EaseInPower(progress, 5); }
    public static float OutQuint(float progress) { return EaseOutPower(progress, 5); }
    public static float InOutQuint(float progress) { return EaseInOutPower(progress, 5); }

    private static float EaseInPower(float progress, int power)
    {
        return (float)Math.Pow(progress, power);
    }

    private static float EaseOutPower(float progress, int power)
    {
        int sign = power % 2 == 0 ? -1 : 1;
        return (float)(sign * (Math.Pow(progress - 1, power) + sign));
    }

    private static float EaseInOutPower(float progress, int power)
    {
        progress *= 2;
        if (progress < 1)
        {
            return (float)Math.Pow(progress, power) / 2f;
        }
        else
        {
            int sign = power % 2 == 0 ? -1 : 1;
            return (float)(sign / 2.0 * (Math.Pow(progress - 2, power) + sign * 2));
        }
    }

    [UsedImplicitly]
    private static float InSine(float progress)
    {
        return (float)Math.Sin(progress * HalfPi - HalfPi) + 1;
    }

    [UsedImplicitly]
    private static float OutSine(float progress)
    {
        return (float)Math.Sin(progress * HalfPi);
    }

    [UsedImplicitly]
    private static float InOutSine(float progress)
    {
        return (float)(Math.Sin(progress * Pi - HalfPi) + 1) / 2;
    }
}
#endif