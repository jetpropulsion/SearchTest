using System.Drawing;

namespace SearchTest
{
	using Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns;
	using Microsoft.VisualStudio.TestPlatform.ObjectModel;
	using Newtonsoft.Json.Linq;

	using Search.Interfaces;

	using System.Collections.Concurrent;
	using System.Collections.Generic;
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


		//Method will terminate execution if offset collections are different
		public static bool AssertEqualOffsets(string firstName, ICollection<int> first, string secondName, ICollection<int> second)
		{
			if (first.Count == second.Count && first.SequenceEqual(second))
			{
				return true;
			}

			IEnumerable<int> intersect = first.Intersect(second);
			IEnumerable<int> reference = first.Except(intersect);
			IEnumerable<int> current = second.Except(intersect);

			for (int i = 0; i < first.Count(); ++i)
			{
				Trace.WriteLine($"{firstName} only at position {i}: {first.ElementAt(i)}");
			}
			for (int i = 0; i < current.Count(); ++i)
			{
				Trace.WriteLine($"{secondName} only at position {i}: {current.ElementAt(i)}");
			}
			return false;
		}


		public class SearchTest
		{
			public SearchTest
			(
				string name,
				int minPatternSize, int maxPatternSize,
				int minBufferSize, int maxBufferSize,
				int minDistance, int maxDistance
			)
			{
				this.Name = name;

				this.MinPatternSize = minPatternSize;
				this.MaxPatternSize = maxPatternSize;
				this.MinBufferSize = minBufferSize;
				this.MaxBufferSize = maxBufferSize;
				this.MinDistance = minDistance;
				this.MaxDistance = maxDistance;

				this.PatternGenerator(minPatternSize, maxPatternSize, out this.PatternSize, out this.Pattern);
				this.BufferGenerator(minBufferSize, maxBufferSize, this.PatternSize, out this.BufferSize, out this.Buffer);
			}
			public string Name;
			public int MinPatternSize;
			public int MaxPatternSize;
			public int MinBufferSize;
			public int MaxBufferSize;
			public int MinDistance;
			public int MaxDistance;

			public int PatternSize;
			public int BufferSize;
			public byte[] Pattern;
			public byte[] Buffer;

			public delegate void OffsetGeneratorDelegate(int bufferSize, int patternSize, int minDistance, int maxDistance, out int[] offsets);
			public delegate void PatternGeneratorDelegate(int minPatternSize, int maxPatternSize, out int patternSize, out byte[] pattern);
			public delegate void BufferGeneratorDelegate(int minBufferSize, int maxBufferSize, int safetyMargin, out int bufferSize, out byte[] buffer);
			public delegate void BufferFillDelegate(ref byte[] buffer, int bufferSize, byte fill);
			public delegate void BufferPatternFillDelegate(ref byte[] buffer, int bufferSize, in byte[] pattern, in IReadOnlyList<int> offsets);

			public OffsetGeneratorDelegate OffsetGenerator = DefaultOffsetGenerator;
			public PatternGeneratorDelegate PatternGenerator = DefaultPatternGenerator;
			public BufferGeneratorDelegate BufferGenerator = DefaultBufferGenerator;
			public BufferFillDelegate BufferFill = DefaultBufferFill;
			public BufferPatternFillDelegate BufferPatternFill = DefaultBufferPatternFill;

			public static void DefaultOffsetGenerator(int bufferSize, int patternSize, int minDistance, int maxDistance, out int[] offsets)
			{
				int lastOffset = bufferSize - patternSize;

				List<int> result = new List<int>();

				int next = 0;
				while (next <= lastOffset)
				{
					int distance = Random.Shared.Next(minDistance, maxDistance);
					int nextMax = Math.Min(next + distance, lastOffset);
					int offset = Random.Shared.Next(next, nextMax);
					result.Add(offset);
					next = offset + patternSize; //set next generated offset's start boundary
				}
				//result.Sort();	//NOTE: do not sort - this allows overlapping strings
				offsets = result.ToArray();
			}

			public static void DefaultPatternGenerator(int minPatternSize, int maxPatternSize, out int patternSize, out byte[] pattern)
			{
				patternSize = Random.Shared.Next(minPatternSize, maxPatternSize);
				pattern = new byte[patternSize];
				Random.Shared.NextBytes(pattern);
			}

			public static void DefaultBufferGenerator(int minBufferSize, int maxBufferSize, int safetyMargin, out int bufferSize, out byte[] buffer)
			{
				bufferSize = Random.Shared.Next(minBufferSize, maxBufferSize);
				buffer = new byte[bufferSize + safetyMargin];
				Array.Fill<byte>(buffer, 0, 0, buffer.Length);
			}

