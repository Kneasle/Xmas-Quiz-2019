using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using SudokuCore.Populating;
using SudokuCore.PuzzleMaking;
using SudokuCore.Solving;

namespace SudokuCore {
	public class Specification {
		// Starting values
		public char [] alphabet;
		public Shape shape;
		public int [] initial_grid;
		public int seed;
		public int [] letters;
		public int min_clues = 0;

		public Puzzle Generate (bool verbose = false, bool print_fixed_cells = false) {
			int [] solution_grid = new FastPopulator ().PopulateGrid (shape, new PopulatorArgs () { alphabet = alphabet, print_fixed_cells = print_fixed_cells }, initial_grid);

			if (verbose)
				shape.Print (solution_grid, alphabet);

			int [] puzzle_grid = PuzzleMaker.GeneratePuzzle (shape, solution_grid, seed, min_clues);

			if (verbose)
				shape.Print (puzzle_grid, alphabet);
			
			int [] output_cells = new int [letters == null ? 0 : letters.Length];

			if (letters != null) {
				if (pick_numbers_randomly) {
					List<int> [] letter_locations = new List<int> [shape.group_size];

					for (int i = 0; i < shape.group_size; i++) {
						letter_locations [i] = new List<int> ();

						for (int j = 0; j < shape.num_cells; j++) {
							if (solution_grid [j] == i + 1 && puzzle_grid [j] == 0) {
								letter_locations [i].Add (j);
							}
						}
					}

					Random rng = new Random (seed);

					for (int i = 0; i < letters.Length; i++) {
						int l = letters [i];
						int ind = rng.Next (letter_locations [l].Count);
						int loc = letter_locations [l] [ind];

						letter_locations [l].RemoveAt (ind);

						output_cells [i] = loc;
					}
				} else {
					new RecursiveSolver ().Solve (
						shape,
						puzzle_grid,
						new SolverArgs () { rng = new Random (seed + 1), max_solutions = 1, iteration_limit = 50000 },
						out int diff,
						out int [] [] cell_fill_orders
					); // We only care about the cell_fill_orders - we know what the solution grid is

					List<int> fill_orders = cell_fill_orders [0].Reverse ().ToList ();
					for (int i = 0; i < letters.Length; i++) {
						int letter = letters [i];

						int last_index = 0;
						for (int j = 0; j < fill_orders.Count; j++) {
							// Console.WriteLine (fill_orders [j] + " " + solution_grid [fill_orders [j]]);

							if (solution_grid [fill_orders [j]] == letter + 1) {
								last_index = j;

								break;
							}
						}

						int last_cell = fill_orders [last_index];
						fill_orders.RemoveAt (last_index);

						Console.WriteLine (alphabet [letter] + " => " + last_cell);

						output_cells [i] = last_cell;
					}
				}
			}

			return new Puzzle (this, puzzle_grid, solution_grid, output_cells);
		}

		public Specification (char [] alphabet, Shape shape, int [] initial_grid, int seed, int [] letters, int min_clues) {
			this.alphabet = alphabet;
			this.shape = shape;
			this.initial_grid = initial_grid;
			this.seed = seed;
			this.letters = letters;
			this.min_clues = min_clues;
		}

		public static Specification LoadFromFile (string path, Dictionary<string, string> word_locations, bool auto_generate = false)
			=> LoadFromParsedFile (Parser.ParseFile (path), path.Substring (path.IndexOf ('.') - 2, 2), word_locations, auto_generate);

		public static Specification LoadFromString (string data, string name, Dictionary<string, string> word_locations, bool auto_generate = false)
			=> LoadFromParsedFile (Parser.ParseString (data), name, word_locations, auto_generate);

