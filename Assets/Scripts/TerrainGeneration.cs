using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


[Serializable]
public class TerrainPreset
{
    public TerrainMethodType terrainType;
    public float roughness;                 // How rough the terrain will be
    public float smoothingFactor = 2f;      // How jagged the terrain will be
    public int featureCount;                // How many hills the terrain will have
    public float absoluteMinHeight = 5f;
    public float absoluteMaxHeight = 75f;
}


public delegate void TerrainMethod(float[] heightMap, TerrainPreset terrainValues);

public enum TerrainMethodType
{
    RockyMountains,
    RollingHills,
}

public static class TerrainGeneration
{
    public static TerrainMethod[] terrainMethods = {
        RockyMountains,
        RollingHills,
    };

    public static Mesh GenerateTerrainMesh(int terrainWidth, int resolution, TerrainPreset terrainValues)
    {
        // Generate the heightmap
        float[] heightMap = GenerateHeightMap(terrainWidth, resolution, terrainValues);

        // Create vertices, UVs, and triangles
        Vector2[] terrainVertices = CreateTerrainVertices(heightMap, resolution);
        Vector2[] terrainUV = GenerateTerrainUV(heightMap, terrainWidth);
        Vector2[] terrainUV2 = GenerateTerrainUV2(heightMap, resolution);
        int[] terrainTriangles = Triangulate(terrainVertices.Length);

        // Convert our Vector2s to Vector3s
        Vector3[] meshVertices = new Vector3[terrainVertices.Length];
        for (int i = 0; i < terrainVertices.Length; i++)
        {
            meshVertices[i] = new Vector3(terrainVertices[i].x, terrainVertices[i].y, 0);
        }

        Mesh mesh = new Mesh();
        mesh.vertices = meshVertices;
        mesh.uv = terrainUV;
        mesh.uv2 = terrainUV2;
        mesh.triangles = terrainTriangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }


    static float[] GenerateHeightMap(int terrainWidth, int resolution, TerrainPreset terrainValues)
    {
        // Create a heightmap array and set the start and endpoints
        float[] heightMap = new float[terrainWidth * resolution + 1];
        heightMap[0] = UnityEngine.Random.Range(terrainValues.absoluteMinHeight, terrainValues.absoluteMaxHeight);
        heightMap[heightMap.Length - 1] = UnityEngine.Random.Range(terrainValues.absoluteMinHeight, terrainValues.absoluteMaxHeight);

        // Call the desired method to generate the heightmap
        TerrainMethod method = terrainMethods[(int)terrainValues.terrainType];
        method(heightMap, terrainValues);

        // Adjust hightmap to be centered
        float heightMapHighest = terrainValues.absoluteMinHeight;
        float heightMapLowest = terrainValues.absoluteMaxHeight;
        for (int i = 0; i < heightMap.Length; i++)
        {
            if (heightMap[i] > heightMapHighest)
            {
                heightMapHighest = heightMap[i];
            }

            if (heightMap[i] < heightMapLowest)
            {
                heightMapLowest = heightMap[i];
            }
        }
        float heightMapMedian = (heightMapLowest + heightMapHighest) / 2;
        float middleHeight = (terrainValues.absoluteMinHeight + terrainValues.absoluteMaxHeight) / 2;
        float adjustment = middleHeight - heightMapMedian;
        for (int i = 0; i < heightMap.Length; i++)
        {
            heightMap[i] = heightMap[i] + adjustment;
        }
        
        return heightMap;
    }

    static void RockyMountains(float[] heightMap, TerrainPreset terrainValues)
    {
        RockyMountainsSubFunction(0, heightMap.Length - 1, terrainValues.roughness, terrainValues.smoothingFactor, heightMap);
    }
    static void RockyMountainsSubFunction(int start, int end, float roughness, float smoothingFactor, float[] heightMap)
    {
        // Find the midpoint of the array for this step
        int midPoint = (int)Mathf.Floor((start + end) / 2);

        if (midPoint != start)
        {
            // Find the mid height for this step
            float midHeight = (heightMap[start] + heightMap[end]) / 2;

            // Generate a new displacement between the roughness factor
            heightMap[midPoint] = midHeight + UnityEngine.Random.Range(-roughness, roughness);

            // Repeat the process for the left side and right side of the new mid point
            RockyMountainsSubFunction(start, midPoint, roughness / smoothingFactor, smoothingFactor, heightMap);
            RockyMountainsSubFunction(midPoint, end, roughness / smoothingFactor, smoothingFactor, heightMap);
        }
    }

