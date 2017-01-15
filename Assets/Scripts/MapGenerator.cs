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
	public enum DrawMode {NoiseMap, Mesh, FalloffMap};
	public DrawMode drawMode;

	public TerrainData terrainData;
	public NoiseData noiseData;
	public TextureData textureData;

	public Material terrainMaterial;

	[Range(0,6)]
	public int editorPreviewLOD;

	public bool autoUpdate;

	float[,] falloffMap;

	// Callback queues
	Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	public int mapChunkSize {
		get { 
			if (terrainData.useFlatShading) {
				return 	95; // compansated for border (-2) and flat shading!
			} else {
				return 239; // compansated for border (-2)
			}
		}
	}

	void OnValuesUpdated() {
		if (!Application.isPlaying) {
			DrawMapInEditor (); // Inspector values has changed so we update our maps
		}
	}

	void OnTextureValuesUpdated() {
		textureData.ApplyToMaterial (terrainMaterial);
	}

	/// <summary>
	/// Chooses what to show in editor based on the DrawMode enumorator.
	/// </summary>
	public void DrawMapInEditor() {
		MapData mapData = GenerateMapData (Vector2.zero);

		MapDisplay display = FindObjectOfType<MapDisplay> ();
		if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.heightMap));
		} else if (drawMode == DrawMode.Mesh) {
			display.DrawMesh (MeshGenerator.GenerateTerrainMesh (mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorPreviewLOD, terrainData.useFlatShading));
		} else if (drawMode == DrawMode.FalloffMap) {
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
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
		MeshData meshData = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod, terrainData.useFlatShading);
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
		float[,] noiseMap = Noise.GenerateNoiseMap (mapChunkSize+2, mapChunkSize+2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistence, noiseData.lacunarity, centre + noiseData.offset, noiseData.normalizeMode); // +2: compansate for border!

		if (terrainData.useFalloff) {

			if (falloffMap == null) {
				falloffMap = FalloffGenerator.GenerateFalloffMap (mapChunkSize + 2);
			}

			// Subtract the falloff map from the noise map
			for (int y = 0; y < mapChunkSize+2; y++) {
				for (int x = 0; x < mapChunkSize+2; x++) {
					if (terrainData.useFalloff) { // Could be cleaned up
						noiseMap [x, y] = Mathf.Clamp01 (noiseMap [x, y] - falloffMap [x, y]);
					}

				}
			}

		}

		// Feed the world height into our custom terrain material
		textureData.UpdateMeshHeights (terrainMaterial, terrainData.minHeight, terrainData.maxHeight);

		return new MapData (noiseMap);
	}

	// Subsribe to custom inspector update methods
	void OnValidate() {

		if (terrainData != null) {
			terrainData.OnValuesUpdated -= OnValuesUpdated; // First unsubscibe
			terrainData.OnValuesUpdated += OnValuesUpdated; // Subsribe to update event
		}
		if (noiseData != null) {
			noiseData.OnValuesUpdated -= OnValuesUpdated; // First unsubscibe
			noiseData.OnValuesUpdated += OnValuesUpdated; // Subsribe to update event
		}
		if (textureData != null) {
			textureData.OnValuesUpdated -= OnTextureValuesUpdated; // First unsubscibe
			textureData.OnValuesUpdated += OnTextureValuesUpdated; // Subsribe to update event
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

// Map data struct
public struct MapData {
	public readonly float[,] heightMap;

	public MapData (float[,] heightMap)
	{
		this.heightMap = heightMap;
	}
}