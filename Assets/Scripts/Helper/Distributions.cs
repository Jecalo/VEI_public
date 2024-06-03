using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;



public static class Distributions
{
    public static float Uniform(ref Unity.Mathematics.Random rng, float min, float max)
    {
        return rng.NextFloat(min, max);
    }

    public static float Eased(ref Unity.Mathematics.Random rng, EasingFunctions.EaseType easeType, float min, float max)
    {
        return min + (max - min) * EasingFunctions.Ease(rng.NextFloat(), easeType);
    }

    public static float NormalOpen(ref Unity.Mathematics.Random rng, float mean, float stdDev)
    {
        float u0 = rng.NextFloat();
        float u1 = rng.NextFloat();

        float a = math.sqrt(math.log(u0) * -2f) * stdDev;

        math.sincos(math.PI * 2f * u1, out float z1, out float z0);
        z0 = z0 * a + mean;
        //z1 = z1 * a + mean;

        return z0;
    }

    public static float Normal(ref Unity.Mathematics.Random rng, float min, float max)
    {
        float u0 = rng.NextFloat();
        float u1 = rng.NextFloat();

        float a = math.sqrt(math.log(u0) * -2f);

        math.sincos(math.PI * 2f * u1, out float z1, out float z0);
        z0 = z0 * a * ((max - min) / 5.0f) + (max + min) / 2.0f;
        //z1 = z1 * a * ((max - min) / 5.0f) + (max + min) / 2.0f;

        return math.clamp(z0, min, max);
    }
}
