using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

using SudokuCore;
using SudokuCore.Populating;
using SudokuCore.Solving;

namespace Generator {
	class RunGenerator {
		static void Main (string [] args) {
			// BashThroughSudokus (base_path + @"Packs\5.txt", 5000);
			// SpeedTest ();
			GenerateQuizData ();

			Console.ReadKey ();
		}

		static void GenerateQuizData () {
			string [] words = File.ReadAllLines (words_path + "list.txt");

			Dictionary<string, string> word_dict = CompileQuizPuzzles (words);

			CompileWords (words, word_dict);

			Console.WriteLine ("Done!");
		}

		static void CompileWords (string [] words, Dictionary<string, string> word_dict, bool spread_lex = false) {
			string padding = " | ";

			var lines = words.Where (s => s.Length > 0).Select (s => {
				List<char> lex = s.Distinct ().ToList ();

				lex.Sort ();

				if (lex [0] == ' ') {
					lex.RemoveAt (0);
				}

				return (lex.Count < s.Replace (" ", "").Length, new string (lex.ToArray ()), s);
			}).OrderBy (x => x.Item2);

			int length = lines.Select (x => x.Item2.Length).Max ();

			StringBuilder builder = new StringBuilder ();

			foreach (var l in lines) {
				(bool b, string lex, string word) = l;

				if (word_dict.ContainsKey (word)) {
					builder.Append (word_dict [word]);
				} else {
					builder.Append ("  ");
				}

				builder.Append (padding);

				builder.Append (b ? " " : "*");
				builder.Append (padding);

				if (spread_lex) {
					int j = 0;

					for (int i = 0; i < 26; i++) {
						char c = (char)('A' + i);

						if (j < lex.Length && lex [j] == c) {
							builder.Append (c);
							j++;
						} else {
							builder.Append (" ");
						}
					}
				} else {
					builder.Append (lex);
					builder.Append (new string (' ', length - lex.Length));
				}

				builder.Append (padding);
				builder.Append (word);
				builder.Append ("\r\n");
			}

			File.WriteAllText (words_path + "list_compiled.txt", builder.ToString ());
		}

		static void ConvertImages () {
			ProcessStartInfo start = new ProcessStartInfo {
				FileName = @"C:\Users\kneas\AppData\Local\Programs\Python\Python36-32\python.exe",
				Arguments = string.Format ("\"C:\\Users\\kneas\\Dropbox\\Programming\\Python\\Quiz 2019\\svgconverter.py\""),
				UseShellExecute = false,
				RedirectStandardOutput = true
			};

			using (Process process = Process.Start (start)) {
				using (StreamReader reader = process.StandardOutput) {
					string result = reader.ReadToEnd ();

					Console.Write (result);
				}
			}
		}

		static Dictionary<string, string> CompileQuizPuzzles (string [] words) {
			Dictionary<string, string> word_locations = new Dictionary<string, string> ();
			List<(string, char [])> alphabets = new List<(string, char [])> ();

			List<string> puzzles_to_vet = new List<string> ();

			foreach (string f in Directory.GetFiles (spec_path)) {
				// Calculate data from the file path
				int slash_ind = f.LastIndexOf ('\\');

				string file_name = f.Substring (slash_ind + 1);

				// Allow files to be commented out
				if (file_name.StartsWith ("--"))
					continue;

				string diff_path = spec_diff_path + file_name;

				string name = f.Substring (f.IndexOf ('.') - 2, 2);

				Console.WriteLine ("(" + name + ")");

				// Run the diff to see whether recalculation is necessary
				string new_file_contents = Utils.ObliterateThoseBloodyCarriageReturns (File.ReadAllText (f));

				bool needs_recalculation = true;

				if (File.Exists (diff_path)) {
					string old_file_contents = Utils.ObliterateThoseBloodyCarriageReturns (File.ReadAllText (diff_path));
					
					needs_recalculation = new_file_contents != old_file_contents;
				}

				// See if recalculation is asked for
				var parsed_file = Parser.ParseString (new_file_contents);

				if (parsed_file.ContainsKey (Parser.Headers.recalculate))
					needs_recalculation = true;

				if (!parsed_file.ContainsKey (Parser.Headers.vetted))
					puzzles_to_vet.Add (name);

				// Always run Specification.LoadFromString, because it will update word_locations
				Specification spec = Specification.LoadFromParsedFile (parsed_file, name, word_locations);

				alphabets.Add ((name, spec.alphabet));

				// If necessary, recalculate
				if (needs_recalculation || override_diff) {
					try {
						// This line will throw the exceptions
						Puzzle puzzle = spec.Generate (true, parsed_file.ContainsKey (Parser.Headers.print_fixed));

						puzzle.SaveToFile (puzzle_path + file_name);

						puzzle.SaveSVGToFile (svg_path + file_name.Substring (0, 2) + ".svg", false);
						puzzle.SaveSVGToFile (solutions_path + file_name.Substring (0, 2) + ".svg", true);

						// Save the new file into the diff folder
						File.WriteAllText (diff_path, new_file_contents);
					} catch (DuplicateElementInGroupException e) {
						Console.WriteLine ("<NOT SOLUBLE!>: '" + spec.alphabet [e.element] + "' is duplicated in a group.");
					} catch (GridNotSolubleException e) {
						Console.WriteLine ("<NOT SOLUBLE!>: " + e.Message);
					}
					
					Console.WriteLine ("\n\n\n");
				} else {
					Console.WriteLine ("No need to recalculate.");
				}
			}

			// Save alphabets
			string [] lines = new string [alphabets.Count];

			for (int i = 0; i < alphabets.Count; i++) {
				List<char> cs = alphabets [i].Item2.ToList ();

				cs.Sort ();

				lines [i] = alphabets [i].Item1 + ": " + string.Join ("", cs);
			}

			File.WriteAllLines (alphabets_path, lines);
			File.WriteAllLines (to_vet_path, puzzles_to_vet);

			Console.WriteLine ("Converting Images");

			ConvertImages ();

			return word_locations;
		}

