using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentScript : MonoBehaviour
{
    private GameObject terrainGameObject;

    public int terrainWidth;                // How many units wide the terrain will be
    public int resolution = 2;              // How many points/vertices per unit
    public TerrainPreset terrainValues;

    public Color terrainColor;
    public float gradientBalance = 1f;
    public float gradientStrength = 1.2f;

    public void Start()
    {
        InitializeEnvironment();
    }


    public void InitializeEnvironment()
    {
        // Delete existing terrain, if there is any
        if (transform.childCount > 0)
        {
            Debug.Log("Deleting Existing Terrain");
            GameObject child = gameObject.transform.GetChild(0).gameObject;
            // Can also find child by name if we end up having more than one child:
            //GameObject child = gameObject.transform.Find("Terrain").gameObject;
            Destroy(child);
        }

        // Create a new game object for the terrain and reparent as a child of this game object
        terrainGameObject = new GameObject("Terrain");
        terrainGameObject.transform.parent = transform;
        terrainGameObject.transform.position = transform.position;

        // Create the terrain
        InitializeTerrain();

        // Apply style to terrain
        ApplyTerrainStyle();
    }

    void InitializeTerrain()
    {
        // Generate the terrain mesh
        Mesh mesh = TerrainGeneration.GenerateTerrainMesh(terrainWidth, resolution, terrainValues);

        // Add a Mesh Renderer and Filter to the terrain game object, and apply the mesh to the filter
        terrainGameObject.AddComponent<MeshRenderer>();
        MeshFilter filter = terrainGameObject.AddComponent<MeshFilter>();
        filter.mesh = mesh;

        // Add EdgeCollider2D
        //EdgeCollider2D edgeCollider = terrainGameObject.AddComponent<EdgeCollider2D>();
        //edgeCollider.points = TerrainGeneration.CreateColliderPoints(mesh);
    }

    void ApplyTerrainStyle()
    {
        Renderer rend = terrainGameObject.GetComponent<Renderer>();
        rend.material.shader = Shader.Find(".Custom/Terrain Gradient");
        rend.material.color = terrainColor;
        rend.material.SetFloat("_GradBalance", gradientBalance);
        rend.material.SetFloat("_GradStrength", gradientStrength);
    }
}
