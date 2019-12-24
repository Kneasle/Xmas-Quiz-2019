using System;
using System.Collections.Generic;
using System.Text;

namespace SudokuCore.Solving {
	public interface ISolver {
		int [] [] Solve (Shape shape, int [] clues, SolverArgs args, out int difficulty, out int [] [] cell_fill_orders);
	}
}