    static void RollingHills(float[] heightMap, TerrainPreset terrainValues)
    {
        // Find the midpoint of the array for this step
        float midHeight = (heightMap[0] + heightMap[heightMap.Length - 1]) / 2;

        // Distance each hill/valley should be from each other
        int distance = (int)Mathf.Ceil((heightMap.Length - 1) / (terrainValues.featureCount + 1));
        int currentLocation = 0;

        // Array to store the points in the heightMap where the hills are
        int[] hillLocationsIndex = new int[terrainValues.featureCount + 2];
        hillLocationsIndex[0] = 0;
        hillLocationsIndex[hillLocationsIndex.Length - 1] = heightMap.Length - 1;

        // For each hill...
        for (int i = 0; i < terrainValues.featureCount; i++)
        {
            // ...set their locations...
            currentLocation += distance;

            hillLocationsIndex[i + 1] = currentLocation;

            // ...and generate a new displacement between the roughness factor
            heightMap[currentLocation] = midHeight + UnityEngine.Random.Range(-terrainValues.roughness, terrainValues.roughness);
        }

        int nextHillLocation = 1;

        // Iterate through the heightMap..
        for (int i = 1; i < heightMap.Length - 1; i++)
        {
            if (i >= hillLocationsIndex[nextHillLocation])
            {
                nextHillLocation++;
            }

            // and determine the Y value for each point between the hills using cosine interpolation
            heightMap[i] = CosineInterpolate(heightMap[hillLocationsIndex[nextHillLocation - 1]],
                heightMap[hillLocationsIndex[nextHillLocation]],
                (i - (float)hillLocationsIndex[nextHillLocation - 1]) / ((float)hillLocationsIndex[nextHillLocation] - hillLocationsIndex[nextHillLocation - 1]));
        }
    }
    
    private static float CosineInterpolate(float start, float end, float percentage)
    {
        float x2 = (1 - Mathf.Cos(percentage * Mathf.PI)) / 2;
        return (start * (1 - x2) + end * x2);
    }

    static Vector2[] CreateTerrainVertices(float[] heightMap, float resolution)
    {
        // The minimum resolution is 1
        resolution = Mathf.Max(1, resolution);

        Vector2[] vertices = new Vector2[heightMap.Length * 2];

        // For each point in the heightmap, create a vertex for the top and the bottom of the terrain.
        for (int i = 0; i < heightMap.Length; i++)
        {
            vertices[i * 2] = new Vector2(i / resolution, heightMap[i]);
            vertices[i * 2 + 1] = new Vector2(i / resolution, 0);
        }
        
        return vertices;
    }

    static Vector2[] GenerateTerrainUV(float[] heightMap, int terrainWidth)
    {
        Vector2[] uv = new Vector2[heightMap.Length * 2];

        float texSize = heightMap.Length - 1f;

        // Loop through heightmap and create a UV point for the top and bottom.
        for (int i = 0; i < heightMap.Length; i++)
        {
            uv[i * 2] = new Vector2(i / texSize, heightMap[i] / terrainWidth);
            uv[i * 2 + 1] = new Vector2(i / texSize, 0);
        }

        return uv;
    }

    static Vector2[] GenerateTerrainUV2(float[] heightMap, int resolution)
    {
        Vector2[] uv = new Vector2[heightMap.Length * 2];

        float texSize = heightMap.Length - 1f;

        // For each point in the heightMap, create a UV location for the top and bottom of the terrain
        for (int i = 0; i < heightMap.Length; i++)
        {
            // For the UV X value, the left side of the terrain is 0 and the right side is 1
            // For the UV Y value:
            // Y is 1 across the top of the terrain
            uv[i * 2] = new Vector2(i / texSize, 1);

            // and Y is an equal amount below the top of the terrain as the terrain is wide
            uv[i * 2 + 1] = new Vector2(i / texSize, ((heightMap[i] * resolution) - texSize) / -texSize);

            // "((heightMap[i] * resolution) - texSize) / -texSize" means that, for example, if the terrain is 100 units wide:
            // If the value of the heightMap at one point is 0, the Y for the UV at the bottom would be 1
            // If the value of the heightMap at one point is 100, the Y for the UV at the bottom would be 0
            // If the value of the heightMap at one point is 200, the Y for the UV at the bottom would be -1
        }

        return uv;
    }

    static int[] Triangulate(int count)
    {
        int[] indices = new int[(count - 2) * 3];

        // For each group of 4 vertices, add 6 indices to create 2 triangles
        for (int i = 0; i <= count - 4; i += 2)
        {
            indices[i * 3] = i;
            indices[(i * 3) + 1] = i + 3;
            indices[(i * 3) + 2] = i + 1;

            indices[(i * 3) + 3] = i + 3;
            indices[(i * 3) + 4] = i;
            indices[(i * 3) + 5] = i + 2;
        }

        return indices;
    }

    public static Vector2[] CreateColliderPoints(Mesh terrainMesh)
    {
        Vector3[] terrainVertices = terrainMesh.vertices;

        Vector2[] colliderPoints = new Vector2[(terrainMesh.vertices.Length / 2)];

        for (int i = 0; i < colliderPoints.Length; i++)
        {
            colliderPoints[i] = terrainVertices[i * 2];
        }

        return colliderPoints;
    }
}