using System.Drawing;

namespace SearchTest
{
	using Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns;
	using Microsoft.VisualStudio.TestPlatform.ObjectModel;
	using Newtonsoft.Json.Linq;

	using Search.Interfaces;

	using System.Collections.Concurrent;
	using System.Diagnostics;
	using System.Reflection;
	using System.Text;

	[TestClass]
	public class SearchUnitTests
	{
		private const string pattern = "456";
		private const string buffer = "12345678.......abcdef.......123456..#@%45/////..........456455////////4$$$$$////////////456";
		private static readonly Memory<byte> patternMemory = Encoding.UTF8.GetBytes(pattern).AsMemory();
		private static readonly Memory<byte> bufferMemory = Encoding.UTF8.GetBytes(buffer).AsMemory();
		private static readonly List<int> simpleExpectedOffsets = new() { 3, 31, 56, 88 };
		private static readonly ConcurrentDictionary<Type, List<int>> resultsMap = new();

		public static List<int> createOffsets(Type type, int offset) => new() { offset };
		public static List<int> appendOffsets(Type type, List<int> offsets, int offset)
		{
			int[] result = new int[offsets.Count + 1];
			offsets.CopyTo(result, 0);
			result[offsets.Count] = offset;
			return result.ToList();
		}
		public static bool DisplayOffset(int offset, Type caller)
		{
			_ = resultsMap.AddOrUpdate<int>(caller, createOffsets, appendOffsets, offset);

			//Trace.WriteLine($"({caller.FullName}) has found \"{pattern}\" at offset: {offset}");
			return true;
		}

		/*****************************************************************************************************************
		*******************************************************************************************************************
		*** Changes for Visual Studio 2022
		*******************************************************************************************************************
		***
		*** https://docs.microsoft.com/en-us/visualstudio/test/mstest-update-to-mstestv2?view=vs-2022
		*** Remove the assembly reference to Microsoft.VisualStudio.QualityTools.UnitTestFramework from your unit test project.
		*** Add NuGet package references to MSTestV2 including the MSTest.TestFramework and the MSTest.TestAdapter packages on nuget.org. You can install packages in the NuGet Package Manager Console with the following commands:
		*** Console
		*** Copy
		*** PM> Install-Package MSTest.TestAdapter -Version 2.1.2
		*** PM> Install-Package MSTest.TestFramework -Version 2.1
		***
		********************************************************************************************************************
		 *****************************************************************************************************************/

		public const int dividerWidth = 150 - 1;
		public const char charLightHorizontalLine = '\u2500';
		public const char charHeavyHorizontalLine = '\u2501';
		public const char charLightVerticalLine = '\u2502';
		public const char charHeavyVerticalLine = '\u2503';
		public static readonly string singleDivider = string.Intern(string.Concat(Enumerable.Repeat<char>('-', dividerWidth)));
		public static readonly string doubleDivider = string.Intern(string.Concat(Enumerable.Repeat<char>('=', dividerWidth)));
		public static readonly string lightDivider = string.Intern(string.Concat(Enumerable.Repeat<char>(charLightHorizontalLine, dividerWidth)));
		public static readonly string heavyDivider = string.Intern(string.Concat(Enumerable.Repeat<char>(charHeavyHorizontalLine, dividerWidth)));
		public static readonly string stringLightVerticalLine = charLightVerticalLine.ToString();
		public static readonly string stringHeavyVerticalLine = charHeavyVerticalLine.ToString();


		public class SearchStatistics
		{
			public readonly List<int> Offsets;
			public long InitTime;
			public long SearchTime;

			public SearchStatistics()
			{
				this.Offsets = new List<int>();
				this.InitTime = 0;
				this.SearchTime = 0;
			}
			public long IncrementInitializationTime(long value) => System.Threading.Interlocked.Add(ref this.InitTime, value);
			public long IncrementSearchTime(long value) => System.Threading.Interlocked.Add(ref this.SearchTime, value);

			public double InitMilliseconds => TimeSpan.FromTicks(this.InitTime).TotalMilliseconds;
			public double SearchMilliseconds => TimeSpan.FromTicks(this.SearchTime).TotalMilliseconds;
			public double TotalMilliseconds => TimeSpan.FromTicks(this.InitTime + this.SearchTime).TotalMilliseconds;
		};


