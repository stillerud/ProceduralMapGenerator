using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

/// <summary>
/// Generates a terrain map based on a perlin noise map
/// </summary>
public class MapGenerator : MonoBehaviour {

	// Public variables to control the map generation and noise map
	public enum DrawMode {NoiseMap, ColourMap, Mesh};
	public DrawMode drawMode;

	public Noise.NormalizeMode normalizeMode;

	public const int mapChunkSize = 241; // max
	[Range(0,6)]
	public int editorPreviewLOD;
	public float noiseScale;

	public int octaves;
	[Range(0,1)]
	public float persistence;
	public float lacunarity;

	public int seed;
	public Vector2 offset;

	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;

	public bool autoUpdate;

	public TerrainType[] regions;

	// Callback queues
	Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	/// <summary>
	/// Chooses what to show in editor based on the DrawMode enumorator.
	/// </summary>
	public void DrawMapInEditor() {
		MapData mapData = GenerateMapData (Vector2.zero);

		MapDisplay display = FindObjectOfType<MapDisplay> ();
		if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMap(mapData.heightMap));
		} else if (drawMode == DrawMode.ColourMap) {
			display.DrawTexture (TextureGenerator.TextureFromColourMap (mapData.colourMap, mapChunkSize, mapChunkSize));
		} else if (drawMode == DrawMode.Mesh) {
			display.DrawMesh (MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD), TextureGenerator.TextureFromColourMap (mapData.colourMap, mapChunkSize, mapChunkSize));
		}
	}

	/// <summary>
	/// Method that requests map data by spawning a new thread.
	/// </summary>
	/// <param name="callback">Callback.</param>
	public void RequestMapData(Vector2 centre, Action<MapData> callback) {
		ThreadStart threadStart = delegate {
			MapDataThread (centre, callback);
		};

		new Thread (threadStart).Start ();
	}

	/// <summary>
	/// Thread that generates map data and adds it to the callback queue.
	/// </summary>
	/// <param name="callback">Callback.</param>
	void MapDataThread(Vector2 centre, Action<MapData> callback) {
		MapData mapData = GenerateMapData (centre);
		lock (mapDataThreadInfoQueue) {
			mapDataThreadInfoQueue.Enqueue (new MapThreadInfo<MapData> (callback, mapData));
		}
	}

	/// <summary>
	/// Method that requests mesh data by spawning a new thread with the map data.
	/// </summary>
	/// <param name="mapData">Map data.</param>
	/// <param name="lod">Level Of detail.</param>
	/// <param name="callback">Callback.</param>
	public void RequestMeshData(MapData mapData,int lod, Action<MeshData> callback) {
		ThreadStart threadstart = delegate {
			MeshDataThread (mapData, lod, callback);
		};

		new Thread (threadstart).Start ();
	}


	/// <summary>
	/// Thread that generates mesh data and adds it to the callback queue.
	/// </summary>
	/// <param name="mapData">Map data.</param>
	/// <param name="lod">Level of detail.</param>
	/// <param name="callback">Callback.</param>
	void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
		MeshData meshData = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
		lock (meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue (new MapThreadInfo<MeshData>(callback, meshData));
		}
	}

	void  Update() {
		// Loop through any items in our map callback queue and ask them to call their callback method with map data.
		if (mapDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}
	
		// Loop through any items in our mesh callback queue and ask them to call their callback method with map data.
		if (meshDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}
	}
		
	/// <summary>
	/// Method that generate the map by a perling noise map.
	/// </summary>
	/// <returns>Returns a MapData struct containing the generated noise and colour map.</returns>
	MapData GenerateMapData(Vector2 centre) {
		float[,] noiseMap = Noise.GenerateNoiseMap (mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistence, lacunarity, centre + offset, normalizeMode);

		// Loop through each terrain type region and assign colours to our colour map based on user list
		Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
		for (int y = 0; y < mapChunkSize; y++) {
			for (int x = 0; x < mapChunkSize; x++) {
				float currentHeight = noiseMap [x, y];
				for (int i = 0; i < regions.Length; i++) {
					if (currentHeight >= regions [i].height) {
						colourMap [y * mapChunkSize + x] = regions [i].colour;
					} else {
						break;
					}
				}
			}
		}

		return new MapData (noiseMap, colourMap);
	}

	// Clamps the public variables
	void OnValidate() {
		if (lacunarity < 1) {
			lacunarity = 1;
		}
		if (octaves < 0) {
			octaves = 0;
		}
	}

	/// <summary>
	/// Generic threading struct for map and mesh data.
	/// </summary>
	struct MapThreadInfo<T> {
		public readonly Action<T> callback;
		public readonly T parameter;

		public MapThreadInfo (Action<T> callback, T parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}
		
	}
}

// Data struct that hold the terrain region data
[System.Serializable]
public struct TerrainType {
	public string name;
	public float height;
	public Color colour;
}

// Map data struct
public struct MapData {
	public readonly float[,] heightMap;
	public readonly Color[] colourMap;

	public MapData (float[,] heightMap, Color[] colourMap)
	{
		this.heightMap = heightMap;
		this.colourMap = colourMap;
	}
}