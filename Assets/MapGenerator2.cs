using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(MapDisplay))]
public class MapGenerator2 : MonoBehaviour
{
    public bool autoUpdate;
    public const int mapChunkSize = 241;
    public int seed;

    public const float minTerrainHeight = -128;
    public const float maxTerrainHeight = 256;


    public enum ChunkGenerationMode
    {
        Everything, Continentalness, Erosion, PV, Temperature, Humidity
    }
    public ChunkGenerationMode generationMode = ChunkGenerationMode.Continentalness;
    public ChunkGenerationMode textureDisplayMode = ChunkGenerationMode.Continentalness;
    [Header("Continentalness")]
    public float cScale;
    public int cOctaves;
    [Range(0f, 1f)]
    public float cPersistance;
    [Range(0f, 10f)]
    public float cLacunarity;
    public AnimationCurve ContHeightModifier;

    [Header("Erosion")]
    public float eScale;
    public int eOctaves;
    [Range(0f, 1f)]
    public float ePersistance;
    [Range(0f, 10f)]
    public float eLacunarity;
    [Range(0f,1f)]
    public float eStrength;
    public AnimationCurve ErHeightModifier;

    [Header("PV")]
    public float pvScale;
    public int pvOctaves;
    [Range(0f, 1f)]
    public float pvPersistance;
    [Range(0f, 10f)]
    public float pvLacunarity;
    [Range(0f, 1f)]
    public float pvStrength;
    public AnimationCurve PvHeightModifier;

    [Header("Temperature")]
    public Gradient tempGradient;
    public float tScale;
    public int tOctaves;
    [Range(0f, 1f)]
    public float tPersistance;
    [Range(0f, 10f)]
    public float tLacunarity;

    [Header("Humidity")]
    public Gradient hGradient;
    public float hScale;
    public int hOctaves;
    [Range(0f, 1f)]
    public float hPersistance;
    [Range(0f, 10f)]
    public float hLacunarity;

    [Header("Biome")]
    public Gradient colorGradient;

    Queue<ThreadInfo<ChunkData>> chunkDataThreadInfoQueue = new Queue<ThreadInfo<ChunkData>>();
    Queue<ThreadInfo<ChunkMeshData>> chunkMeshThreadInfoQueue = new Queue<ThreadInfo<ChunkMeshData>>();

