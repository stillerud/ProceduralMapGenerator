﻿using System.Collections;
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
	public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail, bool useFlatShading){
		AnimationCurve heightCurve = new AnimationCurve (_heightCurve.keys);

		int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2; // if short hand to clamp it to 1

		int borderedSize = heightMap.GetLength (0);
		int meshSize = borderedSize - 2*meshSimplificationIncrement;
		int meshSizeUnsimplified = borderedSize - 2;

		// Center the mesh
		float topLeftX = (meshSizeUnsimplified - 1) / -2f;
		float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

		int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

		MeshData meshData = new MeshData (verticesPerLine, useFlatShading);

		int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
		int meshVertexIndex = 0;
		int borderVertexIndex = -1;

		// Loops through all vertices and generate the terrain mesh
		for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
			for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
				bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

				if (isBorderVertex) {
					vertexIndicesMap [x, y] = borderVertexIndex;
					borderVertexIndex--;
				} else {
					vertexIndicesMap [x, y] = meshVertexIndex;
					meshVertexIndex++;
				}
			}
		}

		// Loops through all vertices and generate the terrain mesh
		for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
			for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
				// Create vertices with positions based on heighmap
				int vertexIndex = vertexIndicesMap [x, y];
				Vector2 percent = new Vector2 ((x-meshSimplificationIncrement) / (float)meshSize, (y-meshSimplificationIncrement) / (float)meshSize);
				float height = heightCurve.Evaluate (heightMap [x, y]) * heightMultiplier;
				Vector3 vertexPosition = new Vector3 (topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified); 

				meshData.AddVertex (vertexPosition, percent, vertexIndex);

				// Generate meh triangles, but ignore bottom and left edges
				if (x < borderedSize - 1 && y < borderedSize - 1) {
					int a = vertexIndicesMap [x, y];
					int b = vertexIndicesMap [x + meshSimplificationIncrement, y];
					int c = vertexIndicesMap [x, y + meshSimplificationIncrement];
					int d = vertexIndicesMap [x + meshSimplificationIncrement, y + meshSimplificationIncrement];

					// Polygon with triangles following a clockwise order(?)
					meshData.AddTriangles (a, d, c); // First triangle
					meshData.AddTriangles (d, a, b); // Second triangle
				}

				vertexIndex++;
			}
		}

		meshData.Finalize ();

		return meshData;
	}
}

/// <summary>
/// Mesh data class.
/// </summary>
public class MeshData {
	Vector3[] vertices;
	int[] triangles;
	Vector2[] uvs;
	Vector3[] bakedNormals;

	Vector3[] borderVertices;
	int[] borderTriangles;

	int trianglesIndex;
	int borderTriangleIndex;

	bool useFlatShading;

	/// <summary>
	/// Initializes a new instance of the <see cref="MeshData"/> class.
	/// </summary>
	/// <param name="meshWidth">Width of the mesh.</param>
	/// <param name="meshHeght">Heght of the mesh.</param>
	public MeshData(int verticesPerLine, bool useFlatShading) {
		this.useFlatShading = useFlatShading;
		vertices = new Vector3[verticesPerLine * verticesPerLine];
		uvs = new Vector2[verticesPerLine * verticesPerLine];
		triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

		borderVertices = new Vector3[verticesPerLine * 4 + 4];
		borderTriangles = new int[24 * verticesPerLine]; // 6 * 4 * vertices per line
	}

	public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex){
		if (vertexIndex < 0) {
			borderVertices [-vertexIndex - 1] = vertexPosition; // boarderd vertices are negative and start at -1.
		} else {
			vertices [vertexIndex] = vertexPosition;
			uvs [vertexIndex] = uv;
		}
	}

	/// <summary>
	/// Adds a triangle to the triangle array.
	/// </summary>
	/// <param name="a">First vertex index.</param>
	/// <param name="b">Second vertex index.</param>
	/// <param name="c">Third vertex index.</param>
	public void AddTriangles( int a, int b, int c) {
		if (a < 0 || b < 0 || c < 0) { // border triangle
			borderTriangles [borderTriangleIndex] = a;
			borderTriangles [borderTriangleIndex + 1] = b;
			borderTriangles [borderTriangleIndex + 2] = c;
			borderTriangleIndex += 3;
		} else {
			triangles [trianglesIndex] = a;
			triangles [trianglesIndex + 1] = b;
			triangles [trianglesIndex + 2] = c;
			trianglesIndex += 3;
		}
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

		int borderTriangleCount = borderTriangles.Length / 3;

		// Loop through all triangles and calculate their vertex normals
		for (int i = 0; i < borderTriangleCount; i++) {
			int normalTriangleIndex = i * 3;
			int vertexIndexA = borderTriangles [normalTriangleIndex];
			int vertexIndexB = borderTriangles [normalTriangleIndex + 1];
			int vertexIndexC = borderTriangles [normalTriangleIndex + 2];

			Vector3 triangleNormal = SurfaceNormalFromIndices (vertexIndexA, vertexIndexB, vertexIndexC);
			if (vertexIndexA >= 0) {
				vertexNormals [vertexIndexA] += triangleNormal;
			}
			if (vertexIndexB >= 0) {
				vertexNormals [vertexIndexB] += triangleNormal;
			}
			if (vertexIndexC >= 0) {
				vertexNormals [vertexIndexC] += triangleNormal;
			}
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
		Vector3 pointA = (indexA < 0) ? borderVertices [-indexA - 1] : vertices [indexA];
		Vector3 pointB = (indexB < 0) ? borderVertices [-indexB - 1] : vertices [indexB];
		Vector3 pointC = (indexC < 0) ? borderVertices [-indexC - 1] : vertices [indexC];

		Vector3 sideAB = pointB - pointA;
		Vector3 sideAC = pointC - pointA;
		return Vector3.Cross (sideAB, sideAC).normalized;
		//return Vector3.Cross (sideAC, sideAB).normalized;
	}

	public void Finalize() {
		if (useFlatShading) {
			FlatShading ();
		} else {
			BakeNormals ();
		}
	}

	void BakeNormals() {
		bakedNormals = CalculateNormals ();
	}

	void FlatShading() {
		Vector3[] flatShadedVertices = new Vector3[triangles.Length];
		Vector2[] flatShadedUvs = new Vector2[triangles.Length];

		for (int i = 0; i < triangles.Length; i++) {
			flatShadedVertices [i] = vertices [triangles [i]];
			flatShadedUvs [i] = uvs [triangles [i]];
			triangles [i] = i;
		}

		vertices = flatShadedVertices;
		uvs = flatShadedUvs;
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
		if (useFlatShading) {
			mesh.RecalculateNormals ();	
		} else {
			mesh.normals = bakedNormals;
		}
		return mesh;
	}
}