		public record StatTimes(long InitTime, long SearchTime, long TotalTime);


		public static int GetRandom(int min, int max) => Random.Shared.Next(min, max);
		public static void GetRandomBytes(ref byte[] bytes) => Random.Shared.NextBytes(bytes);


		public class SearchCore
		{
			protected byte[]? core;

			public byte[]? Value
			{
				get => this.core;
				protected set => this.core = value;
			}

			public SearchCore()
			{
				this.core = null;
			}

			public SearchCore(string value)
			{
				ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
				this.core = Encoding.UTF8.GetBytes(value);
			}

			public SearchCore(in byte[] value)
			{
				this.core = value;
			}

			public SearchCore(int minLength, int maxLength)
			{
				int paternSize = GetRandom(minLength, maxLength);
				this.core = new byte[paternSize];
				Random.Shared.NextBytes(this.core);
			}

			public List<int> GenerateRandomOffsets(in SearchCore other, int minOccurences, int maxOccurences)
			{
				ArgumentNullException.ThrowIfNull(other.Value, $"other={nameof(other.Value)}");
				ArgumentNullException.ThrowIfNull(this.Value, $"this={nameof(this.Value)}");

				List<int> offsets = new List<int>();

				int patternSize = other.Value!.Length;
				int bufferSize = this.Value!.Length;
				int maxTestPatterns = maxOccurences - minOccurences + 1;

				int lastSearchableOffset = bufferSize - patternSize;
				int offset = 0;
				for (int i = 0; i < maxTestPatterns && offset + patternSize <= lastSearchableOffset; ++i)
				{
					int current = Random.Shared.Next(offset, Math.Min(offset + (bufferSize / patternSize), lastSearchableOffset));
					offset = current + patternSize;  //set next offset start boundary
					offsets.Add(current);
				}
				return offsets;
			}

			public List<int> GenerateTestOffsets(in SearchCore other)
			{
				List<int> offsets = new List<int>();

				int patternSize = other.Value!.Length;
				int bufferSize = this.Value!.Length;
				int lastSearchableOffset = bufferSize - patternSize;

				offsets.Add(0);
				offsets.Add(lastSearchableOffset);

				return offsets;
			}

			public void Fill(byte value)
			{
				ArgumentNullException.ThrowIfNull(this.Value, $"this={nameof(this.Value)}");
				Array.Fill(this.Value!, value);
			}

			public void Fill(in SearchCore other, IReadOnlyList<int> offsets)
			{
				ArgumentNullException.ThrowIfNull(other.Value, $"other={nameof(other.Value)}");
				ArgumentNullException.ThrowIfNull(this.Value, $"this={nameof(this.Value)}");

				for (int i = 0; i < offsets.Count; ++i)
				{
					int offset = offsets[i];
					other.Value!.CopyTo(this.Value!, offset);
				}
			}
		};


		//public delegate void InitializeBufferDelegate<T>(in T value, ref byte[] buffer);
		//InitializeBufferDelegate<byte[]> InitializeBytes = (in byte[] value, ref byte[] bytes) =>
		//{
		//	bytes = value;
		//};
		//InitializeBufferDelegate<string> InitializeString = (in string value, ref byte[] bytes) =>
		//{
		//	bytes = Encoding.UTF8.GetBytes(pattern);
		//};


