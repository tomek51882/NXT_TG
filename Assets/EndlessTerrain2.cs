using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static EndlessTerrain;

[RequireComponent(typeof(MapGenerator2))]
public class EndlessTerrain2 : MonoBehaviour
{
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
    public LODInfo[] detailLevels;
    public static float maxViewDistance;

    static MapGenerator2 mapGen;

    public Transform viewer;
    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;

    public Material mapMaterial;

    int chunkSize;
    int chunksVisibleInViewDistance;

    Dictionary<Vector2, ChunkMesh> terrainChunkDict = new Dictionary<Vector2, ChunkMesh>();
    static List<ChunkMesh> chunksVisibleLastUpdate = new List<ChunkMesh>();

    private void Start()
    {
        mapGen = GetComponent<MapGenerator2>();
        maxViewDistance = detailLevels.Last().visibleDistanceThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);
        UpdateVisibleChunks();

    }

    void UpdateVisibleChunks()
    {
        foreach (var chunk in chunksVisibleLastUpdate)
        {
            chunk.SetVisible(false);
        }
        chunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.FloorToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.FloorToInt(viewerPosition.y / chunkSize);


        for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDict.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDict[viewedChunkCoord].UpdateTerrainChunk();
                }
                else
                {
                    terrainChunkDict.Add(viewedChunkCoord, new ChunkMesh(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }


    public class ChunkMesh
    {
        GameObject meshObject;
        Vector2 chunkCoord;
        Vector2 position;
        Bounds bounds;

        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        ChunkData chunkData;
        bool chunkDataReceived = false;
        int previousLODIndex = -1;
        public ChunkMesh(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;
            chunkCoord = coord;
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 posV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject($"Chunk {coord.x}, {coord.y}");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshObject.transform.position = posV3;
            meshObject.transform.parent = parent;
            SetVisible(false);
            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }
            mapGen.RequestChunkData(position, OnChunkDataReceived);
        }

        void OnChunkDataReceived(ChunkData chunkData)
        {
            //Debug.Log($"Chunk {chunkCoord.x} {chunkCoord.y} got data");
            this.chunkData = chunkData;
            chunkDataReceived = true;
            Texture2D texture = TextureGenerator.TextureFromHeightMap(this.chunkData.scaledHeightMap);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }
        public void UpdateTerrainChunk()
        {
            if (chunkDataReceived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDstFromNearestEdge <= maxViewDistance;

                if (visible)
                {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDistanceThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(chunkData);
                        }
                    }
                    chunksVisibleLastUpdate.Add(this);
                }

                SetVisible(visible);
            }

        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;

        Action updateCallback;

        public LODMesh(int lod, Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }
        void OnMeshDataReceived(ChunkMeshData meshData)
        {
            mesh = meshData.CreateMesh();

            hasMesh = true;
            updateCallback();
        }
        public void RequestMesh(ChunkData chunkData)
        {
            hasRequestedMesh = true;
            mapGen.RequestChunkMesh(chunkData, lod, OnMeshDataReceived);
        }
    }
}