			public static void DefaultBufferFill(ref byte[] buffer, int bufferSize, byte value)
			{
				Assert.IsTrue(buffer.Length >= bufferSize);
				Random.Shared.NextBytes(buffer[0..bufferSize]);
			}

			public static void DefaultBufferPatternFill(ref byte[] buffer, int bufferSize, in byte[] pattern, in IReadOnlyList<int> offsets)
			{
				Assert.IsTrue(buffer.Length >= bufferSize);

				int patternSize = pattern.Length;
				for (int i = 0; i < offsets.Count; i++)
				{
					int offset = offsets[i];
					Assert.IsTrue(offset + patternSize <= bufferSize - patternSize);
					pattern.CopyTo(buffer, offset);
				}
			}

		};  //END: class SearchTestParams

		public List<SearchTest> SearchTests = new List<SearchTest>()
		{
			new SearchTest
			(
				name: "Standard",
				minPatternSize: 3,
				maxPatternSize: 273,
				minBufferSize: 1048576 * 16,
				maxBufferSize: 1048576 * 16 * 24,
				minDistance: 0,
				maxDistance: 1048576
				//PatternGenerator: (int, int, out int patternSize, out byte[] pattern) = (minPatternSize, maxPatternSize, patternSize, pattern) =>
				//{

				//}
			)
		};