    private void Update()
    {
        if (chunkDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < chunkDataThreadInfoQueue.Count; i++)
            {
                ThreadInfo<ChunkData> threadInfo = chunkDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        if (chunkMeshThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < chunkMeshThreadInfoQueue.Count; i++)
            {
                ThreadInfo<ChunkMeshData> threadInfo = chunkMeshThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    public void DrawSingleChunkInEditor()
    {
        ChunkData chunk = GenerateMapChunk(Vector2.zero, generationMode);
        MapDisplay display = GetComponent<MapDisplay>();
        //display.DrawTexture(TextureGenerator.TextureFromHeightMap(chunk.heightMap));
        Texture2D texture = null;
        switch (textureDisplayMode)
        {
            case ChunkGenerationMode.Continentalness:
                texture = TextureGenerator.TextureFromHeightMap(chunk.continentalnessMap);
                break;
            case ChunkGenerationMode.Erosion:
                texture = TextureGenerator.TextureFromHeightMap(chunk.erosionMap);
                break;
            case ChunkGenerationMode.PV:
                texture = TextureGenerator.TextureFromHeightMap(chunk.peakValleyMap);
                break;
            case ChunkGenerationMode.Temperature:
                texture = TextureGenerator.TextureFromGradientData(chunk.temperature, tempGradient);
                break;
            case ChunkGenerationMode.Humidity:
                texture = TextureGenerator.TextureFromGradientData(chunk.humidity, hGradient);
                break;
            default:
                texture = TextureGenerator.TextureFromHeightMap(chunk.continentalnessMap);
                break;
        }
        display.DrawMesh(MeshGenerator2.GenerateTerrainMesh(chunk, 0), texture);
    }

    public ChunkData GenerateMapChunk(Vector2 chunkOffset, ChunkGenerationMode genMode)
    {
        AnimationCurve coCurve = new AnimationCurve(ContHeightModifier.keys);
        AnimationCurve erCurve = new AnimationCurve(ErHeightModifier.keys);
        AnimationCurve pvCurve = new AnimationCurve(PvHeightModifier.keys);

        float[,] contMap = HeightMapGenerator.GenerateHeightMap(mapChunkSize, chunkOffset, seed, cScale, cOctaves, cPersistance, cLacunarity);
        float[,] erosionMap = HeightMapGenerator.GenerateHeightMap(mapChunkSize, chunkOffset, seed, eScale, eOctaves, ePersistance, eLacunarity);
        float[,] pvMap = HeightMapGenerator.GenerateHeightMap(mapChunkSize, chunkOffset, seed, pvScale, pvOctaves, pvPersistance, pvLacunarity);
        float[,] temperature = HeightMapGenerator.GenerateHeightMap(mapChunkSize, chunkOffset, seed, tScale, tOctaves, tPersistance, tLacunarity);
        float[,] humidity = HeightMapGenerator.GenerateHeightMap(mapChunkSize, chunkOffset, seed, hScale, hOctaves, hPersistance, hLacunarity);

        int width = contMap.GetLength(0);
        int height = contMap.GetLength(1);

        float[,] result = new float[width, height];
        Color[] colors = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float baseHeight = coCurve.Evaluate(contMap[x, y]) * 2 -1;
                //float erosionEffect = (1 - erCurve.Evaluate(erosionMap[x, y]));
                //float localVariation = pvCurve.Evaluate(pvMap[x, y]);
                
                //float baseHeight = contMap[x, y];
                float erosionEffect = erosionMap[x, y] * (1- baseHeight);
                float localVariation = pvMap[x, y];// * erosionMap[x, y];

                //float localCont = contMap[x, y];
                //float localErosion = erosionMap[x, y];
                if (genMode == ChunkGenerationMode.Everything)
                {
                    baseHeight *= 1 - (erosionEffect * eStrength);
                    baseHeight += (pvMap[x, y]) * pvStrength * ((erosionEffect));
                }
                else if (genMode == ChunkGenerationMode.Continentalness)
                {

                }
                else if (genMode == ChunkGenerationMode.Erosion)
                {
                    baseHeight = (erosionMap[x, y]*2-1) * eStrength;
                }
                else if (genMode == ChunkGenerationMode.PV)
                {
                    baseHeight = (pvMap[x, y] * 2 - 1) * pvStrength;
                }

                //biome selection
                colors[y*height+x] = colorGradient.Evaluate((baseHeight + 1) / 2);

                result[x, y] = Mathf.Lerp(minTerrainHeight, maxTerrainHeight, (baseHeight+1)/2);
            }
        }
        //Mathf.InverseLerp(minTerrainHeight, maxTerrainHeight, xy)
        //blending

        return new ChunkData(result, contMap, erosionMap, pvMap, temperature, humidity, colors);
    }

    public void RequestChunkData(Vector2 chunkOffset, Action<ChunkData> callback)
    {
        void threadBody()
        {
            ChunkDataThread(chunkOffset, callback);
        }
        new Thread(threadBody).Start();
    }
    void ChunkDataThread(Vector2 chunkOffset, Action<ChunkData> callback)
    {
        ChunkData chunkData = GenerateMapChunk(chunkOffset, ChunkGenerationMode.Everything);
        lock (chunkDataThreadInfoQueue)
        {
            chunkDataThreadInfoQueue.Enqueue(new ThreadInfo<ChunkData>(callback, chunkData));
        }
    }

    public void RequestChunkMesh(ChunkData chunkData, int lod, Action<ChunkMeshData> callback)
    {
        void threadBody()
        {
            ChunkDataThread(chunkData, lod, callback);
        }
        new Thread(threadBody).Start();
    }
    void ChunkDataThread(ChunkData chunkData, int lod, Action<ChunkMeshData> callback)
    { 
        ChunkMeshData chunkMesh = MeshGenerator2.GenerateTerrainMesh(chunkData, lod);
        lock(chunkMeshThreadInfoQueue)
        {
            chunkMeshThreadInfoQueue.Enqueue(new ThreadInfo<ChunkMeshData>(callback, chunkMesh));
        }
    }

    struct ThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public ThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

}

public struct ChunkData
{
    public readonly float[,] scaledHeightMap;
    public readonly float[,] continentalnessMap;
    public readonly float[,] erosionMap;
    public readonly float[,] peakValleyMap;
    public readonly float[,] temperature;
    public readonly float[,] humidity;
    public readonly Color[] color;
    public ChunkData(float[,] heightMap, float[,] continentalnessMap, float[,] erosionMap, float[,] peakValleyMap, float[,] temperature, float[,] humidity, Color[] color)
    {
        this.scaledHeightMap = heightMap;
        this.continentalnessMap = continentalnessMap;
        this.erosionMap = erosionMap;
        this.peakValleyMap = peakValleyMap;
        this.temperature = temperature;
        this.humidity = humidity;
        this.color = color;
    }
}

//public struct 