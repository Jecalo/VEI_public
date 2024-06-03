using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;


public class NoiseVisualizer : MonoBehaviour
{
    public enum NoiseType
    {
        Perlin2D_sxxhash,
        Perlin3D_sxxhash,
        Perlin2D_org,
        Perlin3D_org,
        Perlin2D_unity,
        Perlin3D_unity,
        Perlin2D_table,
        Perlin2D_quick
    }

    public NoiseType noiseType;
    public bool Recalculate = false;
    public bool AutoRecalculate = false;

    [SerializeField]
    private int resolution = 64;
    [SerializeField]
    private uint seed = 0;
    [SerializeField]
    private float frequency = 8.0f;
    [SerializeField]
    private int octaves = 1;
    [SerializeField]
    private float lacunarity = 2.0f;

    [SerializeField]
    private Vector3 Translation = new Vector3(0f, 0f, 0f);
    [SerializeField]
    private Vector3 Rotation = new Vector3(0f, 0f, 0f);
    [SerializeField]
    private Vector3 Scale = new Vector3(1f, 1f, 1f);

    private MeshRenderer render;
    private Texture2D texture;
    private Material mat;

    private void Awake()
    {
        render = GetComponent<MeshRenderer>();
        mat = render.material;
    }

    private void Start()
    {
        UpdateNoise();
        Recalculate = false;
    }

    private void Update()
    {
        if (Recalculate)
        {
            Recalculate = false;
            UpdateNoise();
        }
        else if (AutoRecalculate)
        {
            UpdateNoise();
        }
    }

    public void UpdateNoise()
    {
        float4x4 trs = NoiseHelper.BuildTRS(Translation, Rotation, Scale);

        texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        for (int i = 0; i < resolution; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                float3 p = math.transform(trs, new float3((float)i / resolution, (float)j / resolution, 0.0f));
                float v = 0.0f;

                switch (noiseType)
                {
                    case NoiseType.Perlin2D_sxxhash:
                        v = Perlin2Dfbm.Generate(seed, p.xy, frequency, octaves, lacunarity);
                        break;
                    case NoiseType.Perlin3D_sxxhash:
                        v = Perlin3Dfbm.Generate(seed, p, frequency, octaves, lacunarity);
                        break;
                    case NoiseType.Perlin2D_org:
                        break;
                    case NoiseType.Perlin3D_org:
                        v = Perlin3Dfbm_org.Generate(p, frequency, octaves);
                        break;
                    case NoiseType.Perlin2D_unity:
                        v = UnityNoise.Perlin2D(seed, p.xy, frequency, octaves);
                        break;
                    case NoiseType.Perlin3D_unity:
                        v = noise.cnoise(p);
                        break;
                    case NoiseType.Perlin2D_table:
                        v = QuickNoise.Perlin2D_table(seed, p.xy, frequency, octaves);
                        break;
                    case NoiseType.Perlin2D_quick:
                        v = QuickNoise.Perlin2Dx4_(seed, p.xy, frequency, octaves);
                        break;
                    default:
                        break;
                }

                v = v * 0.5f + 0.5f;
                texture.SetPixel(i, j, new Color(v, v, v, 1.0f));
            }
        }

        texture.Apply();
        mat.mainTexture = texture;
    }

    public void RandomizeSeed()
    {
        seed = (uint)UnityEngine.Random.Range(1, 65536);
    }
}
