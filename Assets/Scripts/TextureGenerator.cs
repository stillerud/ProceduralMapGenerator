using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Assigns different maps to a Texture2D so we can view it in the editor
/// </summary>
public static class TextureGenerator {

	/// <summary>
	/// Generates a textures from a colour map.
	/// </summary>
	/// <returns>A Texture2D with the colour map.</returns>
	/// <param name="colourMap">Colour map.</param>
	/// <param name="width">Width of the map.</param>
	/// <param name="height">Height of the map.</param>
	public static Texture2D TextureFromColourMap( Color[] colourMap, int width, int height) {
		Texture2D texture = new Texture2D (width, height);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels (colourMap);
		texture.Apply ();
		return texture;
	}

	/// <summary>
	/// Generates a textures from a height map.
	/// </summary>
	/// <returns>A Texture2D with the height map.</returns>
	/// <param name="heightMap">Height map.</param>
	public static Texture2D TextureFromHeightMap(float[,] heightMap) {
		int width = heightMap.GetLength (0);
		int height = heightMap.GetLength (1);

		Color[] colourMap = new Color[width * height];
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				colourMap [y * width + x] = Color.Lerp (Color.black, Color.white, heightMap [x, y]);
			}
		}

		return TextureFromColourMap (colourMap, width, height);
	}
}
