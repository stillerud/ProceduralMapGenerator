using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Noisemap class
public static class Noise {

	public static float[,] GenerateNoiseMap( int mapWidth, int mapHeight, float scale) {
		float[,] noiseMap = new float[mapWidth,mapHeight];

		// Can't have division by 0
		if (scale <= 0) {
			scale = 0.0001f;
		}

		// Loop through each pixel in the height map array and generate a perlin noise value
		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				float sampleX = x / scale;
				float sampleY = y / scale;

				float perlinValue = Mathf.PerlinNoise (sampleX, sampleY);
				noiseMap [x, y] = perlinValue;
			}
		}

		return noiseMap;

	}
}
