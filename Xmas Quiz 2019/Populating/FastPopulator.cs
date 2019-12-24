using System;
using System.Collections.Generic;
using System.Text;
using SudokuCore.Solving;

namespace SudokuCore.Populating {
	public class FastPopulator : IPopulator {
		public int [] PopulateGrid (Shape shape, PopulatorArgs args, int [] grid = null) {
			// Fill in the first group
			int [] full_grid = new int [shape.num_cells];

			Random rng = new Random (args.random_seed);

			if (grid == null) {
				int [] perm = Utils.RandomPermutation (rng, shape.group_size);

				for (int i = 0; i < shape.group_size; i++) {
					full_grid [shape.cells_by_group [0, i]] = perm [i];
				}
			} else {
				Array.Copy (grid, full_grid, grid.Length);

				for (int i = 0; i < shape.num_groups; i++) {
					bool [] has_been_found = new bool [shape.group_size];

					for (int j = 0; j < shape.group_size; j++) {
						int v = full_grid [shape.cells_by_group [i, j]];

						if (v > 0) {
							if (has_been_found [v - 1])
								throw new DuplicateElementInGroupException ("Duplicate element in a group.", v - 1);

							has_been_found [v - 1] = true;
						}
					}
				}
			}

			try {
				int [] [] solutions = new RecursiveSolver ().Solve (
					shape, 
					full_grid,
					new SolverArgs (args) { rng = rng, max_solutions = args.print_fixed_cells ? 10 : 1, iteration_limit = 10_000_000 },
					out int difficulty,
					out int [] [] cell_fill_orders
				);
				
				if (args.print_fixed_cells) {
					int [] fixed_cells = (int [])solutions [0].Clone ();

					Console.WriteLine (solutions.Length);

					for (int i = 1; i < solutions.Length; i++) {
						for (int j = 0; j < shape.num_cells; j++) {
							if (fixed_cells [j] != 0 && solutions [i] [j] != fixed_cells [j])
								fixed_cells [j] = 0;
						}
					}

					shape.Print (fixed_cells, args.alphabet);
				}

				if (solutions.Length == 0)
					throw new GridNotSolubleException ("Grid could not be completed.");

				return solutions [0];
			} catch (RecursiveSolver.TooManyIterationsException) {
				throw new GridNotSolubleException ("Solver ran for too many iterations.");
			}
		}
	}

	public class GridNotSolubleException : Exception {
		public GridNotSolubleException (string message) : base (message) { }
	}

	public class DuplicateElementInGroupException : GridNotSolubleException {
		public int element;

		public DuplicateElementInGroupException (string message) : base (message) { }

		public DuplicateElementInGroupException (string message, int element) : base (message) {
			this.element = element;
		}
	}
}
