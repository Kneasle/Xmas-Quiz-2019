using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

namespace SudokuCore {
	public class Puzzle {
		public Shape shape;
		public char [] alphabet;

		public int [] puzzle_grid;
		public int [] solution_grid;

		public int [] output_cells;

		public void SaveToFile (string path, Dictionary<string, List<string>> extra_sections = null) {
			List<char> alphabet_sorted = new List<char> (alphabet);

			alphabet_sorted.Sort ();

			Vector2 centre = (Vector2.Min (shape.points) + Vector2.Max (shape.points)) / 2;

			string s = string.Join ("", alphabet_sorted) + "\n" 
				+ string.Join (
					"\n",
					output_cells.Select (
						i => string.Join (
							" ",
							Enumerable.Range (0, 4).Select (
								j => (
										Shape.svg_scale *
										(shape.points [shape.cell_points [i, j]].x - centre.x)
									) + " " + (
										Shape.svg_scale *
										(shape.points [shape.cell_points [i, j]].y - centre.y)
									)
							)
						)
					)
				);

			File.WriteAllText (path, s);
		}

		public void SaveSVGToFile (string path, bool is_solution) {
			File.WriteAllText (path, shape.GenerateSVG (is_solution ? solution_grid : puzzle_grid, alphabet, output_cells));
		}

		public Puzzle (Shape shape, char [] alphabet, int [] puzzle_grid, int [] solution_grid, int [] output_cells) {
			this.shape = shape;
			this.alphabet = alphabet;
			this.puzzle_grid = puzzle_grid;
			this.solution_grid = solution_grid;
			this.output_cells = output_cells;
		}

		public Puzzle (Specification specification, int [] puzzle_grid, int [] solution_grid, int [] output_cells) {
			shape = specification.shape;
			alphabet = specification.alphabet;

			this.puzzle_grid = puzzle_grid;
			this.solution_grid = solution_grid;
			this.output_cells = output_cells;
		}
	}
}
