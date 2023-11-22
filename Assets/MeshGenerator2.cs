using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator2
{
    public static ChunkMeshData GenerateTerrainMesh(ChunkData chunk, int levelOfDetail)
    {
        int width = chunk.scaledHeightMap.GetLength(0);
        int height = chunk.scaledHeightMap.GetLength(1);

        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        int meshSimplificationIncrement = levelOfDetail == 0 ? 1 : levelOfDetail * 2;
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        ChunkMeshData meshData = new ChunkMeshData(verticesPerLine, verticesPerLine);
        Color[] colors = new Color[verticesPerLine * verticesPerLine];
        int vertexIndex = 0;

        for (int y = 0; y < height; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < width; x += meshSimplificationIncrement)
            {
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, chunk.scaledHeightMap[x,y], topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                if (x < width - 1 && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }
                colors[vertexIndex] = chunk.color[y*width + x];
                vertexIndex++;
            }
        }
        meshData.colors = colors;
        return meshData;
    }
}
