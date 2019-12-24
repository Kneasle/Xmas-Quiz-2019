using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SudokuCore.Solving {
	public class RecursiveSolver : ISolver {
		public class TooManyIterationsException : Exception { }

		private class Partial {
			private bool [,] pencilling_freedoms;
			private bool [,] set_penned_in; // Gets assigned to but never used.  Remove?
			private int [] pencilling_freedom_counts;
			public int [] board;
			public int [] cell_fill_order;
			private int depth = 0;
			private RecursiveSolver solver;
			private readonly bool is_automatically_unsolvable = false;

			private Shape shape;

			private bool is_solved {
				get {
					for (int i = 0; i < shape.num_cells; i++) {
						if (pencilling_freedom_counts [i] > 0)
							return false;
					}

					return true;
				}
			}

			private int Score (int index) {
				if (board [index] != 0)
					return 0;

				return shape.group_size + 1 - pencilling_freedom_counts [index];
			}

			private int GetBestMoveByOptions (out int score) {
				int best = 0;
				int best_score = Score (0);

				for (int i = 1; i < shape.num_cells; i++) {
					int s = Score (i);

					if (s > best_score) {
						best = i;
						best_score = s;
					}
				}

				score = shape.group_size + 1 - best_score;

				return best;
			}

			public void RecursiveSolve (List<Partial> solutions_list, ref int difficulty) {
				if (is_automatically_unsolvable)
					return;

				solver.iterations++;

				if (solver.iterations > solver.args.iteration_limit)
					throw new TooManyIterationsException ();

				// Check if the solution limit has been reached
				if (solutions_list.Count == solver.args.max_solutions) {
					return;
				}

				// Check if solved
				if (is_solved) {
					solutions_list.Add (this);
					return;
				}

				// Check for the best move by looking at pencilling options
				int options_move = GetBestMoveByOptions (out int options_score);
				List<int> best_pencilling_moves = new List<int> (shape.group_size);

				#region Pencilling Options
				for (int i = 0; i < shape.group_size; i++) {
					if (pencilling_freedoms [options_move, i]) {
						best_pencilling_moves.Add (i);
					}
				}
				#endregion

				// Because this grid can never be solved, so further computation is unnecessary
				if (best_pencilling_moves.Count == 0) {
					return;
				}

				// See if set elimination will work faster
				List<int> best_set_moves = null;
				int best_set_digit = 0;

				#region Set Elimination
				if (solver.args.use_set_elimination && best_pencilling_moves.Count > 0) {
					for (int group = 0; group < shape.num_groups; group++) { // The group to add it to
						for (int digit = 0; digit < shape.group_size; digit++) { // The digit to add
							List<int> potential_positions = new List<int> (shape.group_size);

							/*
							if (set_penned_in [group, digit]) // The digit we were searching for already occurs
								break;
							*/

							bool is_penned_in = false;

							for (int k = 0; k < shape.group_size; k++) {
								int pos = shape.cells_by_group [group, k];

								if (board [pos] != 0) {
									is_penned_in = true;
									break;
								} else if (pencilling_freedoms [pos, digit]) {
									potential_positions.Add (pos);
								}
							}

							if (is_penned_in)
								continue;

							if (best_set_moves == null || potential_positions.Count < best_set_moves.Count) {
								best_set_moves = potential_positions;
								best_set_digit = digit;

								if (best_set_moves.Count == 0) { // The puzzle must be impossible
									return;
								}
							}
						}
					}
				}
				#endregion

				bool use_pencilling = best_set_moves == null || options_score <= best_set_moves.Count;

				// Recursively make the moves
				if (use_pencilling) {
					if (solver.args.fill_randomly)
						best_pencilling_moves.Shuffle (solver.args.rng);

					foreach (int d in best_pencilling_moves) {
						new Partial (this, options_move, d).RecursiveSolve (solutions_list, ref difficulty);
					}
				} else {
					if (solver.args.fill_randomly)
						best_set_moves.Shuffle (solver.args.rng);

					foreach (int m in best_set_moves) {
						new Partial (this, m, best_set_digit).RecursiveSolve (solutions_list, ref difficulty);
					}
				}

				// Update the difficulty by the branching factor
				int branching_factor = (use_pencilling ? best_pencilling_moves : best_set_moves).Count - 1;
				difficulty += branching_factor * branching_factor;
			}

			// Construct an initial Partial representing a starting puzzle
			public Partial (int [] board, RecursiveSolver solver) {
				this.board = board;
				this.solver = solver;
				shape = solver.shape;

				cell_fill_order = new int [shape.num_cells - board.Where (i => i > 0).Count ()];

				#region Pencilling freedoms
				pencilling_freedoms = new bool [shape.num_cells, shape.group_size];

				for (int i = 0; i < shape.num_cells; i++) {
					for (int j = 0; j < shape.group_size; j++) {
						pencilling_freedoms [i, j] = true;
					}
				}

				for (int i = 0; i < shape.num_cells; i++) {
					if (board [i] == 0)
						continue;

					for (int j = 0; j < shape.groups_by_cell [i].Length; j++) {
						for (int k = 0; k < shape.group_size; k++) {
							int p = shape.cells_by_group [shape.groups_by_cell [i] [j], k];

							pencilling_freedoms [p, board [i] - 1] = false;
						}
					}
				}
				#endregion

				#region Pencilling Counts
				pencilling_freedom_counts = new int [shape.num_cells];

				for (int i = 0; i < shape.num_cells; i++) {
					for (int j = 0; j < shape.group_size; j++) {
						if (pencilling_freedoms [i, j])
							pencilling_freedom_counts [i]++;
					}
				}
				#endregion

				#region Set freedoms
				set_penned_in = new bool [shape.num_groups, shape.group_size];

				for (int g = 0; g < shape.num_groups; g++) {
					for (int d = 0; d < shape.group_size; d++) {
						for (int p = 0; p < shape.group_size; p++) {
							if (board [shape.cells_by_group [g, p]] == d + 1) {
								set_penned_in [g, d] = true;

								break;
							}
						}
					}
				}
				#endregion
			}

			// Make a partial representing the board after a given digit is filled
			public Partial (Partial parent, int digit_position, int digit_added) {
				// Duplicate parent's data
				solver = parent.solver;
				shape = parent.shape;

				board = (int [])parent.board.Clone ();

				cell_fill_order = (int [])parent.cell_fill_order.Clone ();
				cell_fill_order [parent.depth] = digit_position;
				depth = parent.depth + 1;

				pencilling_freedoms = (bool [,])parent.pencilling_freedoms.Clone ();
				pencilling_freedom_counts = (int [])parent.pencilling_freedom_counts.Clone ();

				set_penned_in = (bool [,])parent.set_penned_in.Clone ();

				// Make the necessary changes
				board [digit_position] = digit_added + 1;

				for (int i = 0; i < shape.groups_by_cell [digit_position].Length; i++) {
					for (int j = 0; j < shape.group_size; j++) {
						int p = shape.groups_by_cell [digit_position] [i];
						int q = shape.cells_by_group [p, j];

						if (pencilling_freedoms [q, digit_added]) {
							pencilling_freedom_counts [q]--;

							if (pencilling_freedom_counts [q] == 0 && board [q] == 0) {
								is_automatically_unsolvable = true;
							}
						}

						pencilling_freedoms [q, digit_added] = false;
					}

					set_penned_in [i, digit_added] = true;
				}
			}
		}

		private SolverArgs args;
		private Shape shape;
		private int [] clues;

		private int iterations;

		public int [] [] Solve (Shape shape, int [] clues, SolverArgs args, out int difficulty, out int [] [] cell_orders) {
			this.clues = clues;
			this.shape = shape;
			this.args = args;

			iterations = 0;

			List<Partial> solutions = new List<Partial> ();

			difficulty = 0;

			// Might throw a TooManyIterationsException
			new Partial (clues, this).RecursiveSolve (solutions, ref difficulty);

			// Copy the values out of lists and into arrays and return
			int [] [] output = new int [solutions.Count] [];
			cell_orders = new int [solutions.Count] [];

			for (int i = 0; i < solutions.Count; i++) {
				output [i] = new int [shape.num_cells];

				for (int j = 0; j < shape.num_cells; j++) {
					output [i] [j] = solutions [i].board [j];
				}

				cell_orders [i] = new int [solutions [i].cell_fill_order.Length];
				for (int j = 0; j < solutions [i].cell_fill_order.Length; j++) {
					cell_orders [i] [j] = solutions [i].cell_fill_order [j];
				}
			}

			return output;
		}

		public static int [] Get3x3Grid (string path) {
			int [] grid = new int [81];
			int index = 0;

			foreach (string line in File.ReadAllLines (path)) {
				foreach (char c in line) {
					if (".123456789".Contains (c)) {
						grid [index] = ".123456789".IndexOf (c);
						index++;
					}
				}
			}

			return grid;
		}
	}
}
