using System;
using System.Collections.Generic;
using System.Text;

namespace SudokuCore.Populating {
	public class PopulatorArgs {
		public bool verbose = false;
		public int iteration_limit = 100_000;
		public int random_seed = 0;
		public bool print_fixed_cells = true;
		public char [] alphabet = null;
	}
}
