using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Mesh generator class
public static class MeshGenerator {

	public static MeshData GenerateTerrainMesh(float[,] heightMap){
		int width = heightMap.GetLength (0);
		int height = heightMap.GetLength (1);

		// Center the mesh
		float topLeftX = (width - 1) / -2f;
		float topLeftZ = (height - 1) / 2f;

		MeshData meshData = new MeshData (width, height);
		int vertexIndex = 0;

		// Loops through all vertices and generate the terrain mesh
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {

				// Fill vertices with positions based on heighmap
				meshData.vertices [vertexIndex] = new Vector3 (topLeftX + x, heightMap [x, y], topLeftZ - y); 
				meshData.uvs [vertexIndex] = new Vector2 (x / (float)width, y / (float)height);

				// Generate meh triangles, but ignore bottom and left edges
				if (x < width - 1 && y < height - 1) {
					meshData.AddTriangles (vertexIndex, vertexIndex + width + 1, vertexIndex + width); // First triangle
					meshData.AddTriangles (vertexIndex + width + 1, vertexIndex, vertexIndex + 1); // Second triangle
				}

				vertexIndex++;
			}
		}

		return meshData;
	}
}

// Mesh data class
public class MeshData {
	public Vector3[] vertices;
	public int[] triangles;
	public Vector2[] uvs;

	int trianglesIndex;

	public MeshData(int meshWidth, int meshHeght) {
		vertices = new Vector3[meshWidth * meshHeght];
		uvs = new Vector2[meshWidth * meshHeght];
		triangles = new int[(meshWidth - 1) * (meshHeght - 1) * 6];
	}

	public void AddTriangles( int a, int b, int c) {
		triangles[trianglesIndex] = a;
		triangles[trianglesIndex+1] = b;
		triangles[trianglesIndex+2] = c;
		trianglesIndex+=3;
	}

	public Mesh CreateMesh() {
		Mesh mesh = new Mesh ();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		mesh.RecalculateNormals ();
		return mesh;
	}
}