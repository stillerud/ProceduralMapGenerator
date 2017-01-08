using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Class that generates a perlin noise map
/// </summary>
public static class Noise {

	public enum NormalizeMode {Local, Global};

	/// <summary>
	/// Generates a perlin noise map.
	/// </summary>
	/// <returns>The noise map.</returns>
	/// <param name="mapWidth">Width of the map.</param>
	/// <param name="mapHeight">Height of the map.</param>
	/// <param name="seed">Randomizes the map.</param>
	/// <param name="scale">Scale of the map.</param>
	/// <param name="octaves">Nunmber of times the map is multiplied onto itself.</param>
	/// <param name="persistence">How much amplitude decreases with each octave.</param>
	/// <param name="lacunarity">Controls the frequency so that it increases with each octave.</param>
	/// <param name="offset">Offsets the map.</param>
	/// <param name="normalizeMode">What normalization calculation to use when figuring out minimum and maximum values. For only one noise map use Local. For multiple chunks or an endless terrain, use Global.</param>
	public static float[,] GenerateNoiseMap( int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset, NormalizeMode normalizeMode) {
		float[,] noiseMap = new float[mapWidth,mapHeight];

		// Set the noise seed and offset list for each octave
		System.Random prng = new System.Random (seed);
		Vector2[] octaveOffsets = new Vector2[octaves];

		// Variables used to control the noise map characteristics
		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

		// Loop through and set offsets for each octave
		for (int i = 0; i < octaves; i++) {
			float offsetX = prng.Next (-100000, 100000) + offset.x;
			float offsetY = prng.Next (-100000, 100000) - offset.y;
			octaveOffsets [i] = new Vector2 (offsetX, offsetY);

			maxPossibleHeight += amplitude;
			amplitude *= persistence;
		}

		// We can't have division by 0
		if (scale <= 0) {
			scale = 0.0001f;
		}

		// Variables used to normalize the noisemap between 0 and 1
		float maxLocalNoiseHeight = float.MinValue;
		float minLocalNoiseHeight = float.MaxValue;

		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;

		// Loop through each pixel in the height map array and generate a perlin noise value
		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {

				// Reset to 1
				amplitude = 1;
				frequency = 1;

				float noiseHeight = 0;

				// Loop through each octave and generate noise values
				for (int i = 0; i < octaves; i++) {
					float sampleX = (x-halfWidth + octaveOffsets[i].x) / scale * frequency;
					float sampleY = (y-halfHeight + octaveOffsets[i].y) / scale * frequency;

					float perlinValue = Mathf.PerlinNoise (sampleX, sampleY) * 2 - 1; // Get perlin noise value
					noiseHeight += perlinValue * amplitude;

					amplitude *= persistence; // persistance values from 0-1 so amplitude decreases for each octave.
					frequency *= lacunarity; // lacunarity should be greater than 1 so that the frequency increases for each octave.
				}

				// Find lowest and highest points
				if (noiseHeight > maxLocalNoiseHeight) {
					maxLocalNoiseHeight = noiseHeight;
				} else if (noiseHeight < minLocalNoiseHeight) {
					minLocalNoiseHeight = noiseHeight;
				}
				noiseMap [x, y] = noiseHeight;
			}
		}
		// Normalize the noisemap between 0 and 1
		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				if (normalizeMode == NormalizeMode.Local) {
					noiseMap [x, y] = Mathf.InverseLerp (minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap [x, y]);
				} else {
					float normalizedHeight = (noiseMap [x, y] + 1) / maxPossibleHeight;
					noiseMap [x, y] = Mathf.Clamp(normalizedHeight,0, int.MaxValue);
				}
			}
		}
		return noiseMap;
	}
}
