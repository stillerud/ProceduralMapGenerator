using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Noisemap generation class
public static class Noise {

	public static float[,] GenerateNoiseMap( int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset) {
		float[,] noiseMap = new float[mapWidth,mapHeight];

		// Set the noise seed and offset for each octave
		System.Random prng = new System.Random (seed);
		Vector2[] octaveOffsets = new Vector2[octaves];
		for (int i = 0; i < octaves; i++) {
			float offsetX = prng.Next (-100000, 100000) + offset.x;
			float offsetY = prng.Next (-100000, 100000) + offset.y;
			octaveOffsets [i] = new Vector2 (offsetX, offsetY);
		}
			
		// We can't have division by 0
		if (scale <= 0) {
			scale = 0.0001f;
		}

		// Variables used to normalize the noisemap between 0 and 1
		float maxNoiseHeight = float.MinValue;
		float minNoiseHeight = float.MaxValue;

		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;

		// Loop through each pixel in the height map array and generate a perlin noise value
		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {

				// Variables used to control the noise map characteristics
				float amplitude = 1;
				float frequency = 1;
				float noiseHeight = 0;

				// Loop through each octave and generate noise value
				for (int i = 0; i < octaves; i++) {
					float sampleX = (x-halfWidth) / scale * frequency + octaveOffsets[i].x;
					float sampleY = (y-halfHeight) / scale * frequency + octaveOffsets[i].y;

					float perlinValue = Mathf.PerlinNoise (sampleX, sampleY) * 2 - 1; // Get perlin noise value
					noiseHeight += perlinValue * amplitude;

					amplitude *= persistence; // persistance values from 0-1 so amplitute decreases for each octave.
					frequency *= lacunarity; // lacunarity should be greater than 1 so that the frequency increases for each octave.
				}

				// Find lowest and highest points
				if (noiseHeight > maxNoiseHeight) {
					maxNoiseHeight = noiseHeight;
				} else if (noiseHeight < minNoiseHeight) {
					minNoiseHeight = noiseHeight;
				}
				noiseMap [x, y] = noiseHeight;
			}
		}
		// Normalize the noisemap between 0 and 1
		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				noiseMap[x,y] = Mathf.InverseLerp (minNoiseHeight, maxNoiseHeight, noiseMap[x,y]);
			}
		}
		return noiseMap;
	}
}
