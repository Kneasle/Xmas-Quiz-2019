using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace SudokuCore.Populating {
	public class NaiveTreePopulator : IPopulator {
		private Shape shape;
		private Random rng;
		private int [] solved_grid;
		private PopulatorArgs args;

		// Debug data
		private Stopwatch stopwatch;
		private double last_sampled_time = 0f;
		private const double print_time = 0.333333f;
		private int [] recursion_counts;
		private int iteration_counts = 0;

		public int [] PopulateGrid (Shape shape, PopulatorArgs args, int [] grid = null) {
			this.shape = shape;
			rng = new Random (args.random_seed);
			this.args = args;

			while (true) {
				bool success = true;

				try {
					solved_grid = new int [shape.num_cells];

					// Fill in the first row before we start
					if (grid == null) {
						int [] perm = Utils.RandomPermutation (rng, shape.group_size);

						for (int i = 0; i < shape.group_size; i++) {
							solved_grid [shape.cells_by_group [0, i]] = perm [i];
						}
					} else {
						Array.Copy (grid, solved_grid, grid.Length);
					}

					recursion_counts = new int [shape.num_cells];
					iteration_counts = 0;
					stopwatch = Stopwatch.StartNew ();

					RecursivelyGenerate (0);
				} catch (UpSticksAndRunException) {
					if (args.verbose)
						Console.WriteLine ("Went on for too long.");

					success = false;
				}

				if (success) {
					break;
				}
			}

			if (args.verbose) {
				for (int i = 0; i < shape.num_cells; i++) {
					Console.Write (recursion_counts [i] + ((i + 1) % shape.group_size == 0 ? "\n" : "\t"));
				}
			}

			return solved_grid;
		}

		private bool RecursivelyGenerate (int index) {
			if (index >= shape.num_cells) {
				return true;
			}

			recursion_counts [index]++;

			iteration_counts++;

			if (iteration_counts == args.iteration_limit)
				throw new UpSticksAndRunException ();

			if (solved_grid [index] != 0) {
				return RecursivelyGenerate (index + 1);
			}

			if (stopwatch.Elapsed.TotalSeconds - last_sampled_time > print_time) {
				last_sampled_time = stopwatch.Elapsed.TotalSeconds;
				if (args.verbose)
					Console.Write (index + "\t");
			}

			foreach (int i in Utils.RandomPermutation (rng, shape.group_size)) {
				bool jump = false;

				int [] group_indices = shape.groups_by_cell [index];

				for (int p = 0; p < group_indices.Length; p++) {
					for (int q = 0; q < shape.group_size; q++) {
						int v = solved_grid [shape.cells_by_group [group_indices [p], q]];

						if (v == i) {
							jump = true;

							break;
						}
					}

					if (jump)
						break;
				}

				if (jump)
					continue;

				solved_grid [index] = i;

				if (RecursivelyGenerate (index + 1))
					return true;
			}

			solved_grid [index] = 0;

			return false;
		}

		private class UpSticksAndRunException : Exception { }
	}
}
