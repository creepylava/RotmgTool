using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RotmgTool.Network;

namespace RotmgTool
{
	internal class SpamFilter
	{
		private static int EditDistance(string x, string y)
		{
			// Validate parameters
			if (x == null) throw new ArgumentNullException("x");
			if (y == null) throw new ArgumentNullException("y");


			// Get the length of both.  If either is 0, return
			// the length of the other, since that number of insertions
			// would be required.
			int n = x.Length, m = y.Length;
			if (n == 0) return m;
			if (m == 0) return n;

			// Rather than maintain an entire matrix (which would require O(n*m) space),
			// just store the current row and the next row, each of which has a length m+1,
			// so just O(m) space. Initialize the current row.
			int curRow = 0, nextRow = 1;
			int[][] rows = { new int[m + 1], new int[m + 1] };
			for (int j = 0; j <= m; ++j) rows[curRow][j] = j;

			// For each virtual row (since we only have physical storage for two)
			for (int i = 1; i <= n; ++i)
			{
				// Fill in the values in the row
				rows[nextRow][0] = i;
				for (int j = 1; j <= m; ++j)
				{
					int dist1 = rows[curRow][j] + 1;
					int dist2 = rows[nextRow][j - 1] + 1;
					int dist3 = rows[curRow][j - 1] +
					            (x[i - 1].Equals(y[j - 1]) ? 0 : 1);

					rows[nextRow][j] = Math.Min(dist1, Math.Min(dist2, dist3));
				}

				// Swap the current and next rows
				if (curRow == 0)
				{
					curRow = 1;
					nextRow = 0;
				}
				else
				{
					curRow = 0;
					nextRow = 1;
				}
			}

			// Return the computed edit distance
			return rows[curRow][m];
		}

		private Dictionary<string, string> replTbl;
		private List<string> terms;
		private int termViewMax = 10;
		private int termViewMin = int.MaxValue;

		private readonly IToolInstance tool;

		public SpamFilter(IToolInstance tool)
		{
			this.tool = tool;
		}

		public void LoadWordList()
		{
			replTbl = new Dictionary<string, string>();
			terms = new List<string>();
			string dat = File.ReadAllText(Path.Combine(Program.RootDirectory, "data.txt"));
			bool repl = false;
			using (var reader = new StringReader(dat))
			{
				while (reader.Peek() > 0)
				{
					string line = reader.ReadLine();

					if (line[0] == '#')
						continue;
					if (line == "---")
					{
						repl = !repl;
						continue;
					}

					if (repl)
					{
						string[] r = line.Split(' ');
						if (r.Length != 2)
							throw new Exception("Invalid replace table format");
						if (replTbl.ContainsKey(r[0]))
							throw new Exception("Duplicated replace table key");
						replTbl[r[0]] = r[1];
					}
					else
					{
						if (line.Contains(" "))
							throw new Exception("No space allowed in filter words");
						if (line.Any(x => char.IsPunctuation(x)))
							throw new Exception("No punctuation allowed in filter words");
						terms.Add(line.ToUpper());
						if (line.Length > termViewMax) termViewMax = line.Length;
						if (line.Length < termViewMin) termViewMin = line.Length;
					}
				}
			}
			termViewMax += termViewMin;
		}

		public bool IsSpam(string text)
		{
			if (text.StartsWith("{\"key\":\"")) return false;

			foreach (var i in replTbl)
				text = text.Replace(i.Key, i.Value);

			var final = new StringBuilder();
			char prev = '\0';
			foreach (char i in text)
			{
				if (i == prev) continue;

				if (char.IsWhiteSpace(i) || char.IsPunctuation(i)) continue;
				final.Append(char.ToUpper(i));

				prev = i;
			}
			text = final.ToString();
			for (var len = termViewMin; len < termViewMax; len++)
				for (var i = 0; i < text.Length - len + 1; i++)
				{
					string x = text.Substring(i, len);
					foreach (string entry in terms)
					{
						var dist = EditDistance(entry, x);
						if ((float)dist / x.Length < 0.3)
						{
							return true;
						}
					}
				}
			return false;
		}

		public double Randomness(string s)
		{
			return (double)s.Length - s.Distinct().Count();
		}

		public bool IsSpam(TextPacket packet)
		{
			if (packet.star == 0 && tool.Settings.GetValue<bool>("spam.0starspam", "true")) return true;

			if (tool.Settings.GetValue<bool>("spam.randomnamespam", "true") && MarkovFilter.isRandom(packet.name)) return true;

			if (packet.name.Length > 0 && (packet.name[0] == '*' || packet.name[0] == '@')) return false;

			return IsSpam(packet.text);
		}
	}

	// https://github.com/rrenaud/Gibberish-Detector
	internal class MarkovFilter
	{
		private const string accepted_chars = "abcdefghijklmnopqrstuvwxyz";
		private static readonly Dictionary<char, int> pos = accepted_chars.ToDictionary(x => x, x => accepted_chars.IndexOf(x));

		private static IEnumerable<char> normalize(string line)
		{
			return line.ToLower().Where(x => accepted_chars.Contains(x));
		}

		private static IEnumerable<string> ngram(int n, string l)
		{
			var filtered = new string(normalize(l).ToArray());
			for (int i = 0; i < filtered.Length - n; i++)
			{
				yield return filtered.Substring(i, n);
			}
		}

		private static double avg_transition_prob(string l, double[][] log_prob_mat)
		{
			double log_prob = 1;
			int transition_ct = 0;
			foreach (var i in ngram(2, l))
			{
				log_prob += log_prob_mat[pos[i[0]]][pos[i[1]]];
				transition_ct++;
			}
			return Math.Exp(log_prob / (transition_ct == 0 ? 1 : transition_ct));
		}

		private static readonly double[][] counts;
		private static readonly double threshold;

		static MarkovFilter()
		{
			int k = accepted_chars.Length;

			var dat = typeof(MarkovFilter).Assembly.GetManifestResourceStream("RotmgTool.matrix.dat");
			using (var rdr = new BinaryReader(dat))
			{
				counts = new double[k][];
				for (int i = 0; i < k; i++)
				{
					counts[i] = new double[k];
					for (int j = 0; j < k; j++)
						counts[i][j] = rdr.ReadDouble();
				}
				threshold = rdr.ReadDouble();
			}
		}

		public static bool isRandom(string val)
		{
			return avg_transition_prob(val, counts) < threshold;
		}
	}
}