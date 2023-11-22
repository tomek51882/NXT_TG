using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class HeightMapGenerator
{
    //public static float[,] GenerateHeightMap(int chunkSize, Vector2 chunkCoord, int seed, HeightMapSettings settings)
    public static float[,] GenerateHeightMap(int chunkSize, Vector2 chunkCoord, int seed, float scale, int octaves, float persistance, float lacunarity)
    {
        float[,] heightMap = new float[chunkSize, chunkSize];
        float highestPossibleNoiseVal = 0;
        float amplitude = 1;
        for (int i = 0; i < octaves; i++)
        {
            highestPossibleNoiseVal += amplitude;
            amplitude *= persistance;
        }
        Vector2[] octaveOffsets = new Vector2[octaves];
        System.Random prng = new System.Random(seed);

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + chunkCoord.x;
            float offsetY = prng.Next(-100000, 100000) - chunkCoord.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }
        float halfWidth = chunkSize / 2f;
        float halfHeight = chunkSize / 2f;

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);// * 2 - 1;
                    noiseHeight += perlinValue * amplitude;
                    amplitude *= persistance;
                    frequency *= lacunarity;

                }
                heightMap[x, y] = Mathf.InverseLerp(0, 1, noiseHeight);
            }
        }
        return heightMap;
    }
}
