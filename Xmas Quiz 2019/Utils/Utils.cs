using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SudokuCore {
	public static class Utils {
		public static int [] RandomPermutation (Random rng, int length, int start = 1) {
			int [] parts = new int [length];

			for (int i = 0; i < length; i++) {
				parts [i] = i + start;
			}

			parts.Shuffle (rng);

			return parts;
		}

		public static string GetSideBySideString (string padding, string [] strings) {
			string [] [] lines = new string [strings.Length] [];

			int [] widths = new int [strings.Length];

			for (int i = 0; i < strings.Length; i++) {
				lines [i] = strings [i].Split ('\n');
				widths [i] = lines [i].Select (x => x.Length).Max ();
			}

			int depth = lines.Select (x => x.Length).Max ();
			int total_width = widths.Sum () + (widths.Length - 1) * padding.Length;

			StringBuilder builder = new StringBuilder (total_width * depth + depth - 1);

			for (int i = 0; i < depth; i++) {
				bool is_first = true;

				for (int j = 0; j < strings.Length; j++) {
					if (!is_first)
						builder.Append (padding);

					string line = i < lines [j].Length ? lines [j] [i] : "";

					builder.Append (line);
					builder.Append (new string (' ', widths [j] - line.Length));

					is_first = false;
				}

				builder.Append ("\n");
			}

			return builder.ToString ();
		}

		public static void PrintSideBySide (string padding, params string [] strings) => Console.WriteLine (GetSideBySideString (padding, strings));

		public static void AssertEqual (object left, object right) {
			if (left == null) {
				if (right != null)
					throw new AssertTrippedExeception (left + " != " + right);
			} else {
				if (!left.Equals (right))
					throw new AssertTrippedExeception (left + " != " + right);
			}
		}

		public static int ProperMod (int x, int n) {
			int o = x % n;

			if (o < 0)
				o += n;

			return o;
		}

		public static string ObliterateThoseBloodyCarriageReturns (string s) => s.Replace ("\r", "");
	}

	public class AssertTrippedExeception : Exception {
		public AssertTrippedExeception (string message) : base (message) { }
	}
}
