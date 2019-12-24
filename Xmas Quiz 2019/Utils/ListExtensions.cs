using System;
using System.Collections.Generic;
using System.Text;

namespace SudokuCore {
	public static class ListExtensions {
		public static void Shuffle<T> (this IList<T> list, Random rng) {
			for (var i = 0; i < list.Count - 1; i++)
				list.Swap (i, rng.Next (i, list.Count));
		}

		public static IEnumerable<T> GetRow<T> (this T [,] array, int index) {
			for (int i = 0; i < array.GetLength (1); i++) {
				yield return array [index, i];
			}
		}

		public static IEnumerable<IEnumerable<T>> GetAllRows<T> (this T [,] array) {
			for (int i = 0; i < array.GetLength (0); i++) {
				yield return array.GetRow (i);
			}
		}

		public static void Swap<T> (this IList<T> list, int i, int j) {
			var temp = list [i];
			list [i] = list [j];
			list [j] = temp;
		}
	}
}
