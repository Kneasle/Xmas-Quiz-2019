using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SudokuCore {
	public static class Parser {
		public static Dictionary<string, List<string>> ParseFile (string path) 
			=> ParseString (File.ReadAllLines (path));

		public static Dictionary<string, List<string>> ParseString (string s) 
			=> ParseString (s.Split ('\n'));
		
		public static int [] [] LexifyString (string s, out int length) {
			// Create compressed version (i.e. remove whitespace)
			StringBuilder compressed_string_builder = new StringBuilder (s.Length);

			foreach (char c in s) {
				if (!whitespace_chars.Contains (c)) {
					compressed_string_builder.Append (c);
				}
			}

			string compressed_string = compressed_string_builder.ToString ();

			length = compressed_string.Length;

			// Generate sets of indices used by each char
			Dictionary<char, List<int>> group_table = new Dictionary<char, List<int>> ();

			for (int j = 0; j < compressed_string.Length; j++) {
				char c = compressed_string [j];

				if (spacers.Contains (c))
					continue;

				if (group_table.ContainsKey (c)) {
					group_table [c].Add (j);
				} else {
					group_table [c] = new List<int> { j };
				}
			}

			// Consume and convert each group
			List<int> [] new_groups = group_table.Values.ToArray ();

			List<int []> groups = new List<int []> ();

			foreach (List<int> group in new_groups) {
				int [] converted_group = new int [group.Count];

				for (int j = 0; j < group.Count; j++) {
					converted_group [j] = group [j];
				}

				groups.Add (converted_group);
			}

			return groups.ToArray ();
		}

		public static Dictionary<string, List<string>> ParseString (string [] lines) {
			Dictionary<string, List<string>> output = new Dictionary<string, List<string>> ();

			List<string> current_section = new List<string> ();
			string current_header = null;

			void SaveSection () {
				if (current_section.Count == 0)
					return;

				string s = string.Join ("\n", current_section).TrimEnd ();

				if (current_header != null) {
					if (output.ContainsKey (current_header)) {
						output [current_header].Add (s);
					} else {
						output.Add (current_header, new List<string> { s });
					}
				}

				current_section.Clear ();
			}

			for (int i = 0; i < lines.Length + 1; i++) {
				string line = i < lines.Length ? lines [i] : "";

				if (line.StartsWith (">> ")) {
					SaveSection ();

					current_header = line.Substring (3);
				} else {
					current_section.Add (line);
				}
			}

			SaveSection ();

			return output;
		}

		public static string CompileString (Dictionary<string, List<string>> sections) {
			StringBuilder builder = new StringBuilder ();

			foreach (string key in sections.Keys) {
				List<string> values = sections [key];

				foreach (string s in values) {
					builder.Append (">> ");
					builder.Append (key);
					builder.Append ("\n");
					builder.Append (s);
					builder.Append (s.EndsWith ("\n") ? "\n" : "\n\n");
				}
			}

			builder.Replace ("\n", "\r\n");

			return builder.ToString ();
		}

		public static void CompileToFile (Dictionary<string, List<string>> sections, string path) 
			=> File.WriteAllText (path, CompileString (sections));

		public static class Headers {
			public const string format = "FORMAT";
			public const string groups = "GROUPS";
			public const string shape = "SHAPE";
			public const string highlight = "HIGHLIGHT";
			public const string fixed_digits = "FIX";
			public const string alphabet = "ALPHABET";
			public const string words = "WORDS";
			public const string seed = "SEED";
			public const string symmetry = "SYMMETRY";
			public const string solution = "SOLUTION";
			public const string puzzle = "PUZZLE";
			public const string recalculate = "RECALCULATE";
			public const string print_fixed = "PRINT_FIXED";
			public const string petal = "PETAL";
			public const string points = "POINTS";
			public const string cell_points = "CELL_POINTS";
			public const string box_points = "BOX_POINTS";
			public const string box_corners = "BOX_CORNERS";
			public const string box_size = "BOX_SIZE";
			public const string letters = "LETTERS";
			public const string output_locations = "OUTPUT_LOCATIONS";
			public const string output_solutions = "OUTPUT_SOLUTIONS";
			public const string vetted = "VETTED";
			public const string min_clues = "CLUES";
			public const string force_symmetry = "FORCE_SYMMETRY";
		}

		public static class Symmetry {
			public const string none = "none";
			public const string rotational_180 = "rot 180";
			public const string rotational_90 = "rot 90";
			public const string horizontal_reflection = "ref h";
			public const string vertical_reflection = "ref v";
			public const string double_reflection = "ref d";
			public const string mega = "mega";
		}

		public const string alpha = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!\"£$%^&*()-=_+[]{};'#:@~,./<?>\\|`¬";
		public const string alpha_with_dot = "." + alpha;

		public const string petal_last_alpha = "1234567890-=!\"£$%^&*()_+`";
		public const string petal_current_alpha = "abcdefghijklmnopqrstuvwxyz";
		public const string petal_next_alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

		public const string spacers = ".-#";

		public const string whitespace_chars = " \t\r\n";

		public static string shape_directory = @"C:\Sudoku\Quiz Specifics\Shapes\";
	}
}
