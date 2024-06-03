using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System.Runtime.CompilerServices;

public static class EasingFunctions
{
	public enum EaseType
	{
		Linear,
		InQuad,
		OutQuad,
		InOutQuad,
		InCubic,
		OutCubic,
		InOutCubic,
		InQuart,
		OutQuart,
		InOutQuart,
		InQuint,
		OutQuint,
		InOutQuint,
		InSine,
		OutSine,
		InOutSine,
		InCirc,
		OutCirc,
		InOutCirc,
		InExpo,
		OutExpo,
		InOutExpo,
		InBack,
		OutBack,
		InOutBack,
		InElastic,
		OutElastic,
		InOutElastic,
		InBounce,
		OutBounce,
		InOutBounce,
	}

	public static float Ease(float t, EaseType easeType)
	{
		switch (easeType)
		{
			case EaseType.Linear:
				return Linear(t);
			case EaseType.InQuad:
				return InQuad(t);
			case EaseType.OutQuad:
				return OutQuad(t);
			case EaseType.InOutQuad:
				return InOutQuad(t);
			case EaseType.InCubic:
				return InCubic(t);
			case EaseType.OutCubic:
				return OutCubic(t);
			case EaseType.InOutCubic:
				return InOutCubic(t);
			case EaseType.InQuart:
				return InQuart(t);
			case EaseType.OutQuart:
				return OutQuart(t);
			case EaseType.InOutQuart:
				return InOutQuart(t);
			case EaseType.InQuint:
				return InQuint(t);
			case EaseType.OutQuint:
				return OutQuint(t);
			case EaseType.InOutQuint:
				return InOutQuint(t);
			case EaseType.InSine:
				return InSine(t);
			case EaseType.OutSine:
				return OutSine(t);
			case EaseType.InOutSine:
				return InOutSine(t);
			case EaseType.InCirc:
				return InCirc(t);
			case EaseType.OutCirc:
				return OutCirc(t);
			case EaseType.InOutCirc:
				return InOutCirc(t);
			case EaseType.InExpo:
				return InExpo(t);
			case EaseType.OutExpo:
				return OutExpo(t);
			case EaseType.InOutExpo:
				return InOutExpo(t);
			case EaseType.InBack:
				return InBack(t);
			case EaseType.OutBack:
				return OutBack(t);
			case EaseType.InOutBack:
				return InOutBack(t);
			case EaseType.InElastic:
				return InElastic(t);
			case EaseType.OutElastic:
				return OutElastic(t);
			case EaseType.InOutElastic:
				return InOutElastic(t);
			case EaseType.InBounce:
				return InBounce(t);
			case EaseType.OutBounce:
				return OutBounce(t);
			case EaseType.InOutBounce:
				return InOutBounce(t);
			default:
				throw new NotImplementedException($"{typeof(EaseType)}: {easeType}");
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Linear(float t)
	{
		return t;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InQuad(float t)
	{
		return t * t;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float OutQuad(float t)
	{
		float a = 1 - t;
		return 1 - a * a;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InOutQuad(float t)
	{
		float a = t * -2 + 2;
		if (t < 0.5f) { return t * t * 2; }
		else { return 1 - (a * a) / 2; }
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InCubic(float t)
	{
		return t * t * t;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float OutCubic(float t)
	{
		float a = 1 - t;
		return 1 - a * a * a;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InOutCubic(float t)
	{
		float a = t * -2 + 2;
		if (t < 0.5f) { return 4 * t * t * t; }
		else { return 1 - (a * a * a) / 2; }
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InQuart(float t)
	{
		return t * t * t * t;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float OutQuart(float t)
	{
		float a = 1 - t;
		return 1 - a * a * a * a;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InOutQuart(float t)
	{
		float a = t * -2 + 2;
		if (t < 0.5f) { return 8 * t * t * t * t; }
		else { return 1 - (a * a * a * a) / 2; }
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InQuint(float t)
	{
		return t * t * t * t * t;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float OutQuint(float t)
	{
		float a = 1 - t;
		return 1 - a * a * a * a * a;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InOutQuint(float t)
	{
		float a = t * -2 + 2;
		if (t < 0.5f) { return 16 * t * t * t * t * t; }
		else { return 1 - (a * a * a * a * a) / 2; }
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InSine(float t)
	{
		return 1 - math.cos(t * math.PI / 2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float OutSine(float t)
	{
		return math.sin(t * math.PI / 2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InOutSine(float t)
	{
		return (math.cos(t * math.PI) - 1) / -2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InExpo(float t)
	{
		return math.pow(2, 10 * (t - 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float OutExpo(float t)
	{
		return 1 - math.pow(2, t * -10);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InOutExpo(float t)
	{
		if (t < 0.5f) { return math.pow(2, 20 * t - 10) / 2; }
		else { return (2 - math.pow(2, -20 * t + 10) / 2) / 2; }
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InCirc(float t)
	{
		return 1 - math.sqrt(1 - t * t);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float OutCirc(float t)
	{
		float a = t - 1;
		return math.sqrt(1 - a * a);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InOutCirc(float t)
	{
		float a = -2 * t + 2;
		if (t < 0.5f) { return (1 - math.sqrt(1 - 4 * t * t)) / 2; }
		else { return (math.sqrt(1 - a * a) + 1) / 2; }
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InElastic(float t)
	{
		return -math.pow(2, 10 * t - 10) * math.sin((t * 10 - 10.75f) * (2 * math.PI / 3));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float OutElastic(float t)
	{
		return math.pow(2, -10 * t) * math.sin((t * 10 - 0.75f) * (2 * math.PI / 3)) + 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InOutElastic(float t)
	{
		if (t < 0.5f) { return -(math.pow(2, 20 * t - 10) * math.sin((20 * t - 11.125f) * ((2 * math.PI) / 4.5f))) / 2; }
		else { return (math.pow(2, -20 * t + 10) * math.sin((20 * t - 11.125f) * ((2 * math.PI) / 4.5f))) / 2 + 1; }
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InBack(float t)
	{
		return 2.70158f * t * t * t - 1.70158f * t * t;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float OutBack(float t)
	{
		float a = t - 1;
		return 1 + 2.70158f * a * a * a + 1.70158f * a * a;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InOutBack(float t)
	{
		float a = 2 * t - 2;
		if (t < 0.5f) { return (t * t * 4 * ((2.5949f + 1) * 2 * t - 2.5949f)) / 2; }
		else { return (a * a * ((2.5949f + 1) * a + 2.5949f) + 2) / 2; }
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InBounce(float t)
	{
		return 1 - OutBounce(1 - t);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float OutBounce(float t)
	{
		const float d = 2.75f;
		const float n = 7.5625f;

		if (t < (1.0f / d))
		{
			return n * t * t;
		}
		else if (t < (2.0f / d))
		{
			t -= 1.5f / d;
			return n * t * t + 0.75f;
		}
		else if (t < (2.5f / d))
		{
			t -= 2.25f / d;
			return n * t * t + 0.9375f;
		}
		else
		{
			t -= 2.625f / d;
			return n * t * t + 0.984375f;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float InOutBounce(float t)
	{
		if (t < 0.5f) { return (1 - OutBounce(1 - 2 * t)) / 2; }
		else { return (1 + OutBounce(2 * t - 1)) / 2; }
	}
}
