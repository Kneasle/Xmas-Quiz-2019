using System;
using System.Collections.Generic;
using System.Text;

namespace SudokuCore {
	public struct Vector2 {
		public float x;
		public float y;

		public override string ToString () => "(" + x + ", " + y + ")";

		public static Vector2 operator + (Vector2 a, Vector2 b) => new Vector2 (a.x + b.x, a.y + b.y);
		public static Vector2 operator - (Vector2 a, Vector2 b) => new Vector2 (a.x - b.x, a.y - b.y);

		public static Vector2 operator - (Vector2 a) => new Vector2 (-a.x, -a.y);

		public static Vector2 operator * (Vector2 vec, float scalar) => new Vector2 (vec.x * scalar, vec.y * scalar);
		public static Vector2 operator * (float scalar, Vector2 vec) => new Vector2 (vec.x * scalar, vec.y * scalar);

		public static Vector2 operator / (Vector2 vec, float scalar) => new Vector2 (vec.x / scalar, vec.y / scalar);

		public static Vector2 Min (IEnumerable<Vector2> vectors) {
			Vector2 min = new Vector2 (float.PositiveInfinity, float.PositiveInfinity);

			foreach (Vector2 v in vectors) {
				if (v.x < min.x)
					min.x = v.x;

				if (v.y < min.y)
					min.y = v.y;
			}

			return min;
		}

		public static Vector2 Max (IEnumerable<Vector2> vectors) {
			Vector2 max = new Vector2 (float.NegativeInfinity, float.NegativeInfinity);

			foreach (Vector2 v in vectors) {
				if (v.x > max.x)
					max.x = v.x;

				if (v.y > max.y)
					max.y = v.y;
			}

			return max;
		}

		public static float Dot (Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;

		public static Vector2 Lerp (Vector2 a, Vector2 b, float t) => a * (1 - t) + b * t;

		public static Vector2 FromPolar (float a, float r = 1) => new Vector2 (r * (float)Math.Sin (a), - r * (float)Math.Cos (a));

		public static Vector2 left => new Vector2 (-1, 0);
		public static Vector2 right => new Vector2 (1, 0);
		public static Vector2 up => new Vector2 (0, -1);
		public static Vector2 down => new Vector2 (0, 1);

		public static Vector2 zero => new Vector2 (0, 0);
		public static Vector2 one => new Vector2 (1, 1);

		public Vector2 (float x, float y) {
			this.x = x;
			this.y = y;
		}
	}
}
