using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Map generator class
public class MapGenerator : MonoBehaviour {

	public enum DrawMode {NoiseMap, ColourMap};
	public DrawMode drawMode;

	// Public variables to control the map generation and noise map
	public int mapWidth;
	public int mapHeight;
	public float noiseScale;

	public int octaves;
	[Range(0,1)]
	public float persistence;
	public float lacunarity;

	public int seed;
	public Vector2 offset;

	public bool autoUpdate;

	public TerrainType[] regions;

	// Method that generate the map by a noise map and display it
	public void GenerateMap() {
		float[,] noiseMap = Noise.GenerateNoiseMap (mapWidth, mapHeight, seed, noiseScale, octaves, persistence, lacunarity, offset);

		// Loop through each terrain type region and assign colour to our colour map
		Color[] colourmMap = new Color[mapWidth * mapHeight];
		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				float currentHeight = noiseMap [x, y];
				for (int i = 0; i < regions.Length; i++) {
					if (currentHeight <= regions [i].height) {
						colourmMap [y * mapWidth + x] = regions [i].colour;
						break;
					}
				}
			}
		}

		MapDisplay display = FindObjectOfType<MapDisplay> ();
		if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMap(noiseMap));
		} else if (drawMode == DrawMode.ColourMap) {
			display.DrawTexture (TextureGenerator.TextureFromColourMap (colourmMap, mapWidth, mapHeight));
		}
	}

	// Clamps the public variables
	void OnValidate() {
		if (mapWidth < 1) {
			mapWidth = 1;
		}
		if (mapHeight < 1) {
			mapHeight = 1;
		}
		if (lacunarity < 1) {
			lacunarity = 1;
		}
		if (octaves < 0) {
			octaves = 0;
		}
	}
}

[System.Serializable]
public struct TerrainType {
	public string name;
	public float height;
	public Color colour;
}

