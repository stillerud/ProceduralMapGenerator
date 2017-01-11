using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mesh generator class.
/// </summary>
public static class MeshGenerator {

	/// <summary>
	/// Generates the terrain mesh.
	/// </summary>
	/// <returns>Mesh data.</returns>
	/// <param name="heightMap">Height map.</param>
	/// <param name="heightMultiplier">Height multiplier.</param>
	/// <param name="_heightCurve">Height curve.</param>
	/// <param name="levelOfDetail">Level of detail.</param>
	public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail){
		AnimationCurve heightCurve = new AnimationCurve (_heightCurve.keys);
		
		int width = heightMap.GetLength (0);
		int height = heightMap.GetLength (1);

		// Center the mesh
		float topLeftX = (width - 1) / -2f;
		float topLeftZ = (height - 1) / 2f;

		int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2; // if short hand to clamp it to 1
		int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

		MeshData meshData = new MeshData (verticesPerLine, verticesPerLine);
		int vertexIndex = 0;

		// Loops through all vertices and generate the terrain mesh
		for (int y = 0; y < height; y += meshSimplificationIncrement) {
			for (int x = 0; x < width; x += meshSimplificationIncrement) {

				// Create vertices with positions based on heighmap
				meshData.vertices [vertexIndex] = new Vector3 (topLeftX + x, heightCurve.Evaluate(heightMap [x, y]) * heightMultiplier, topLeftZ - y); 
				meshData.uvs [vertexIndex] = new Vector2 (x / (float)width, y / (float)height);

				// Generate meh triangles, but ignore bottom and left edges
				if (x < width - 1 && y < height - 1) {
					meshData.AddTriangles (vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine); // First triangle
					meshData.AddTriangles (vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1); // Second triangle
				}

				vertexIndex++;
			}
		}

		return meshData;
	}
}

/// <summary>
/// Mesh data class.
/// </summary>
public class MeshData : MonoBehaviour {
	public Vector3[] vertices;
	public int[] triangles;
	public Vector2[] uvs;

	int trianglesIndex;

	/// <summary>
	/// Initializes a new instance of the <see cref="MeshData"/> class.
	/// </summary>
	/// <param name="meshWidth">Width of the mesh.</param>
	/// <param name="meshHeght">Heght of the mesh.</param>
	public MeshData(int meshWidth, int meshHeght) {
		vertices = new Vector3[meshWidth * meshHeght];
		uvs = new Vector2[meshWidth * meshHeght];
		triangles = new int[(meshWidth - 1) * (meshHeght - 1) * 6];
	}

	/// <summary>
	/// Adds a triangle to the triangle array.
	/// </summary>
	/// <param name="a">First vertex index.</param>
	/// <param name="b">Second vertex index.</param>
	/// <param name="c">Third vertex index.</param>
	public void AddTriangles( int a, int b, int c) {
		triangles[trianglesIndex] = a;
		triangles[trianglesIndex+1] = b;
		triangles[trianglesIndex+2] = c;
		trianglesIndex+=3;
	}

	Vector3[] CalculateNormals() {
		Vector3[] vertexNormals = new Vector3[vertices.Length];
		int triangleCount = triangles.Length / 3;

		// Loop through all triangles and calculate their vertex normals
		for (int i = 0; i < triangleCount; i++) {
			int normalTriangleIndex = i * 3;
			int vertexIndexA = triangles [normalTriangleIndex];
			int vertexIndexB = triangles [normalTriangleIndex + 1];
			int vertexIndexC = triangles [normalTriangleIndex + 2];
		
			Vector3 triangleNormal = SurfaceNormalFromIndices (vertexIndexA, vertexIndexB, vertexIndexC);
			vertexNormals [vertexIndexA] += triangleNormal;
			vertexNormals [vertexIndexB] += triangleNormal;
			vertexNormals [vertexIndexC] += triangleNormal;
		}

		// Normalize all vertex normals
		for (int i = 0; i < vertexNormals.Length; i++) {
			vertexNormals [i].Normalize ();
		}
			
		return vertexNormals;
	}

	/// <summary>
	/// Get the surface normal from three indices.
	/// </summary>
	/// <returns>The normal from indices.</returns>
	/// <param name="indexA">Index a.</param>
	/// <param name="indexB">Index b.</param>
	/// <param name="indexC">Index c.</param>
	Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
		Vector3 pointA = vertices [indexA];
		Vector3 pointB = vertices [indexB];
		Vector3 pointC = vertices [indexC];

		Vector3 sideAB = pointB - pointA;
		Vector3 sideAC = pointC - pointA;
		return Vector3.Cross (sideAB, sideAC).normalized;
		//return Vector3.Cross (sideAC, sideAB).normalized;
	}


	void printMeshInfo(Mesh mesh){
		for (int i = 0; i < 100; i+=3) {
			string meshInfo = "### MeshInfo: ###\n";
			meshInfo += "i: " + i + ", ";
			meshInfo += "verts: ";
			meshInfo += mesh.triangles [i] + ": " + mesh.vertices [i] + ", ";
			meshInfo += mesh.triangles [i+1] + "; " + mesh.vertices [i+1] + ", ";
			meshInfo += mesh.triangles [i+2] + ": " + mesh.vertices [i+2] + ", ";
			meshInfo += "normals: ";
			meshInfo += mesh.normals [i] + ", ";
			meshInfo += mesh.normals [i+1] + ", ";
			meshInfo += mesh.normals [i+2] + ", ";
			//meshInfo += "#######";

			print (meshInfo);
		}
	
	} 

	/// <summary>
	/// Creates a mesh.
	/// </summary>
	/// <returns>A mesh.</returns>
	public Mesh CreateMesh() {
		Mesh mesh = new Mesh ();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		//mesh.RecalculateNormals ();
		mesh.normals = CalculateNormals ();
		//printMeshInfo (mesh);
		return mesh;
	}
}