		public static Specification LoadFromParsedFile (Dictionary<string, List<string>> parsed_file, string name, Dictionary<string, string> word_locations, bool auto_generate = false) {
			Shape shape = null;
			int [] initial_grid = null;
			char [] alphabet = null;
			bool [] highlight = null;
			int [] letters = null;

			HashSet<char> alphabet_set = new HashSet<char> ();

			void GenerateAlphabetFromString (string s) {
				foreach (char c in s) {
					if (!Parser.spacers.Contains (c) && !Parser.whitespace_chars.Contains (c)) {
						alphabet_set.Add (c);
					}
				}
			}

			// Read Shape
			string [] shape_string = parsed_file [Parser.Headers.shape] [0].Split (' ');

			if (shape_string.Length == 1) {
				shape = Shape.LoadFromFile (Parser.shape_directory + shape_string [0] + ".txt");
			} else {
				if (shape_string [0] == "standard") {
					shape = Shape.StandardGrid (
						int.Parse (shape_string [1]),
						int.Parse (shape_string [2]),
						parsed_file.ContainsKey (Parser.Headers.groups) ? parsed_file [Parser.Headers.groups] : null
					);
				} else if (shape_string [0] == "square") {
					shape = Shape.SquareGrid (
						int.Parse (shape_string [1]),
						parsed_file.ContainsKey (Parser.Headers.groups) ? parsed_file [Parser.Headers.groups] : null
					);
				} else if (shape_string [0] == "jigsaw") {
					shape = Shape.JigsawGrid (
						int.Parse (shape_string [1]),
						parsed_file [Parser.Headers.groups]
					);
				} else if (shape_string [0] == "star") {
					shape = Shape.StarGrid (
						int.Parse (shape_string [1]),
						int.Parse (shape_string [2]),
						parsed_file.ContainsKey (Parser.Headers.petal) ? parsed_file [Parser.Headers.petal] [0] : null,
						parsed_file.ContainsKey (Parser.Headers.force_symmetry)
					);
				} else {
					throw new Exception ("Unknown shape string: '" + string.Join (" ", shape_string) + "'.");
				}
			}

			// Check if symmetry is overloaded
			if (parsed_file.ContainsKey (Parser.Headers.symmetry)) {
				shape.symmetry = Symmetry.GetSymmetryFromParsedFile (parsed_file, shape.group_size, shape.num_cells);
			}

			// Check if the alphabet exists
			if (parsed_file.ContainsKey (Parser.Headers.alphabet)) {
				GenerateAlphabetFromString (parsed_file [Parser.Headers.alphabet] [0]);
			}

			// Parse words
			if (parsed_file.ContainsKey (Parser.Headers.words)) {
				string s = string.Join ("\n", parsed_file [Parser.Headers.words]);

				GenerateAlphabetFromString (s);

				foreach (string word in s.Split ('\n')) {
					if (word == "")
						continue;

					if (word_locations.ContainsKey (word)) {
						throw new Exception ("Word '" + word + "' was repeated in puzzles " + word_locations [word] + " and " + name + ".");
					} else {
						word_locations.Add (word, name);
					}
				}
			}

			// Read highlight
			highlight = new bool [shape.num_cells];

			if (parsed_file.ContainsKey (Parser.Headers.highlight)) {
				int i = 0;

				foreach (string s in parsed_file [Parser.Headers.highlight]) {
					foreach (char c in s) {
						if (c == '#') {
							highlight [i] = true;

							i += 1;
						} else if (c == '.') {
							i += 1;
						}
					}
				}
			}

			shape.highlight = highlight;

			// Read fixed digits
			if (parsed_file.ContainsKey (Parser.Headers.fixed_digits)) {
				string s = parsed_file [Parser.Headers.fixed_digits] [0];

				if (alphabet == null) {
					GenerateAlphabetFromString (s);
				}

				// Finish the alphabet
				alphabet = alphabet_set.ToArray ();

				// Generate lookup table for faster parsing
				Dictionary<char, int> indices = new Dictionary<char, int> (shape.group_size);

				for (int i = 0; i < shape.group_size; i++) {
					indices.Add (alphabet [i], i);
				}

				initial_grid = s.Where (c => Parser.spacers.Contains (c) || indices.ContainsKey (c)).Select (c => Parser.spacers.Contains (c) ? 0 : indices [c] + 1).ToArray ();
			} else {
				// Finish the alphabet
				alphabet = alphabet_set.ToArray ();
			}

			// Read letters
			if (parsed_file.ContainsKey (Parser.Headers.letters)) {
				List<int> ls = new List<int> ();

				foreach (string s in parsed_file [Parser.Headers.letters]) {
					foreach (char c in s) {
						if (alphabet_set.Contains (c)) {
							for (int i = 0; i < alphabet.Length; i++) {
								if (alphabet [i] == c) {
									ls.Add (i);

									break;
								}
							}
						}
					}
				}

				letters = ls.ToArray ();
			}

			Utils.AssertEqual (alphabet.Length, shape.group_size);

			int seed = 0;
			if (parsed_file.ContainsKey (Parser.Headers.seed)) {
				string s = parsed_file [Parser.Headers.seed] [0];

				if (!int.TryParse (s, out seed))
					seed = s.GetHashCode ();
			}

			int min_clues = parsed_file.ContainsKey (Parser.Headers.min_clues) ? int.Parse (parsed_file [Parser.Headers.min_clues] [0]) : 0;

			Specification spec = new Specification (alphabet, shape, initial_grid, seed, letters, min_clues);

			if (auto_generate)
				spec.Generate ();

			return spec;
		}

		static bool pick_numbers_randomly = true;
	}
}
