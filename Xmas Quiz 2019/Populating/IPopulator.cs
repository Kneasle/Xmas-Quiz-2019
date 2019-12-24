using System;
using System.Collections.Generic;
using System.Text;

namespace SudokuCore.Populating {
	public interface IPopulator {
		int [] PopulateGrid (Shape shape, PopulatorArgs args, int [] grid = null);
	}
}
