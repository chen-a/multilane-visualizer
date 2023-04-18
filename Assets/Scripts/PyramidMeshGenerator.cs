using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))] // This will add the MeshFilter and MeshRenderer components to the GameObject if they are not already there
public class PyramidMeshGenerator : MonoBehaviour
{
    Vector3[] vertices;
    int[] triangles;
    Mesh mesh;
    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        CreatePyramid();
        UpdateMesh();

        transform.position = transform.parent.position;
        transform.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Transparent/Diffuse"));
        transform.GetComponent<MeshRenderer>().material.color = new Color (1f, 1f, 0.1f, 0.5f);
    }

    void UpdateMesh() {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    // Update is called once per frame
    void CreatePyramid()
    {
        float fov = 89; // field of view in degrees
        float pyramidLength = 2; // how far out the pyramid goes
        float horizontalOffset = (Mathf.PI * (fov / 2) / 180) * pyramidLength; // how far out the pyramid goes horizontally
        vertices = new Vector3[] {
            new Vector3(horizontalOffset, -horizontalOffset, pyramidLength), // 0 = bottom right 
            new Vector3(-horizontalOffset, -horizontalOffset, pyramidLength), // 1 = bottom left
            new Vector3(horizontalOffset, horizontalOffset, pyramidLength), // 2 = top right
            new Vector3(-horizontalOffset, horizontalOffset, pyramidLength), // 3 = top left
            new Vector3(0, 0, 0) // 4 = top of pyramid
        };

        triangles = new int[] {
            // Base
            0, 1, 2,
            2, 1, 3,
            // Sides
            4,0,2,
            4,2,3,
            4,3,1,
            4,1,0
        };
    }
}