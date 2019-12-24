using System;
using System.Collections.Generic;
using System.Text;
using SudokuCore.Solving;
using System.Linq;

namespace SudokuCore.PuzzleMaking {
	public static class PuzzleMaker {
		private class GetMeOutOfHereException : Exception {
			public int [] final_board;

			public GetMeOutOfHereException (int [] final_board) {
				this.final_board = final_board;
			}
		}

		public static int [] GeneratePuzzle (Shape shape, int [] solution, int seed = 0, int min_clues = 0) {
			int [] initial_board = (int [])solution.Clone ();

			Random rng = new Random (seed);

			int iterations = 0;

			int best_score = 0;
			int [] best_board = null;

			try {
				RecursiveRemove (initial_board, 0, 0);
			} catch (GetMeOutOfHereException) {
				return best_board;
			}

			throw new Exception ("This code should never be executed.");

			void RecursiveRemove (int [] board, int groups_gone, int squares_gone) {
				int [] class_order = Utils.RandomPermutation (rng, shape.symmetry.cells_by_class.Length, 0);

				// Request that higlighted groups are removed first
				if (false && shape.highlight != null && shape.highlight.Any ()) {
					int i = class_order.Length - 1;
					int j = 0;

					while (i >= j) {
						if (shape.symmetry.cells_by_class [class_order [i]]
								.Select (x => shape.highlight [x])
								.Any ()
						) {
							int temp = class_order [i];
							class_order [i] = class_order [j];
							class_order [j] = temp;

							j += 1;
						} else {
							i -= 1;
						}
					}
				}

				// Console.WriteLine (string.Join (",", class_order.Select (x => x.ToString ())));

				for (int i = 0; i < class_order.Length; i++) {
					// Check if this has already been removed
					if (board [shape.symmetry.cells_by_class [class_order [i]] [0]] == 0)
						continue;

					// Buck out after too many iterations (so this function doesn't hang forever)
					iterations++;

					if (iterations > 100)
						throw new GetMeOutOfHereException (board);

					// Construct new board
					int [] points = shape.symmetry.cells_by_class [class_order [i]];

					// Check if too few clues are left
					if (shape.num_cells - points.Length - squares_gone < min_clues)
						continue;

					int [] new_board = (int [])board.Clone ();

					for (int j = 0; j < points.Length; j++) {
						new_board [points [j]] = 0;
					}

					// Check that every letter in the alphabet appears once
					bool [] has_been_used = new bool [shape.group_size];

					for (int b = 0; b < shape.num_cells; b++) {
						if (new_board [b] > 0)
							has_been_used [new_board [b] - 1] = true;
					}

					bool any_missing = false;
					for (int v = 0; v < shape.group_size; v++) {
						if (!has_been_used [v]) {
							any_missing = true;
							break;
						}
					}

					if (any_missing)
						continue;

					// Check for solutions
					try {
						int [] [] solutions = new RecursiveSolver ().Solve (
							shape, 
							new_board, 
							new SolverArgs () { max_solutions = 2, iteration_limit = 50000 }, 
							out int diff, 
							out int [] [] cell_fill_orders
						);

						if (solutions.Length == 1) {
							int score = squares_gone + diff;

							if (score > best_score) {
								best_score = score;
								best_board = new_board;
							}

							RecursiveRemove (new_board, groups_gone + 1, squares_gone + points.Length);
						}
					} catch (RecursiveSolver.TooManyIterationsException) {

					}
				}
			}
		}
	}
}