		[TestMethod]
		[Timeout(38400 * 1000)]
		public void Test_All_ISearch_Derivates()
		{
			Assembly asm = typeof(Search.Interfaces.ISearch).Assembly;
			ArgumentNullException.ThrowIfNull(asm, nameof(asm));
			Trace.AutoFlush = true;

			Trace.WriteLine(heavyDivider);
			Trace.WriteLine(lightDivider);

			const int maxTestIterations = 2;	// 5;	// 10;  // 20;
			const int maxTestPatterns = 1000; // maximal amount of matching byte sequences, which will be distributed randomly over the buffer
			const int minPatternSize = 3;
			const int maxPatternSize = 273;
			const int minBufferSize = 1048576 * 16;
			const int maxBufferSize = minBufferSize * 24;

			//Reference search algorithm is now BruteForce, choosen over its simplicity (and slowness)
			ISearch referenceSearch = new Search.Algorithms.BruteForce();
			Type referenceType = referenceSearch.GetType();

			Dictionary<Type, SearchStatistics> statistics = new();
			for (int testIteration = 1; testIteration <= maxTestIterations; ++testIteration)
			{
				//Clear previous offsets and counters only. Keep accumulated timings.
				statistics.Keys.ToList().ForEach(k => statistics[k].Offsets.Clear());

				//Generate both search pattern and buffer over which the search is performed from the random data
				int patternSize = GetRandom(minPatternSize, maxPatternSize);
				int bufferSize = GetRandom(minBufferSize, maxBufferSize);
				int safetyMarginSizeInBytes = 0;	// patternSize;	// 2 zeros are needed for BerryRavindran

				//Generate unique pattern for this iteration from the random data
				byte[] testPattern = new byte[patternSize];
				Random.Shared.NextBytes(testPattern);

				//Allocate buffer where searching algorithms will be looking for the pattern
				byte[] testBuffer = new byte[bufferSize + safetyMarginSizeInBytes];
				byte fillByte = testPattern[Random.Shared.Next(minValue: 0, maxValue: patternSize)];
				Array.Fill<byte>(testBuffer, fillByte, 0, bufferSize);
				//Array.Fill<byte>(testBuffer, 0, bufferSize, safetyMarginSizeInBytes);
				//Array.Copy(testPattern, 0, testBuffer, bufferSize, patternSize);	//only for BackwardFast: copy pattern after the search buffer end

				Trace.WriteLine($"Generator: iteration #{testIteration,5}, fillByte:0x{fillByte:X2}, patternSize:{patternSize,6}, bufferSize:{bufferSize,16:###,###,###,###}, testBuffer.Length:{testBuffer.Length}");

				List<int> testOffsets = new List<int>();

				int lastSearchableOffset = bufferSize - patternSize;
				int testOffset = 0;
				for (int i = 0; i < maxTestPatterns && testOffset + patternSize <= lastSearchableOffset; ++i)
				{
					int offset = Random.Shared.Next(testOffset, Math.Min(testOffset + (bufferSize / patternSize), lastSearchableOffset));
					testOffset = offset + patternSize;	//set next offset start boundary
					testOffsets.Add(offset);
					testPattern.CopyTo(testBuffer, offset);
					//Trace.WriteLine($"Generator: inserting at {offset}");
				}

				List<int> referenceOffsets = new List<int>();
				referenceSearch.Init(testPattern, (int offset, Type caller) => { referenceOffsets.Add(offset); return true; });
				referenceSearch.Search(testBuffer, 0);
				referenceOffsets.Sort();

				if (referenceOffsets.Count != testOffsets.Count || !referenceOffsets.SequenceEqual(testOffsets))
				{
					IEnumerable<int> intersect = referenceOffsets.Intersect(testOffsets);
					IEnumerable<int> reference = referenceOffsets.Except(intersect);
					IEnumerable<int> current = testOffsets.Except(intersect);

					for (int i = 0; i < reference.Count(); ++i)
					{
						Trace.WriteLine($"algorithm \"{referenceSearch.GetType().FullName}\" only at position {i}: {reference.ElementAt(i)}");
					}

					for (int i = 0; i < current.Count(); ++i)
					{
						Trace.WriteLine($"test offsets at position {i}: {current.ElementAt(i)}");
					}

					Assert.Fail($"Reference offsets (Count={referenceOffsets.Count}) not equal to Test offsets (Count={testOffsets.Count})");
				}

				Stopwatch initWatch = new();
				Stopwatch searchWatch = new();

				//double timeUpscaling = 1000.0;
				//double timeDownscaling = 0.001;

				HashSet<Type> blacklistedTypes = new HashSet<Type>()
				{
					typeof(Search.Common.SearchBase)					//Skip SearchBase itself (TODO?: use IsInherited)
				};
				List<Type> blacklistedAttributes = new List<Type>()
				{
					typeof(Search.Attributes.UnstableAttribute),
					typeof(Search.Attributes.ExperimentalAttribute),
					typeof(Search.Attributes.SlowAttribute),
				};
				foreach (Type type in ((TypeInfo[])asm.DefinedTypes).Select(t => t.UnderlyingSystemType))
				{
					//hasMetric equals true if type is inheriting from ISearch interface
					bool hasMetric = type.GetInterfaces().Contains(typeof(Search.Interfaces.ISearch));

					//Skip if type is not a class, or it is an abstract class, or has no implemented ISearch interface
					if (!type.IsClass || type.IsAbstract || !hasMetric)
					{
						continue;
					}

					//Skip blacklisted tyoes, like base classes
					if (blacklistedTypes.Contains(type))
					{
						continue;
					}

					//Skip search algorithm class if marked with blacklisted Attribute
					if (blacklistedAttributes.Any(x => type.IsDefined(x)))
					{
						continue;
					}

					//If statistics context doesn't contain SearchStatistics object, this is the first occurence, add new one
					if (!statistics.ContainsKey(type))
					{
						statistics.Add(type, new SearchStatistics());
					}


					//Create instance of generic search algorithm exposing ISearch interface
					Assembly assembly = type.Assembly;
					ISearch genericSearch = (ISearch)(assembly.CreateInstance(type.FullName!, false) ?? throw new ApplicationException(type.FullName));

					//System.Numerics.BitOperations.PopCount();
					//Trace.WriteLine($"Running \"{type.FullName}\"");

					//Accumulate duration of initialization for each generic search algorithm
					initWatch.Restart();

					//Lambda inline function advises search algorithm implementation what is the search pattern and the delegate whic receives the offset when pattern is found.
					genericSearch.Init
					(
						testPattern,
						(int offset, Type caller) =>
						{
							statistics[caller].Offsets.Add(offset);
							return true;
						}
					);
					//Stop the timer, and increment initialization time statistics
					initWatch.Stop();
					_ = statistics[type].IncrementInitializationTime(initWatch.Elapsed.Ticks);

					//Accumulate duration of search for each generic search algorithm
					searchWatch.Restart();
					try
					{
						genericSearch.Search(testBuffer, 0);
					}
					catch (Exception ex)
					{
						Assert.Fail($"SEARCH EXCEPTION: Type={type}, Details:{ex}", ex);
					}
					searchWatch.Stop();
					_ = statistics[type].IncrementSearchTime(searchWatch.Elapsed.Ticks);
				}

				int discrepancies = 0;
				foreach (Type key in statistics.Keys.OrderBy(x => x.FullName, StringComparer.Ordinal))
				{
					if (key.Equals(referenceType))
					{
						continue;
					}

					List<int> offsets = statistics[key].Offsets;
					offsets.Sort();
					if (offsets.Count != referenceOffsets.Count || !offsets.SequenceEqual(referenceOffsets))
					{
						++discrepancies;
						Trace.WriteLine($"results of the algorithm run \"{key}\" differs from brute force");

						IEnumerable<int> intersect = referenceOffsets.Intersect(offsets);
						IEnumerable<int> reference = referenceOffsets.Except(intersect);
						IEnumerable<int> current = offsets.Except(intersect);

						for (int i = 0; i < reference.Count(); ++i)
						{
							Trace.WriteLine($"algorithm \"{referenceSearch.GetType().FullName}\" only at position {i}: {reference.ElementAt(i)}");
						}

						for (int i = 0; i < current.Count(); ++i)
						{
							Trace.WriteLine($"algorithm \"{key.FullName}\" match at position {i}: {current.ElementAt(i)}");
						}
					}
				}
				if (discrepancies != 0)
				{
					Debug.WriteLine($"Total {discrepancies} discrepancies.");
					Assert.Fail();
				}
			} //END: for(int testIteration

			Trace.WriteLine(lightDivider);

			int maxName = statistics.Keys.Select(t => t.FullName!.Length).Max();
			IEnumerable<StatTimes> totalsList = statistics.Values.Select(value => new StatTimes(value.InitTime, value.SearchTime, value.InitTime + value.SearchTime));
			StatTimes grandTotals = totalsList.Aggregate((a, b) => new StatTimes(a.InitTime + b.InitTime, a.SearchTime + b.SearchTime, a.TotalTime + b.TotalTime));
			double grandInit = TimeSpan.FromTicks(grandTotals.InitTime).TotalMilliseconds;
			double grandSearch = TimeSpan.FromTicks(grandTotals.SearchTime).TotalMilliseconds;
			double grandTotal = TimeSpan.FromTicks(grandTotals.InitTime + grandTotals.SearchTime).TotalMilliseconds;

			string verticalSeparator = string.Concat(" ", stringLightVerticalLine, " ");

			List<string[]> statColumns = new List<string[]>();
			double referenceTime = statistics[referenceType].TotalMilliseconds;
			const string stringRef = @" [Ref]";
			foreach (Type type in statistics.Keys.OrderBy(t => statistics[t].InitTime + statistics[t].SearchTime))
			{
				SearchStatistics stats = statistics[type];

				List<string> statColumn = new List<string>();

				if(type.Equals(referenceType))
				{ statColumn.Add(string.Concat(type.FullName!, stringRef).PadRight(maxName + stringRef.Length)); }
				else
				{ statColumn.Add(type.FullName!.PadRight(maxName + stringRef.Length)); }

				statColumn.Add($"{stats.InitMilliseconds:##0.000}");
				statColumn.Add($"{stats.InitMilliseconds * 100.0 / grandTotal:##0.00}");
				statColumn.Add($"{stats.SearchMilliseconds:##0.000}");
				statColumn.Add($"{stats.SearchMilliseconds * 100.0 / grandTotal:##0.00}");
				statColumn.Add($"{stats.TotalMilliseconds:##0.000}");
				statColumn.Add($"{stats.TotalMilliseconds * 100.0 / grandTotal:##0.00}");
				statColumn.Add($"{stats.TotalMilliseconds * 100.0 / referenceTime:##0.00}");
				statColumns.Add(statColumn.ToArray());
			}

			//Calculate maximum display column length
			int[] maxColumnLengths = Enumerable.Repeat<int>(0, statColumns.Count).ToArray();
			for (int i = 0; i < statColumns.Count; i++)
			{
				for (int j = 0; j < statColumns[i].Length; ++j)
				{
					maxColumnLengths[j] = Math.Max(maxColumnLengths[j], statColumns[i][j].Length);
				}
			}
			//Pad values to the left to the max length of that value column
			for (int i = 0; i < statColumns.Count; i++)
			{
				for(int j = 0; j < statColumns[i].Length; ++j)
				{
					statColumns[i][j] = statColumns[i][j].PadLeft(maxColumnLengths[j]);
				}
			}

			for (int i = 0; i < statColumns.Count; i++)
			{
				string[] col = statColumns[i];

				int j = 0;
				string[] statStrings =
				{
					col[j++],
					$"Init {col[j++]} ms ({col[j++]}%)",
					$"Search {col[j++]} ms ({col[j++]}%)",
					$"Total {col[j++]} ms ({col[j++]}%)",
					$"Ref.% {col[j++]}"
				};
				//string[] statStrings =
				//{
				//	col[0],
				//	$"Init {col[1]} ms ({col[2]}%)",
				//	$"Search {col[3]} ms ({col[4]}%)",
				//	$"Total {col[5]} ms ({col[6]}%)",
				//	$"Ref.%{col[7]}"
				//};
				Trace.WriteLine(string.Join(verticalSeparator, statStrings.Select(v => string.Concat(v, "")).ToArray()));
			}

			Trace.WriteLine(lightDivider);

			string grand = string.Join(System.Environment.NewLine, new string[]
			{
				$"GrandInit   {grandInit,16:###,###,##0.000} ms",
				$"GrandSearch {grandSearch,16:###,###,##0.000} ms",
				$"GrandTotal  {grandTotal,16:###,###,##0.000} ms"
			});
			Trace.WriteLine(grand);

			Trace.WriteLine(heavyDivider);
		}

	};  //END: class SearchUnitTests
};

