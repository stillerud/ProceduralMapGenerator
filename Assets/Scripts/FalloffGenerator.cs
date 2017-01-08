using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Falloff map generator.
/// </summary>
public static class FalloffGenerator {
	
	/// <summary>
	/// Generates a falloff map.
	/// </summary>
	/// <returns>A falloff map.</returns>
	/// <param name="size">Size.</param>
	public static float[,] GenerateFalloffMap(int size){
		float[,] map = new float[size, size];
		for (int i = 0; i < size; i++) {
			for (int j = 0; j < size; j++) {
				float x = i/(float)size * 2 - 1;
				float y = j/(float)size * 2 - 1;

				// Closest to edge of square
				float value = Mathf.Max (Mathf.Abs (x), Mathf.Abs (y));
				map[i,j] = Evaluate(value);
			}
		}

		return map;
	}

	/// <summary>
	/// Equation for a curve that values evaluates through. Equation created on desmos.com
	/// </summary>
	/// <param name="value">Value.</param>
	static float Evaluate(float value) {
		float a = 3;
		float b = 2.2f;

		return Mathf.Pow (value, a) / (Mathf.Pow (value, a) + Mathf.Pow (b - b * value, a));
	}
}
