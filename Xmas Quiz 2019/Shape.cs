using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SudokuCore {
	public class Shape {
		public int num_cells { get; private set; }
		public int group_size { get; private set; }
		public int num_groups { get; private set; }

		public int [,] cells_by_group { get; private set; }
		public int [] [] groups_by_cell { get; private set; }

		public Vector2 [] points { get; private set; }
		public int [,] cell_points { get; private set; }

		public bool [] highlight;

		public Symmetry symmetry { get; set; }

		public string console_format { get; private set; }

		public Dictionary<string, List<string>> CompileToSections () {
			List<string> group_strings = new List<string> ();

			#region Generate group strings
			char [] current_group_string = null;
			bool [] cells_used = null;

			int current_char_index = 0;

			void ResetCurrentGroupString () {
				current_group_string = new char [num_cells];
				for (int i = 0; i < num_cells; i++) {
					current_group_string [i] = '.';
				}

				cells_used = new bool [num_cells];

				current_char_index = 0;
			}

			void AddCurrentGroupString () => group_strings.Add (new string (current_group_string));

			ResetCurrentGroupString ();

			for (int i = 0; i < num_groups; i++) {
				bool all_cells_free = Enumerable.Range (0, group_size)
					.Select (j => cells_by_group [i, j])
					.All (x => !cells_used [x]);

				if (!all_cells_free) {
					// We need to entirely start a new group string
					AddCurrentGroupString ();
					ResetCurrentGroupString ();
				}

				for (int j = 0; j < group_size; j++) {
					cells_used [cells_by_group [i, j]] = true;
					current_group_string [cells_by_group [i, j]] = Parser.alpha [current_char_index];
				}

				current_char_index++;
			}
			
			AddCurrentGroupString ();
			#endregion

			var d = new Dictionary<string, List<string>> {
				{ Parser.Headers.groups, group_strings },
				{ Parser.Headers.format, new List<string> { console_format } }
			};

			if (points != null)
				d.Add (Parser.Headers.points, new List<string> { string.Join ("\n", points.Select (v => v.x + " " + v.y)) });

			if (cell_points != null)
				d.Add (Parser.Headers.cell_points, new List<string> { string.Join ("\n", cell_points.GetAllRows ().Select (r => string.Join (" ", r))) });

			return d;
		}

		public string GetString (int [] grid, char [] alphabet = null) {
			StringBuilder builder = new StringBuilder (console_format);

			string s = "." + (alphabet == null ? "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ" : new string (alphabet));

			int p = 0;
			for (int i = 0; i < console_format.Length; i++) {
				if (console_format [i] == '#') {
					builder [i] = s [grid [p]];
					p++;
				}
			}

			return builder.ToString ();
		}

		public string GenerateSVG (int [] grid, char [] alphabet = null, int [] output_cells = null) {
			// Create useful variables
			if (points == null) {
				Console.WriteLine ("No points!");

				return "";
			}

			Vector2 min = Vector2.Min (points);
			Vector2 max = Vector2.Max (points);

			Vector2 preffered_size = (max - min) * svg_scale + Vector2.one * svg_margin * 2f;

			bool force_power_of_two = false;

			Vector2 size = force_power_of_two ? new Vector2 (
				1 << ((int)Math.Ceiling (Math.Log (preffered_size.x, 2f))),
				1 << ((int)Math.Ceiling (Math.Log (preffered_size.y, 2f)))
			) : preffered_size;

			// Create root of the document
			string header = "<?xml version = \"1.0\" encoding = \"utf-8\" standalone = \"no\"?>\n";

			XNamespace xNamespace = "http://www.w3.org/2000/svg";
			XNamespace sodipodi = "http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd";
			XNamespace inkscape = "http://www.inkscape.org/namespaces/inkscape";
			
			XElement root = new XElement (
				xNamespace + "svg",
				new XAttribute ("width", size.x.ToString () + unit),
				new XAttribute ("height", size.y.ToString () + unit),
				new XAttribute ("version", "1.1"),
				new XAttribute ("baseProfile", "full"),
				new XAttribute ("viewBox", "0 0 " + size.x.ToString () + " " + size.y.ToString ()),
				new XAttribute ("xmlns", "http://www.w3.org/2000/svg"),
				new XAttribute (XNamespace.Xmlns + "ev", "http://www.w3.org/2001/xml-events"),
				new XAttribute (XNamespace.Xmlns + "xlink", "http://www.w3.org/1999/xlink"),
				new XAttribute (XNamespace.Xmlns + "sodipodi", "http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd"),
				new XAttribute (XNamespace.Xmlns + "inkscape", "http://www.inkscape.org/namespaces/inkscape"),
				new XElement (
					sodipodi + "namedview",
					new XAttribute (inkscape + "document-units", "px")
				)
			);

			// Generate edge layout
			List<(int, int)> edges = new List<(int, int)> ();
			List<List<int>> sides = new List<List<int>> ();

			for (int i = 0; i < num_cells; i++) {
				for (int j = 0; j < 4; j++) {
					int a = cell_points [i, j];
					int b = cell_points [i, (j + 1) % 4];

					int p1 = Math.Min (a, b);
					int p2 = Math.Max (a, b);

					bool has_edge_already_been_found = false;

					for (int k = 0; k < edges.Count; k++) {
						int c1, c2;
						(c1, c2) = edges [k];

						if (c1 == p1 && c2 == p2) {
							sides [k].Add (i);

							has_edge_already_been_found = true;

							break;
						}
					}

					if (!has_edge_already_been_found) {
						edges.Add ((p1, p2));
						sides.Add (new List<int> () { i });
					}
				}
			}

			// Check for group continuity
			int group_colour_index = 0;

			string [] group_colours = new string [] {
				"#ff9999",
				"#99ff99",
				"#9999ff",
				"#ff99ff",
				"#99ffff"
			};

			string [] cell_colours = new string [num_cells];

			for (int i = 0; i < num_groups; i++) {
				List<int> cells_found = new List<int> (group_size);
				Queue<int> cells_to_search = new Queue<int> ();
				bool [] has_cell_been_found = new bool [num_cells]; // Initialised to false
				bool [] is_cell_in_group = new bool [num_cells];

				for (int j = 0; j < num_cells; j++) {
					if (groups_by_cell [j].Contains (i)) {
						is_cell_in_group [j] = true;
					}
				}

				cells_to_search.Enqueue (cells_by_group [i, 0]);
				has_cell_been_found [cells_by_group [i, 0]] = true;

				while (cells_to_search.Count > 0) {
					int cell = cells_to_search.Dequeue ();

					cells_found.Add (cell);

					foreach (List<int> xs in sides) {
						if (xs.Count == 1) {
							continue;
						}

						int a = xs [0];
						int b = xs [1];

						if (!(is_cell_in_group [a] && is_cell_in_group [b])) {
							continue;
						}

						if (a == cell || b == cell) {
							int other = b == cell ? a : b;

							if (!has_cell_been_found [other]) {
								has_cell_been_found [other] = true;

								cells_to_search.Enqueue (other);
							}
						}
					}
				}

				if (cells_found.Count () < group_size) {
					for (int j = 0; j < group_size; j++) {
						cell_colours [cells_by_group [i, j]] = group_colours [group_colour_index % group_colours.Length];
					}

					group_colour_index += 1;
				}
			}

			#region Add shapes to SVG
			for (int i = 0; i < num_cells; i++) {
				// Create background polygon
				string [] point_strings = new string [5];

				for (int j = 0; j < 4; j++) {
					Vector2 p = (points [cell_points [i, j]] - min) * svg_scale + Vector2.one * svg_margin;

					point_strings [j] = p.x + "," + p.y;
				}

				point_strings [4] = point_strings [0];

				string colour = cell_colours [i] ?? "white";

				if (groups_by_cell [i].Length > 3) {
					colour = "#cccccc";
				}

				if (highlight != null && highlight [i]) {
					colour = "#999999";
				}

				root.Add (
					new XElement (
						"polygon",
						new XAttribute ("fill", colour),
						new XAttribute ("points", string.Join (" ", point_strings))
					)
				);

				// Create text
				Vector2 sum = Vector2.zero;

				for (int j = 0; j < 4; j++) {
					sum += (points [cell_points [i, j]] - min) * svg_scale + Vector2.one * svg_margin;
				}

				Vector2 centre = sum / 4f;

				if (grid [i] > 0) {
					root.Add (
						new XElement (
							"text",
							new XAttribute ("font-family", "Consolas"),
							new XAttribute ("font-size", (svg_text_size * svg_scale) + unit),
							new XAttribute ("x", centre.x + unit),
							new XAttribute ("y", (centre.y - svg_baseline_fix * svg_scale * svg_text_size) + unit),
							new XAttribute ("dominant-baseline", "middle"),
							new XAttribute ("text-anchor", "middle"),
							alphabet [grid [i] - 1]
						)
					);
				}

				// Add dot for output cells
				if (add_dots_for_outputs && output_cells.Contains (i)) {
					root.Add (
						new XElement (
							"circle",
							new XAttribute ("cx", centre.x),
							new XAttribute ("cy", centre.y),
							new	XAttribute ("r", svg_scale * 0.1f),
							new XAttribute ("fill", "black")
						)
					);
				}
			}

			for (int i = 0; i < edges.Count; i++) {
				Vector2 p1 = (points [edges [i].Item1] - min) * svg_scale + Vector2.one * svg_margin;
				Vector2 p2 = (points [edges [i].Item2] - min) * svg_scale + Vector2.one * svg_margin;

				bool is_solid_line = false;

				if (sides [i].Count == 1) {
					is_solid_line = true;
				} else {
					int i1 = sides [i] [0];
					int i2 = sides [i] [1];

					int shared_groups = 0;

					if (groups_by_cell [i1].Length == groups_by_cell [i2].Length) {
						for (int j = 0; j < Math.Min (groups_by_cell [i1].Length, 3); j++) {
							for (int k = 0; k < Math.Min (groups_by_cell [i2].Length, 3); k++) {
								if (groups_by_cell [i1] [j] == groups_by_cell [i2] [k]) {
									shared_groups += 1;
								}
							}
						}
					}

					if (shared_groups == 1) {
						is_solid_line = true;
					}
				}
				
				root.Add (
					new XElement (
						"line",
						new XAttribute ("x1", p1.x + unit),
						new XAttribute ("y1", p1.y + unit),
						new XAttribute ("x2", p2.x + unit),
						new XAttribute ("y2", p2.y + unit),
						new XAttribute ("stroke", "black"),
						new XAttribute ("stroke-linecap", "round"),
						new XAttribute (
							"stroke-width",
							(is_solid_line ? svg_box_line_width : svg_standard_line_width) + unit
						),
						new XAttribute ("fill", "none")
					)
				);
			}
			#endregion

			return header + root.ToString ().Replace (" xmlns=\"\"", "");
		}

		public void Print (int [] grid, char [] alphabet = null) => Console.WriteLine (GetString (grid, alphabet));

		public Shape (int num_cells, int group_size, int [,] cells_by_group, Vector2 [] points, int [,] cell_points, string console_format, Symmetry symmetry) {
			this.num_cells = num_cells;
			this.group_size = group_size;
			this.cells_by_group = cells_by_group;
			this.points = points;
			this.cell_points = cell_points;
			this.console_format = console_format;

			this.symmetry = symmetry ?? Symmetry.NoSymmetry (num_cells);

			// Generate groups_by_point lookups
			List<int> [] groups_by_cell_temp = new List<int> [num_cells];
			for (int i = 0; i < num_cells; i++) {
				groups_by_cell_temp [i] = new List<int> (10);
			}

			for (int i = 0; i < cells_by_group.GetLength (0); i++) {
				for (int j = 0; j < cells_by_group.GetLength (1); j++) {
					groups_by_cell_temp [cells_by_group [i, j]].Add (i);
				}
			}

			// Convert the lists to arrays
			groups_by_cell = new int [num_cells] [];

			for (int i = 0; i < num_cells; i++) {
				groups_by_cell [i] = new int [groups_by_cell_temp [i].Count];

				for (int j = 0; j < groups_by_cell_temp [i].Count; j++) {
					groups_by_cell [i] [j] = groups_by_cell_temp [i] [j];
				}
			}

			num_groups = cells_by_group.GetLength (0);
		}

		public static Shape LoadFromFile (string path) {
			// Parse file
			Dictionary<string, List<string>> parsed_file = Parser.ParseFile (path);

			if (parsed_file.ContainsKey (Parser.Headers.box_corners)) {
				Vector2 Parse (string s) {
					string [] parts = s.Split (' ');

					return new Vector2 (
						float.Parse (parts [0]),
						float.Parse (parts [1])
					);
				}

				Vector2 [] box_points = string.Join ("\n", parsed_file [Parser.Headers.box_points])
					.Split ('\n')
					.Select (Parse)
					.ToArray ();
				string [] box_corners_lines = string.Join ("\n", parsed_file [Parser.Headers.box_corners])
					.Split ('\n');

				int num_boxes = box_corners_lines.Length;
				int box_width = int.Parse (parsed_file [Parser.Headers.box_size] [0].Split ('\n') [0]);

				int group_size = box_width * box_width;

				int [,] box_corners = new int [num_boxes, 4];

				#region Parse Input
				for (int i = 0; i < num_boxes; i++) {
					string [] parts = box_corners_lines [i].Split (' ');

					for (int j = 0; j < 4; j++) {
						box_corners [i, j] = int.Parse (parts [j]);
					}
				}
				#endregion

				(int, int) [] edges = null;
				List<int> [] edge_boxes = null;
				Dictionary<(int, int), int> edge_indices = new Dictionary<(int, int), int> ();
				int [,] box_edges = new int [num_boxes, 4];

				#region Generate edge representations
				{
					HashSet<(int, int)> edge_set = new HashSet<(int, int)> ();

					for (int i = 0; i < num_boxes; i++) {
						for (int j = 0; j < 4; j++) {
							int a = box_corners [i, j];
							int b = box_corners [i, (j + 1) % 4];

							edge_set.Add (a < b ? (a, b) : (b, a));
						}
					}

					// Generate a representation of the edges
					edges = edge_set.ToArray ();
					edge_boxes = new List<int> [edges.Length];

					for (int e = 0; e < edges.Length; e++) {
						int v1, v2;
						(v1, v2) = edges [e];

						edge_indices [edges [e]] = e;

						List<int> current_edge_boxes = new List<int> ();

						for (int i = 0; i < num_boxes; i++) {
							for (int j = 0; j < 4; j++) {
								int a = box_corners [i, j];
								int b = box_corners [i, (j + 1) % 4];

								if (b < a) {
									int t = b;
									b = a;
									a = t;
								}

								if (a == v1 && b == v2) {
									current_edge_boxes.Add (i);
								}
							}
						}

						edge_boxes [e] = current_edge_boxes;
					}
				}

				// Determine which edges surround each face
				for (int b = 0; b < num_boxes; b++) {
					for (int e = 0; e < 4; e++) {
						int v1 = box_corners [b, e];
						int v2 = box_corners [b, (e + 1) % 4];

						box_edges [b, e] = edge_indices [v1 < v2 ? (v1, v2) : (v2, v1)];
					}
				}

				#endregion

				List<(int, int) []> quad_paths = new List<(int, int) []> (); // (box, rotation)

				#region Generate quad paths
				{
					List<int> single_edges_left = new List<int> ();

					for (int e = 0; e < edges.Length; e++) {
						if (edge_boxes [e].Count == 1) {
							single_edges_left.Add (e);
						}
					}

					while (single_edges_left.Count > 0) {
						int starting_edge = single_edges_left [0];
						single_edges_left.RemoveAt (0);

						int edge = starting_edge;
						int box = edge_boxes [edge] [0];

						(int, int) [] quad_path = new (int, int) [box_width];
						int index = 0;

						while (true) {
							int edge_in_index = -100000;
							for (int i = 0; i < 4; i++) {
								if (box_edges [box, i] == edge) {
									edge_in_index = i;
									break;
								}
							}

							quad_path [index] = (box, edge_in_index);
							index += 1;

							int edge_out = box_edges [box, (edge_in_index + 2) % 4];

							if (edge_boxes [edge_out].Count == 1) {
								single_edges_left.Remove (edge_out);
								break;
							} else {
								int b1 = edge_boxes [edge_out] [0];
								int b2 = edge_boxes [edge_out] [1];

								if (b1 == box) {
									box = b2;
								} else if (b2 == box) {
									box = b1;
								}
							}

							edge = edge_out;
						}

						quad_paths.Add (quad_path);
					}
				}
				#endregion

				int num_cells = num_boxes * box_width * box_width;
				int num_groups = num_boxes + box_width * quad_paths.Count;
				int [,] cells_by_group = new int [num_groups, group_size];

				#region Generate cell group data
				{
					int Index (int box, int x, int y) => box * group_size + x * box_width + y;

					int w = box_width - 1;

					// Boxes
					for (int b = 0; b < num_boxes; b++) {
						for (int c = 0; c < group_size; c++) {
							cells_by_group [b, c] = b * group_size + c;
						}
					}

					// Quad paths
					for (int q = 0; q < quad_paths.Count; q++) {
						for (int b = 0; b < quad_paths [q].Length; b++) {
							int box, rotation;
							(box, rotation) = quad_paths [q] [b];

							int base_index = num_boxes + q * box_width;

							for (int r = 0; r < box_width; r++) {
								for (int c = 0; c < box_width; c++) {
									if (rotation == 1) {
										cells_by_group [base_index + c, b * box_width + r] = Index (box, r, c);
									} else if (rotation == 2) {
										cells_by_group [base_index + c, b * box_width + r] = Index (box, w - c, r);
									} else if (rotation == 3) {
										cells_by_group [base_index + c, b * box_width + r] = Index (box, r, w - c);
									} else if (rotation == 0) {
										cells_by_group [base_index + c, b * box_width + r] = Index (box, c, r);
									} else {
										throw new Exception ("Unknown rotation");
									}
								}
							}
						}
					}
				}
				#endregion

				/*
				for (int i = 0; i < num_groups; i++) {
					for (int j = 0; j < group_size; j++) {
						Console.Write (cells_by_group [i, j] + " ");
					}

					Console.WriteLine ("");
				}
				*/

				Vector2 [] vertices = new Vector2 [
					box_points.Length
					+ edges.Length * (box_width - 1)
					+ num_boxes * (box_width - 1) * (box_width - 1)
				];
				int [,] cell_points = new int [num_boxes * box_width * box_width, 4];

				#region Generate graphical cell layout
				{
					int w = box_width - 1;

					int [,] edge_vertex_indices = new int [edges.Length, w];
					int [,,] box_vertex_indices = new int [num_boxes, w, w];

					{
						// Vertices unique to an edge or a box
						Vector2 [,] edge_vertices = new Vector2 [edges.Length, w];
						Vector2 [,,] box_vertices = new Vector2 [num_boxes, w, w];

						#region Generate Vertex Locations
						for (int e = 0; e < edges.Length; e++) {
							int i1, i2;
							(i1, i2) = edges [e];

							for (int i = 0; i < box_width - 1; i++) {
								edge_vertices [e, i] = Vector2.Lerp (box_points [i1], box_points [i2], (i + 1f) / box_width);
							}
						}

						for (int b = 0; b < num_boxes; b++) {
							for (int x = 0; x < box_width - 1; x++) {
								for (int y = 0; y < box_width - 1; y++) {
									box_vertices [b, x, y] = Vector2.Lerp (
										Vector2.Lerp (
											box_points [box_corners [b, 0]],
											box_points [box_corners [b, 1]],
											(x + 1f) / box_width
										),
										Vector2.Lerp (
											box_points [box_corners [b, 3]],
											box_points [box_corners [b, 2]],
											(x + 1f) / box_width
										),
										(y + 1f) / box_width
									);
								}
							}
						}
						#endregion

						#region Remap all vertices into a full list
						for (int i = 0; i < box_points.Length; i++) {
							vertices [i] = box_points [i];
						}

						for (int e = 0; e < edges.Length; e++) {
							for (int i = 0; i < w; i++) {
								int ind = box_points.Length + e * w + i;

								vertices [ind] = edge_vertices [e, i];
								edge_vertex_indices [e, i] = ind;
							}
						}

						for (int b = 0; b < num_boxes; b++) {
							for (int x = 0; x < box_width - 1; x++) {
								for (int y = 0; y < box_width - 1; y++) {
									int ind = box_points.Length + edges.Length * w + (b * w + x) * w + y;

									vertices [ind] = box_vertices [b, x, y];
									box_vertex_indices [b, x, y] = ind;
								}
							}
						}
						#endregion
					}

					// Generate cell points
					for (int b = 0; b < num_boxes; b++) {
						// The edge arrays might be the wrong way round, so just make a set that are the right size
						int [,] remapped_edges = new int [4, w];

						for (int e = 0; e < 4; e++) {
							if (box_corners [b, e] < box_corners [b, (e + 1) % 4]) {
								for (int j = 0; j < w; j++) {
									remapped_edges [e, j] = edge_vertex_indices [box_edges [b, e], j];
								}
							} else {
								for (int j = 0; j < w; j++) {
									remapped_edges [e, j] = edge_vertex_indices [box_edges [b, e], w - 1 - j];
								}
							}
						}

						/* 0 ----0---> 1
						 * 
						 * ^           |
						 * |           |
						 * 3           1
						 * |           |
						 * |           v
						 * 
						 * 3 <---2---- 2
						 */
						
						// Generate a consistent map of the vertex indices, so we don't have to
						// deal with corners and edges as special cases
						int [,] remapped_vertices = new int [box_width + 1, box_width + 1];

						{
							remapped_vertices [0, 0] = box_corners [b, 0];
							remapped_vertices [box_width, 0] = box_corners [b, 1];
							remapped_vertices [box_width, box_width] = box_corners [b, 2];
							remapped_vertices [0, box_width] = box_corners [b, 3];

							for (int j = 0; j < w; j++) {
								remapped_vertices [j + 1, 0] = remapped_edges [0, j];
								remapped_vertices [box_width, j + 1] = remapped_edges [1, j];
								remapped_vertices [w - j, box_width] = remapped_edges [2, j];
								remapped_vertices [0, w - j] = remapped_edges [3, j];
							}

							for (int x = 0; x < w; x++) {
								for (int y = 0; y < w; y++) {
									remapped_vertices [x + 1, y + 1] = box_vertex_indices [b, x, y];
								}
							}
						}

						// Use this map to easily populate cell_points
						int ind = b * box_width * box_width;

						for (int x = 0; x < box_width; x++) {
							for (int y = 0; y < box_width; y++) {
								cell_points [ind + x * box_width + y, 0] = remapped_vertices [x + 0, y + 0];
								cell_points [ind + x * box_width + y, 1] = remapped_vertices [x + 1, y + 0];
								cell_points [ind + x * box_width + y, 2] = remapped_vertices [x + 1, y + 1];
								cell_points [ind + x * box_width + y, 3] = remapped_vertices [x + 0, y + 1];
							}
						}
					}
				}
				#endregion

				return new Shape (
					num_cells,
					group_size,
					cells_by_group,
					vertices,
					cell_points,
					"!NO FORMAT!",
					Symmetry.GetSymmetryFromParsedFile (parsed_file, group_size, num_cells)
				);
			} else {
				// Values to be filled
				int group_size = -1;
				int num_cells = -1;
				string format_string = "!FORMAT STRING NOT ASSIGNED!";
				int [,] points_by_group = null;

				format_string = parsed_file [Parser.Headers.format] [0];
				List<string> group_strings = parsed_file [Parser.Headers.groups];

				List<int []> groups = new List<int []> ();

				#region Generate Groups
				for (int i = 0; i < group_strings.Count; i++) {
					int [] [] new_groups = Parser.LexifyString (group_strings [i], out int length);

					if (num_cells == -1)
						num_cells = length;

					if (num_cells != length)
						throw new Exception ("Group strings are not of a consistent size.");

					foreach (int [] group in new_groups) {
						// Check that the groups are of a consistent size
						if (group_size == -1)
							group_size = group.Length;

						if (group_size != group.Length)
							throw new Exception ("Groups are not of consistent sizes.");

						groups.Add (group);
					}
				}

				// Convert the groups into a 2D array
				points_by_group = new int [groups.Count, group_size];

				for (int i = 0; i < groups.Count; i++) {
					for (int j = 0; j < group_size; j++) {
						points_by_group [i, j] = groups [i] [j];
					}
				}
				#endregion

				#region Generate Printable Shape
				
				#endregion

				return new Shape (
					num_cells,
					group_size,
					points_by_group,
					null,
					null,
					format_string,
					Symmetry.GetSymmetryFromParsedFile (parsed_file, group_size, num_cells)
				);
			}
		}

		public static Shape SquareGrid (int s = 3, List<string> group_strings = null, Symmetry.Class symmetry_class = Symmetry.Class.Rotational180deg) => StandardGrid (s, s, group_strings, symmetry_class);

		public static Shape StandardGrid (int a, int b, List<string> group_strings = null, Symmetry.Class symmetry_class = Symmetry.Class.Rotational180deg, List<string> extra_shape_strings = null) {
			// a boxes wide, b boxes tall
			// each box b cells wide, a cells tall

			int group_size = a * b;
			int num_cells = group_size * group_size;

			int Index (int x, int y) => y * group_size + x;

			int [,] points_by_group = new int [group_size * 3 + CountGroupsInString (group_strings), group_size];

			for (int i = 0; i < group_size; i++) {
				for (int j = 0; j < group_size; j++) {
					points_by_group [i + group_size, j] = Index (i, j); // Row
					points_by_group [i, j] = Index (j, i); // Column
				}
			}

			int index = group_size * 2;

			// Boxes
			for (int b_x = 0; b_x < a; b_x++) {
				for (int b_y = 0; b_y < b; b_y++) {
					int sub_index = 0;

					for (int x = 0; x < b; x++) {
						for (int y = 0; y < a; y++) {
							int i = Index (b_x * b + x, b_y * a + y);
							points_by_group [index, sub_index] = i;

							sub_index++;
						}
					}

					index++;
				}
			}

			GenerateGroupsFromString (group_size * 3, group_size, group_strings, points_by_group);

			// Generate formatting string
			string dashes = new string ('-', b * 2 + 1);
			string barrier = "+";
			for (int i = 0; i < a; i++) {
				barrier += dashes + "+";
			}

			string normal_line = "| ";
			for (int x = 0; x < group_size; x++) {
				normal_line += "#" + ((x + 1) % b == 0 ? " | " : " ");
			}

			string box_segment = "";
			for (int x = 0; x < a; x++) {
				box_segment += "\n" + normal_line;
			}
			box_segment += "\n" + barrier;

			string format_string = barrier;
			for (int y = 0; y < b; y++) {
				format_string += box_segment;
			}


			// Generate shape
			Vector2 [] points = null;
			int [,] cell_points = null;

			(points, cell_points) = GenerateSquarePointGrid (group_size);


			// Create shape object
			return new Shape (
				num_cells,
				group_size,
				points_by_group,
				points,
				cell_points,
				format_string, 
				Symmetry.GenerateSquareSymmetry (symmetry_class, group_size)
			);
		}

		public static Shape JigsawGrid (int group_size, List<string> group_strings, Symmetry.Class symmetry_class = Symmetry.Class.Rotational180deg) {
			// Set up rows and columns
			int Index (int x, int y) => y * group_size + x;

			int [,] points_by_group = new int [group_size * 2 + CountGroupsInString (group_strings), group_size];

			for (int i = 0; i < group_size; i++) {
				for (int j = 0; j < group_size; j++) {
					points_by_group [i + group_size, j] = Index (i, j); // Row
					points_by_group [i, j] = Index (j, i); // Column
				}
			}

			int [] group_indices = GenerateGroupsFromString (group_size * 2, group_size, group_strings, points_by_group);

			#region Format string
			// First line
			StringBuilder format_builder = new StringBuilder ("+---");

			for (int i = 1; i < group_size; i++) {
				if (group_indices [Index (i - 1, 0)] == group_indices [Index (i, 0)])
					format_builder.Append ("-");
				else
					format_builder.Append ("+");

				format_builder.Append ("---");
			}

			format_builder.Append ("+\n");

			// Next lines
			for (int y = 0; y < group_size; y++) {
				// Horizontal lines
				if (y >= 1) {
					if (group_indices [Index (0, y - 1)] == group_indices [Index (0, y)])
						format_builder.Append ("|");
					else
						format_builder.Append ("+");

					for (int x = 0; x < group_size; x++) {
						int tl = group_indices [Index (x, y - 1)];
						int bl = group_indices [Index (x, y)];

						if (tl == bl)
							format_builder.Append ("   ");
						else
							format_builder.Append ("---");

						if (x < group_size - 1) {
							int tr = group_indices [Index (x + 1, y - 1)];
							int br = group_indices [Index (x + 1, y)];

							if (tl == bl && tl == tr && tl == br)
								format_builder.Append (" ");
							else if (tl == bl && tr == br)
								format_builder.Append ("|");
							else if (tl == tr && bl == br)
								format_builder.Append ("-");
							else
								format_builder.Append ("+");
						}
					}

					if (group_indices [Index (group_size - 1, y - 1)] == group_indices [Index (group_size - 1, y)])
						format_builder.Append ("|");
					else
						format_builder.Append ("+");

					format_builder.Append ("\n");
				}

				// Main line
				format_builder.Append ("|");

				for (int x = 0; x < group_size; x++) {
					if (x >= 1) {
						if (group_indices [Index (x - 1, y)] == group_indices [Index (x, y)])
							format_builder.Append (" ");
						else
							format_builder.Append ("|");
					}

					format_builder.Append (" # ");
				}

				format_builder.Append ("|\n");
			}

			// Last line
			format_builder.Append ("+---");

			for (int i = 1; i < group_size; i++) {
				if (group_indices [Index (i - 1, group_size - 1)] == group_indices [Index (i, group_size - 1)])
					format_builder.Append ("-");
				else
					format_builder.Append ("+");

				format_builder.Append ("---");
			}

			format_builder.Append ("+");
			#endregion

			
			Vector2 [] points = null;
			int [,] cell_points = null;

			(points, cell_points) = GenerateSquarePointGrid (group_size);


			return new Shape (
				group_size * group_size,
				group_size,
				points_by_group,
				points,
				cell_points,
				format_builder.ToString (),
				Symmetry.GenerateSquareSymmetry (symmetry_class, group_size)
			);
		}

		public static Shape StarGrid (int box_width, int symmetry_number, string petal_string = null, bool force_symmetry = false) {
			int num_cells = box_width * box_width * symmetry_number;
			int group_size = 2 * box_width;

			Utils.AssertEqual (num_cells % group_size, 0);

			List<(int, int)> decoded_petal_string = new List<(int, int)> ();

			// Decode petal string
			if (petal_string == null) {
				Utils.AssertEqual (box_width, 2);

				for (int i = 0; i < box_width * box_width; i++) {
					decoded_petal_string.Add ((0, 0));
				}
			} else {
				foreach (char c in petal_string) {
					if (Parser.petal_last_alpha.Contains (c)) {
						decoded_petal_string.Add ((-1, Parser.petal_last_alpha.IndexOf (c)));
					} else if (Parser.petal_current_alpha.Contains (c)) {
						decoded_petal_string.Add ((0, Parser.petal_current_alpha.IndexOf (c)));
					} else if (Parser.petal_next_alpha.Contains (c)) {
						decoded_petal_string.Add ((1, Parser.petal_next_alpha.IndexOf (c)));
					}
				}
			}

			Utils.AssertEqual (box_width * box_width, decoded_petal_string.Count);

			int [,] cells_by_group = new int [box_width * symmetry_number + num_cells / group_size, group_size];

			// Fast polynomial evaluations (expands to petal_num * box_width * box_width + y * box_width + x)
			int Index (int petal_num, int x, int y) => (petal_num * box_width + y) * box_width + x;

			#region Generate Groups
			// (rows/cols)
			for (int p = 0; p < symmetry_number; p++) {
				for (int g = 0; g < box_width; g++) {
					int group_index = p * box_width + g;

					for (int i = 0; i < box_width; i++) {
						cells_by_group [group_index, i] = Index (p, g, i);
						cells_by_group [group_index, i + box_width] = Index (
							Utils.ProperMod (p + 1, symmetry_number),
							i,
							box_width - 1 - g
						);
					}
				}
			}

			// (boxes/whatever's in the petal string)
			int [] group_index_counters = new int [num_cells / group_size];

			int groups_per_box = box_width / 2;
			
			for (int y = 0; y < box_width; y++) {
				for (int x = 0; x < box_width; x++) {
					for (int p = 0; p < symmetry_number; p++) {
						int petal_offset, sub_petal_group_index;

						(petal_offset, sub_petal_group_index) = decoded_petal_string [x + y * box_width];

						int group_index = Utils.ProperMod (p + petal_offset, symmetry_number) * groups_per_box + sub_petal_group_index;
						int cell_index = Index (p, x, y);

						cells_by_group [symmetry_number * box_width + group_index, group_index_counters [group_index]] = cell_index;

						group_index_counters [group_index]++;
					}
				}
			}
			#endregion

			#region Generate Format String
			string RepeatAndJoin (string sep, int count, string part) => string.Join (sep, Enumerable.Repeat (part, count));

			string head_tail_line = "+" + new string ('-', 2 * box_width + 1) + "+";

			string box_string = head_tail_line + "\n" + RepeatAndJoin (
				"\n", box_width,
				"| " + RepeatAndJoin (
					" ", box_width,
					"#"
				) + " |"
			) + "\n" + head_tail_line;

			string format_string = RepeatAndJoin ("\n", symmetry_number, box_string) + "\n";
			#endregion


			Vector2 [] points = new Vector2 [1 + symmetry_number * (box_width + 1) * box_width];
			int [,] cell_points = new int [num_cells, 4];

			#region Generate Grid Coordinates
			int PointIndex (int p, int x, int y) {
				if (x == 0 && y == box_width)
					return 0;

				if (y == box_width)
					return PointIndex (p + 1, 0, box_width - x);

				return Utils.ProperMod (p, symmetry_number) * box_width * (box_width + 1)
					+ y * (box_width + 1)
					+ x
					+ 1;
			}

			float a = (float)(2 * Math.PI / symmetry_number);

			// Point coordinates
			for (int p = 0; p < symmetry_number; p++) {
				Vector2 i_hat = Vector2.FromPolar (a * (p + 1));
				Vector2 j_hat = Vector2.FromPolar (a * p);

				for (int x = 0; x < box_width + 1; x++) {
					for (int y = 0; y < box_width + 1; y++) {
						int i = PointIndex (p, x, y);

						points [i] = i_hat * x + j_hat * (box_width - y);
					}
				}
			}

			// Cell corners
			for (int p = 0; p < symmetry_number; p++) {
				for (int x = 0; x < box_width; x++) {
					for (int y = 0; y < box_width; y++) {
						int i = Index (p, x, y);

						cell_points [i, 0] = PointIndex (p, x, y);
						cell_points [i, 1] = PointIndex (p, x + 1, y);
						cell_points [i, 2] = PointIndex (p, x + 1, y + 1);
						cell_points [i, 3] = PointIndex (p, x, y + 1);
					}
				}
			}
			#endregion

			Symmetry symmetry = null;

			#region Generate Symmetry
			if (force_symmetry) {
				int [] groups = new int [num_cells];

				for (int i = 0; i < box_width * box_width; i++) {
					for (int j = 0; j < symmetry_number; j++) {
						groups [j * box_width * box_width + i] = i;
					}
				}

				symmetry = new Symmetry (num_cells, groups);
			}
			#endregion

			return new Shape (
				num_cells,
				group_size,
				cells_by_group,
				points,
				cell_points,
				format_string,
				symmetry
			);
		}

		public static Shape CubeGrid () {
			int box_width = 4;
			int box_size = box_width * box_width;

			int [,] cell_index = new int [box_width * 3, box_width * 4];
			bool [,] cell_mask = new bool [box_width * 3, box_width * 4];

			int [,] cell_groups = new int [6 + box_width * 3, box_size];

			for (int i = 0; i < box_width; i++) {
				for (int j = 0; j < box_width; j++) {
					cell_mask [i + box_width * 1, j + box_width * 0] = true;
					cell_mask [i + box_width * 0, j + box_width * 1] = true;
					cell_mask [i + box_width * 1, j + box_width * 1] = true;
					cell_mask [i + box_width * 2, j + box_width * 1] = true;
					cell_mask [i + box_width * 1, j + box_width * 2] = true;
					cell_mask [i + box_width * 1, j + box_width * 3] = true;
				}
			}

			{
				int i = 0;

				for (int y = 0; y < box_width * 4; y++) {
					for (int x = 0; x < box_width * 3; x++) {
						if (cell_mask [x, y]) {
							cell_index [x, y] = i;

							i += 1;
						} else {
							cell_index [x, y] = -1;
						}
					}
				}
			}

			#region Groups

			// Boxes
			for (int x = 0; x < box_width; x++) {
				for (int y = 0; y < box_width; y++) {
					for (int k = 0; k < 6; k++) {
						cell_groups [box_width * 3 + 0, x + y * box_width] = cell_index [box_width * 1 + x, box_width * 0 + y];
						cell_groups [box_width * 3 + 1, x + y * box_width] = cell_index [box_width * 0 + x, box_width * 1 + y];
						cell_groups [box_width * 3 + 2, x + y * box_width] = cell_index [box_width * 1 + x, box_width * 1 + y];
						cell_groups [box_width * 3 + 3, x + y * box_width] = cell_index [box_width * 2 + x, box_width * 1 + y];
						cell_groups [box_width * 3 + 4, x + y * box_width] = cell_index [box_width * 1 + x, box_width * 2 + y];
						cell_groups [box_width * 3 + 5, x + y * box_width] = cell_index [box_width * 1 + x, box_width * 3 + y];
					}
				}
			}

			// Loops
			for (int g = 0; g < box_width; g++) {
				for (int c = 0; c < box_width; c++) {
					/*		....
							....
							....
							....

					   aaaa aaaa aaaa
					   bbbb bbbb bbbb
					   cccc cccc cccc
					   dddd dddd dddd

							....
							....
							....
							....

							dddd
							cccc
							bbbb
							aaaa
					*/
					cell_groups [box_width * 0 + g, box_width * 0 + c] = cell_index [box_width * 0 + c, box_width * 1 + g];
					cell_groups [box_width * 0 + g, box_width * 1 + c] = cell_index [box_width * 1 + c, box_width * 1 + g];
					cell_groups [box_width * 0 + g, box_width * 2 + c] = cell_index [box_width * 2 + c, box_width * 1 + g];
					cell_groups [box_width * 0 + g, box_width * 3 + c] = cell_index [box_width * 1 + c, box_width * 3 + 3 - g];

					/*		 aaaa
							 bbbb
							 cccc
							 dddd

						abcd .... dcba
						abcd .... dcba
						abcd .... dcba
						abcd .... dcba

							 dddd
							 cccc
							 bbbb
							 aaaa
							 
							 ....
							 ....
							 ....
							 ....
					*/
					cell_groups [box_width * 1 + g, box_width * 0 + c] = cell_index [box_width * 1 + c    , box_width * 0 + g];
					cell_groups [box_width * 1 + g, box_width * 1 + c] = cell_index [box_width * 0 + 3 - g, box_width * 1 + c];
					cell_groups [box_width * 1 + g, box_width * 2 + c] = cell_index [box_width * 2 + g    , box_width * 1 + c];
					cell_groups [box_width * 1 + g, box_width * 3 + c] = cell_index [box_width * 1 + c    , box_width * 2 + 3 - g];
				}
			}

			/*		 abcd
					 abcd
					 abcd
					 abcd

				.... abcd ....
				.... abcd ....
				.... abcd ....
				.... abcd ....

					 abcd
					 abcd
					 abcd
					 abcd

					 abcd
					 abcd
					 abcd
					 abcd
			*/
			for (int g = 0; g < box_width; g++) {
				for (int c = 0; c < box_width * 4; c++) {
					cell_groups [box_width * 2 + g, c] = cell_index [box_width * 1 + g, c];
				}
			}

			#endregion

			Vector2 [] points = null;
			int [,] cell_points = null;

			(points, cell_points) = GenerateSquarePointGrid (box_width * 3, box_width * 4, cell_mask);

			return new Shape (
				box_size * 6,
				box_size,
				cell_groups,
				points,
				cell_points,
				@"          +---------+
          | # # # # |
          | # # # # |
          | # # # # |
          | # # # # |
+---------+---------+---------+
| # # # # | # # # # | # # # # |
| # # # # | # # # # | # # # # |
| # # # # | # # # # | # # # # |
| # # # # | # # # # | # # # # |
+---------+---------+---------+
          | # # # # |
          | # # # # |
          | # # # # |
          | # # # # |
          +---------+
          | # # # # |
          | # # # # |
          | # # # # |
          | # # # # |
          +---------+",
				null
			);
		}

		private static (Vector2 [], int [,]) GenerateSquarePointGrid (int width, int height = -1, bool [,] mask = null) {
			if (height == -1)
				height = width;

			int num_cells = width * height;

			int PointIndex (int x, int y) => y * (width + 1) + x;
			int Index (int x, int y) => y * width + x;

			// Generate shape
			Vector2 [] points = new Vector2 [(width + 1) * (width + 1)];
			int [,] cell_points = new int [num_cells, 4];

			for (int x = 0; x < width + 1; x++) {
				for (int y = 0; y < height + 1; y++) {
					points [PointIndex (x, y)] = new Vector2 (x, y);
				}
			}

			/*  +----->   <X>
			 *  |
			 *  |
			 *  V
			 *  
			 * <Y>
			 * 
			 * [i,0] -- [i,1]
			 *   |        |
			 *   |        |
			 *   |        |
			 * [i,3] -- [i,2]
			 */

			int i = 0;

			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					// int i = Index (x, y);

					cell_points [i, 0] = PointIndex (x, y);
					cell_points [i, 1] = PointIndex (x + 1, y);
					cell_points [i, 2] = PointIndex (x + 1, y + 1);
					cell_points [i, 3] = PointIndex (x, y + 1);

					i += 1;
				}
			}

			return (points, cell_points);
		}

		private static int [] GenerateGroupsFromString (int start_index, int group_size, List<string> group_strings, int [,] points_by_group) {
			if (group_strings == null)
				return null;

			int unknown_char_index = start_index;
			Dictionary<char, int> lex_table = new Dictionary<char, int> ();
			Dictionary<char, int> uses_table = new Dictionary<char, int> ();

			int [] group_indices = new int [group_size * group_size];

			int point_index = 0;
			foreach (string s in group_strings) {
				foreach (char c in s) {
					if (c == '.') {
						point_index++;

						continue;
					}

					if (Parser.whitespace_chars.Contains (c))
						continue;

					if (lex_table.ContainsKey (c)) {
						points_by_group [lex_table [c], uses_table [c]] = point_index;

						uses_table [c]++;

						group_indices [point_index] = lex_table [c];
					} else {
						lex_table.Add (c, unknown_char_index);
						uses_table.Add (c, 1);

						points_by_group [unknown_char_index, 0] = point_index;

						group_indices [point_index] = unknown_char_index;

						unknown_char_index++;
					}

					point_index++;
				}
			}

			// Utils.AssertEqual (point_index, group_size * group_size);
			foreach (int i in uses_table.Values) {
				Utils.AssertEqual (i, group_size);
			}

			return group_indices;
		}

		private static int CountGroupsInString (List<string> group_strings) {
			if (group_strings == null)
				return 0;

			HashSet<char> chars = new HashSet<char> ();

			foreach (string s in group_strings) {
				foreach (char c in s) {
					if (Parser.whitespace_chars.Contains (c))
						continue;

					if (c == '.')
						continue;

					chars.Add (c);
				}
			}

			return chars.Count;
		}

		const float svg_text_size = 0.4f;
		const float svg_baseline_fix = -0.3f;
		const float svg_standard_line_width = 0.5f; // px
		const float svg_box_line_width = 3; // px
		public const float svg_scale = 100; // px per unit
		public const float svg_margin = 10; // px
		const string unit = "px";
		static bool add_dots_for_outputs = false;
	}
}