		[TestMethod]
		[Timeout(38400 * 1000)]
		public void Test_All_ISearch_Derivates()
		{
			Trace.AutoFlush = true;

			Assembly assembly = typeof(Search.Interfaces.ISearch).Assembly;
			ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));

			Trace.WriteLine(heavyDivider);
			Trace.WriteLine(lightDivider);

			//const double timeUpscaling = 1000.0;
			//const double timeDownscaling = 0.001;

			const int maxTestIterations = 2;	// 5;	// 10;  // 20;

			//Reference search algorithm is now BruteForce, choosen over its simplicity (and slowness)
			ISearch referenceSearch = new Search.Algorithms.BruteForce();
			Type referenceSearchType = referenceSearch.GetType();

			Dictionary<Type, SearchStatistics> statistics = new();

			for (int testIteration = 1; testIteration <= maxTestIterations; ++testIteration)
			{
				//Clear previous offsets and counters only. Keep accumulated timings.
				statistics.Keys.ToList().ForEach(k => statistics[k].Offsets.Clear());

				const int minPatternSize = 3;
				const int maxPatternSize = 273;
				//Generate both search pattern and buffer over which the search is performed from the random data
				int patternSize = GetRandom(minPatternSize, maxPatternSize);
				//Generate unique pattern for this iteration from the random data
				byte[] testPattern = new byte[patternSize];
				Random.Shared.NextBytes(testPattern);
				int safetyMarginSizeInBytes = patternSize;  // 2 zeros are needed for BerryRavindran, patternSize for MaximalShift and BackwardFast

				const int minBufferSize = 1048576 * 16;
				const int maxBufferSize = minBufferSize * 24;
				//Allocate buffer where searching algorithms will be looking for the pattern
				int bufferSize = GetRandom(minBufferSize, maxBufferSize);
				byte[] testBuffer = new byte[bufferSize + safetyMarginSizeInBytes];

				//Fill buffer
				byte fillByte = testPattern[Random.Shared.Next(minValue: 0, maxValue: patternSize)];
				Array.Fill<byte>(testBuffer, fillByte, 0, bufferSize);							//fill buffer with fill character
				Array.Fill<byte>(testBuffer, 0, bufferSize, safetyMarginSizeInBytes);	//clear buffer after the bufferSize boundary to the end (end == bufferSize + patternSize)
				Array.Copy(testPattern, 0, testBuffer, bufferSize, patternSize);  //only for BackwardFast: copy pattern after the search buffer end


				int lastOffset = bufferSize - patternSize;
				const int maxTestPatterns = int.MaxValue; //10000; // maximal amount of matching byte sequences, which will be distributed randomly over the buffer
				//generatedOffsets list contains offsets where pattern was randomly inserted
				List<int> generatedOffsets = new List<int>();
				int generatedOffset = 0;
				int minDistance = 0;
				int maxDistance = 1024; //65536;	// bufferSize / patternSize;	//4096
				for (int i = 0; i < maxTestPatterns && generatedOffset + patternSize <= lastOffset; ++i)
				{
					int distance = Random.Shared.Next(minDistance, maxDistance);
					int offset = Random.Shared.Next( generatedOffset, Math.Min(	generatedOffset + distance, lastOffset	) );
					generatedOffset = offset + patternSize;	//set next generated offset's start boundary
					generatedOffsets.Add(offset);
					testPattern.CopyTo(testBuffer, offset);
					//Trace.WriteLine($"Generator: inserting at {offset.ToString().PadLeft(16)}, distance is {distance.ToString().PadLeft(16)}");
				}
				Trace.WriteLine($"Generator: fillByte:0x{fillByte:X2}, patternSize:{patternSize,6}, bufferSize:{bufferSize,16:###,###,###,###}, offsetCount={generatedOffsets.Count}");

				List<int> referenceOffsets = new List<int>();
				referenceSearch.Init(testPattern, (int offset, Type caller) => { referenceOffsets.Add(offset); return true; });
				referenceSearch.Search(testBuffer, 0, bufferSize);
				//referenceOffsets.Sort();
/*
				//Compare generated vs. reference search gathered offsets
				//NOTE: this check should be skipped, if overlapping test strings are used
				if(!AssertEqualOffsets("generated", generatedOffsets, "reference", referenceOffsets))
				{
					Assert.Fail($"{nameof(generatedOffsets)} (Count={generatedOffsets.Count}) not equal to {nameof(referenceOffsets)} (Count={referenceOffsets.Count})");
				}
*/
				Stopwatch initWatch = new();
				Stopwatch searchWatch = new();

				//Blacklists
				HashSet<Type> blacklistedTypes = new HashSet<Type>()
				{
					typeof(Search.Common.SearchBase)					//Skip SearchBase itself (TODO?: use IsInherited)
				};
				List<Type> blacklistedAttributes = new List<Type>()
				{
					typeof(Search.Attributes.UnstableAttribute),
					//typeof(Search.Attributes.ExperimentalAttribute),
					typeof(Search.Attributes.SlowAttribute),
				};

				foreach(Type type in ((TypeInfo[])assembly.DefinedTypes).Select(t => t.UnderlyingSystemType))
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
				}

				Type[] searchTypes = statistics.Keys.Select(x => x).OrderBy(x => x.FullName, StringComparer.Ordinal).ToArray();

				//Automatically instantiate ISearch derivates with Type and custom Attribute filtering
				foreach (Type type in searchTypes)
				{
					//Create instance of generic search algorithm exposing ISearch interface
					Assembly searchAssembly = type.Assembly;
					ISearch genericSearch = (ISearch)(searchAssembly.CreateInstance(type.FullName!, false) ?? throw new ApplicationException(type.FullName));

					//Accumulate duration of initialization for each generic search algorithm
					initWatch.Restart();

					//Lambda inline function advises search algorithm implementation what is the search pattern and the delegate whic receives the offset when pattern is found.
					genericSearch.Init(testPattern, (int offset, Type caller) => { statistics[caller].Offsets.Add(offset); return true; });

					//Stop the timer, and increment initialization time statistics
					initWatch.Stop();
					_ = statistics[type].IncrementInitializationTime(initWatch.Elapsed.Ticks);

					//Accumulate duration of search for each generic search algorithm
					searchWatch.Restart();
					try
					{
						genericSearch.Search(testBuffer, 0, bufferSize);
					}
					catch (Exception ex)
					{
						Assert.Fail($"[SEARCH EXCEPTION] Type={type}, Details:{ex}", ex);
					}
					searchWatch.Stop();
					_ = statistics[type].IncrementSearchTime(searchWatch.Elapsed.Ticks);

					//Check if offsets of the found pattern returned by generic algorithm is equal to reference offsets of the pattern
					if (!type.Equals(referenceSearchType))
					{
						if (!AssertEqualOffsets("generic", statistics[type].Offsets, "reference", referenceOffsets))
						{
							Assert.Fail($"generic (Count={statistics[type].Offsets.Count}) not equal to {nameof(referenceOffsets)} (Count={referenceOffsets.Count})");
						}
					}
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
			double referenceTime = statistics[referenceSearchType].TotalMilliseconds;
			const string stringRef = @" [Ref]";
			//Write test report, ordered by Total time (Initialization + Search)
			foreach (Type type in statistics.Keys.OrderBy(t => statistics[t].InitTime + statistics[t].SearchTime))
			{
				SearchStatistics stats = statistics[type];

				List<string> statColumn = new List<string>();

				if(type.Equals(referenceSearchType))
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

			//Statistics for each ISearch instance
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

				Trace.WriteLine(string.Join(verticalSeparator, statStrings.Select(v => string.Concat(v, "")).ToArray()));
			}

			Trace.WriteLine(lightDivider);

			//Display cumulative timings
			string grand = string.Join(System.Environment.NewLine, new string[]
			{
				$"Grand Init   {grandInit,16:###,###,##0.000} ms",
				$"Grand Search {grandSearch,16:###,###,##0.000} ms",
				$"Grand Total  {grandTotal,16:###,###,##0.000} ms"
			});
			Trace.WriteLine(grand);

			Trace.WriteLine(heavyDivider);
		}

	};  //END: class SearchUnitTests
};

