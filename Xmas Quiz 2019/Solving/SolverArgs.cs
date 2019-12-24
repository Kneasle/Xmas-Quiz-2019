using System;
using System.Collections.Generic;
using System.Text;
using SudokuCore.Populating;

namespace SudokuCore.Solving {
	public class SolverArgs {
		public bool verbose = true;
		public int iteration_limit = 100_000;
		public int max_solutions = -1;
		public bool fill_randomly = false;
		public bool use_set_elimination = false;
		public Random rng;

		public SolverArgs () { }

		public SolverArgs (PopulatorArgs args) {
			verbose = args.verbose;
			iteration_limit = args.iteration_limit;
			rng = new Random (args.random_seed);

			max_solutions = 1;
			fill_randomly = true;
		}
	}
}