		static void SpeedTest () {
			GenerateSomeShape (Shape.LoadFromFile (shape_path + "cube.txt"));
			GenerateSomeShape (Shape.LoadFromFile (shape_path + "hexagon.txt"));
			GenerateSomeShape (Shape.LoadFromFile (shape_path + "4x4 jigsaw.txt"));
			GenerateSomeShape (Shape.StandardGrid (2, 3));
			GenerateSomeShape (Shape.StandardGrid (4, 3));
			GenerateSomeShape (Shape.StandardGrid (4, 5));
			GenerateSomeShape (Shape.StandardGrid (3, 7));
			GenerateSomeShape (Shape.StandardGrid (6, 4));
			GenerateSomeShape (Shape.SquareGrid (2));
			GenerateSomeShape (Shape.SquareGrid (3));
			GenerateSomeShape (Shape.SquareGrid (4));
			GenerateSomeShape (Shape.SquareGrid (5));
		}

		static void GenerateSomeShape (Shape shape) {
			Stopwatch stopwatch = Stopwatch.StartNew ();
			int [] slow_grid = null;
			int [] fast_grid = null;

			// slow_grid = new NaiveTreePopulator ().PopulateGrid (shape, new PopulatorArgs ());

			double slow_time = (double)stopwatch.ElapsedTicks / Stopwatch.Frequency;

			stopwatch.Restart ();

			try {
				fast_grid = new FastPopulator ().PopulateGrid (shape, new PopulatorArgs ());
			} catch (GridNotSolubleException) {
				Console.WriteLine ("! Grid cannot be solved.");
			}

			double fast_time = (double)stopwatch.ElapsedTicks / Stopwatch.Frequency;

			Utils.PrintSideBySide (
				"   ",
				slow_grid == null ? "<failed>" : shape.GetString (slow_grid),
				fast_grid == null ? "<failed>" : shape.GetString (fast_grid)
			);

			Console.WriteLine ("Constrainedness: " + (double)shape.group_size / shape.cells_by_group.GetLength (0));
			Console.WriteLine ("Naive Populator: " + slow_time);
			Console.WriteLine ("Beefy Populator: " + fast_time);
		}

		static void SolveSome3x3Puzzle () {
			// Solve something
			Shape shape3x3 = Shape.SquareGrid (3);

			int [] clues = RecursiveSolver.Get3x3Grid (base_path + @"3x3 Puzzles\hard.txt");

			int [] [] solutions = new RecursiveSolver ().Solve (shape3x3, clues, new SolverArgs () { max_solutions = 2 }, out int difficulty, out int [] [] cell_orders);

			shape3x3.Print (clues);

			Console.WriteLine (difficulty);

			foreach (int [] sol in solutions) {
				shape3x3.Print (sol);
			}
		}

		static void BashThroughSudokus (string path, int number, int start = 0) {
			Shape shape = Shape.SquareGrid (3);

			string [] lines = File.ReadAllLines (path);

			ISolver solver = new RecursiveSolver ();
			SolverArgs args = new SolverArgs () { use_set_elimination = false };

			Stopwatch stopwatch = Stopwatch.StartNew ();

			for (int i = start; i < number + start; i++) {
				int [] grid = lines [i].Select (x => int.Parse (x.ToString ())).ToArray ();

				int [] [] solution = solver.Solve (shape, grid, args, out int diff, out int [] [] fill_orders);

				if (solution.Length != 1)
					Console.WriteLine ("!");

				if ((i + 1) % 100 == 0) {
					Console.Write (".");
				}
				if ((i + 1) % 500 == 0) {
					Console.Write (" ");
				}
				if ((i + 1) % 1000 == 0) {
					Console.Write ("\n");
				}
				if ((i + 1) % 5000 == 0) {
					Console.Write ("\n");
				}
			}

			double n = stopwatch.Elapsed.TotalSeconds / number;

			Console.WriteLine ("\n" + n * 1000 + "ms per sudoku.");
			Console.WriteLine (1 / n + " sudoku per second.");
		}

		const string base_path = @"C:\Sudoku\";

		const string quiz_path = base_path + @"Quiz Specifics\";

		const string shape_path = quiz_path + @"Shapes\";
		const string words_path = quiz_path + @"Words\";
		const string spec_path = quiz_path + @"Specifications\";
		const string spec_diff_path = quiz_path + @"SpecDiff\";
		const string solutions_path = quiz_path + @"Solutions\";
		const string alphabets_path = quiz_path + "Alphabets.txt";
		const string to_vet_path = quiz_path + "Unvetted.txt";

		const string godot_path = @"C:\Users\kneas\Dropbox\Programming\Godot\Xmas Quiz 2019\";

		const string puzzle_path = godot_path + @"Puzzles\";
		const string svg_path = quiz_path + @"SVGs\";

		const bool override_diff = true;
	}
}
