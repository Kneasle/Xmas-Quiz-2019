using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SudokuCore {
	public class Symmetry {
		public enum Class {
			None,
			Rotational180deg,
			Rotational90deg,
			HorizontalReflection,
			VerticalReflection,
			DoubleReflection,
			Mega
		}

		public int [] [] cells_by_class;
		public int [] class_by_cells;

		public Symmetry (int num_cells, int [] class_by_cells) {
			this.class_by_cells = class_by_cells;

			int num_classes = class_by_cells.Distinct ().Count ();

			List<int> [] temp = new List<int> [num_classes];

			for (int i = 0; i < num_classes; i++) {
				temp [i] = new List<int> ();
			}

			for (int i = 0; i < num_cells; i++) {
				temp [class_by_cells [i]].Add (i);
			}

			cells_by_class = temp.Select (x => x.ToArray ()).ToArray ();
		}

		public static Symmetry GetSymmetryFromParsedFile (Dictionary<string, List<string>> parsed_file, int group_size, int num_cells) {
			if (parsed_file.ContainsKey (Parser.Headers.symmetry)) {
				string symmetry_string = parsed_file [Parser.Headers.symmetry] [0].Trim ();

				switch (symmetry_string) {
					case Parser.Symmetry.none:
						return GenerateSquareSymmetry (Class.None, group_size);
					case Parser.Symmetry.rotational_180:
						return GenerateSquareSymmetry (Class.Rotational180deg, group_size);
					case Parser.Symmetry.rotational_90:
						return GenerateSquareSymmetry (Class.Rotational90deg, group_size);
					case Parser.Symmetry.horizontal_reflection:
						return GenerateSquareSymmetry (Class.HorizontalReflection, group_size);
					case Parser.Symmetry.vertical_reflection:
						return GenerateSquareSymmetry (Class.VerticalReflection, group_size);
					case Parser.Symmetry.double_reflection:
						return GenerateSquareSymmetry (Class.DoubleReflection, group_size);
					case Parser.Symmetry.mega:
						return GenerateSquareSymmetry (Class.Mega, group_size);
					default:
						// Generate a custom symmetry group
						int [] [] groups = Parser.LexifyString (symmetry_string, out int length);

						int [] class_by_cells = new int [num_cells];

						for (int i = 0; i < groups.Length; i++) {
							foreach (int index in groups [i]) {
								class_by_cells [index] = i;
							}
						}
						
						return new Symmetry (num_cells, class_by_cells);
				}
			} else {
				return GenerateSquareSymmetry (Class.Rotational180deg, group_size);
			}
		}

		public static Symmetry GenerateSquareSymmetry (Class symmetry_class, int group_size) {
			int num_cells = group_size * group_size;

			// This is literally just division by two but rounding up (it's ugly - I know - but it works,
			// and is only ever run once per function call).
			int classes_per_row = (group_size % 2 == 0) ? group_size / 2 : group_size / 2 + 1;

			int i = 0;

			int Index (int x, int y) => y * group_size + x;

			switch (symmetry_class) {
				case Class.None:
					return NoSymmetry (num_cells);
				case Class.Rotational180deg:
					int [] class_by_point = new int [num_cells];

					for (int j = 0; j < num_cells / 2; j++) {
						class_by_point [j] = j;
						class_by_point [num_cells - 1 - j] = j;
					}

					if (num_cells % 2 == 1) {
						class_by_point [(num_cells - 1) / 2] = (num_cells - 1) / 2;
					}

					return new Symmetry (num_cells, class_by_point);
				case Class.Rotational90deg:
					int [] class_by_cells_d_rot = new int [num_cells];

					i = 0;

					for (int y = 0; y < classes_per_row; y++) {
						for (int x = 0; x < classes_per_row; x++) {
							// Gotta love multiple simultaneous assignments
							class_by_cells_d_rot [Index (x, y)]
								= class_by_cells_d_rot [Index (group_size - 1 - y, x)]
								= class_by_cells_d_rot [Index (y, group_size - 1 - x)]
								= class_by_cells_d_rot [Index (group_size - 1 - x, group_size - 1 - y)]
								= i;

							i++;
						}
					}

					return new Symmetry (num_cells, class_by_cells_d_rot);
				case Class.HorizontalReflection:
					int [] class_by_cells_h_ref = new int [num_cells];

					i = 0;

					for (int y = 0; y < group_size; y++) {
						for (int x = 0; x < classes_per_row; x++) {
							class_by_cells_h_ref [Index (x, y)] 
								= class_by_cells_h_ref [Index (group_size - 1 - x, y)] 
								= i;

							i++;
						}
					}

					return new Symmetry (num_cells, class_by_cells_h_ref);
				case Class.VerticalReflection:
					int [] class_by_cells_v_ref = new int [num_cells];

					i = 0;

					for (int y = 0; y < classes_per_row; y++) {
						for (int x = 0; x < group_size; x++) {
							class_by_cells_v_ref [Index (x, y)]
								= class_by_cells_v_ref [Index (x, group_size - 1 - y)]
								= i;

							i++;
						}
					}

					return new Symmetry (num_cells, class_by_cells_v_ref);
				case Class.DoubleReflection:
					int [] class_by_cells_d_ref = new int [num_cells];

					i = 0;

					for (int y = 0; y < classes_per_row; y++) {
						for (int x = 0; x < y; x++) {
							// Gotta love multiple simultaneous assignments
							class_by_cells_d_ref [Index (x, y)]
								= class_by_cells_d_ref [Index (group_size - 1 - x, y)]
								= class_by_cells_d_ref [Index (group_size - 1 - x, group_size - 1 - y)]
								= class_by_cells_d_ref [Index (x, group_size - 1 - y)]
								= i;

							i++;
						}
					}

					return new Symmetry (num_cells, class_by_cells_d_ref);
				case Class.Mega:
					int [] class_by_cells_mega = new int [num_cells];

					i = 0;

					for (int y = 0; y < classes_per_row; y++) {
						for (int x = 0; x < classes_per_row; x++) {
							// Gotta love multiple simultaneous assignments
							class_by_cells_mega [Index (x, y)]
								= class_by_cells_mega [Index (y, x)]
								= class_by_cells_mega [Index (group_size - 1 - x, y)]
								= class_by_cells_mega [Index (y, group_size - 1 - x)]
								= class_by_cells_mega [Index (group_size - 1 - x, group_size - 1 - y)]
								= class_by_cells_mega [Index (group_size - 1 - y, group_size - 1 - x)]
								= class_by_cells_mega [Index (group_size - 1 - y, x)]
								= i;

							i++;
						}
					}

					return new Symmetry (num_cells, class_by_cells_mega);
				default:
					throw new Exception ("Brexit!");
			}
		}

		public static Symmetry NoSymmetry (int num_cells) => new Symmetry (num_cells, Enumerable.Range (0, num_cells).ToArray ());
	}
}
