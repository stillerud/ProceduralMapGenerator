using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator {

	public static void Generate TerrainMesh(float[,] heightMap){
		int width = heightMap.GetLength (0);
		int height = heightMap.GetLength (1);

		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				
			}
		}
	}
}


public class MeshData {
	public Vector3[] vertices;
	public int[] triangles;

	int trianglesIndex;

	public MeshData(int meshWidth, int meshHeght) {
		vertices = new Vector3[meshWidth * meshHeght];
		triangles = new int[(meshWidth - 1) * (meshHeght - 1) * 6];
	}

	public void AddTriangles( int a, int b, int c)) {
		triangles[trianglesIndex] = a;
		triangles[trianglesIndex+1] = b;
		triangles[trianglesIndex+2] = c;
		trianglesIndex+=3;
	}
}