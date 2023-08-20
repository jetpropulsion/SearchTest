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
	using System.Xml.Linq;

	[TestClass]
	public class SearchUnitTests
	{
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

		//Method will terminate execution if offset collections are different
		public static bool EqualOffsets(string firstName, ICollection<int> first, string secondName, ICollection<int> second)
		{
			if (first.Count == second.Count && first.SequenceEqual(second))
			{
				return true;
			}

			IEnumerable<int> intersect = first.Intersect(second);
			IEnumerable<int> reference = first.Except(intersect);
			IEnumerable<int> current = second.Except(intersect);

			for (int i = 0; i < reference.Count(); ++i)
			{
				Trace.WriteLine($"{firstName} only at position {i}: {reference.ElementAt(i)}");
			}
			for (int i = 0; i < current.Count(); ++i)
			{
				Trace.WriteLine($"{secondName} only at position {i}: {current.ElementAt(i)}");
			}
			return false;
		}


		public List<SearchTestParams> SearchTests = new List<SearchTestParams>()
		{
			new SearchTestParams
			(
				name: "BackgroundIsPatternMinus1",
				maxTestIterations: 5,
				minPatternSize: 3,
				maxPatternSize: 273,
				minBufferSize: 1048576 * 16,
				maxBufferSize: 1048576 * 16 * 24,
				minDistance: 1,
				maxDistance: 273,
				bufferPatternFill: SearchTestParams.PatternMinusOneBufferPatternFill
			),
			new SearchTestParams
			(
				name: "StandardDistanceAndPattern",
				maxTestIterations: 5,
				minPatternSize: 3,
				maxPatternSize: 273,
				minBufferSize: 1048576 * 16,
				maxBufferSize: 1048576 * 16 * 24,
				minDistance: 256,
				maxDistance: 65536
			),
			new SearchTestParams
			(
				name: "SmallDistanceSmallPattern",
				maxTestIterations: 5,
				minPatternSize: 3,
				maxPatternSize: 10,
				minBufferSize: 1048576 * 16,
				maxBufferSize: 1048576 * 16 * 24,
				minDistance: 2,
				maxDistance: 127
			)
		};

		public void WriteStats(Dictionary<Type, SearchStatistics> statistics, Type referenceSearchType, double total)
		{
			double referenceTime = statistics[referenceSearchType].TotalMilliseconds;
			const string stringRef = @" [Ref]";
			int maxName = statistics.Keys.Select(t => t.FullName!.Length).Max();

			List<string[]> statColumns = new List<string[]>();

			//statColumns.Clear();

			//Write test report, ordered by Total time (Initialization + Search)
			foreach (Type type in statistics.Keys.OrderBy(t => statistics[t].InitTime + statistics[t].SearchTime))
			{
				SearchStatistics stats = statistics[type];

				List<string> statColumn = new List<string>();

				if (type.Equals(referenceSearchType))
				{ statColumn.Add(string.Concat(type.FullName!, stringRef).PadRight(maxName + stringRef.Length)); }
				else
				{ statColumn.Add(type.FullName!.PadRight(maxName + stringRef.Length)); }

				statColumn.Add($"{stats.InitMilliseconds:##0.000}");
				statColumn.Add($"{stats.InitMilliseconds * 100.0 / total:##0.00}");
				statColumn.Add($"{stats.SearchMilliseconds:##0.000}");
				statColumn.Add($"{stats.SearchMilliseconds * 100.0 / total:##0.00}");
				statColumn.Add($"{stats.TotalMilliseconds:##0.000}");
				statColumn.Add($"{stats.TotalMilliseconds * 100.0 / total:##0.00}");
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
				for (int j = 0; j < statColumns[i].Length; ++j)
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

				Trace.WriteLine
				(
					string.Join
					(
						string.Concat(" ", stringLightVerticalLine, " "),
						statStrings.Select(v => string.Concat(v, "")).ToArray()
					)
				);
			}
		}

		[TestMethod]
		[Timeout(86400 * 365)]    //Test timeout is one year
		public void Test_All_ISearch_Derivates()
		{
			Trace.AutoFlush = true;

			Assembly assembly = typeof(Search.Interfaces.ISearch).Assembly;
			ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));

			Trace.WriteLine(heavyDivider);
			Trace.WriteLine(lightDivider);


			//Reference search algorithm is now BruteForce, choosen over its simplicity (and slowness)
			ISearch referenceSearch = new Search.Algorithms.BruteForce();
			Type referenceSearchType = referenceSearch.GetType();

			//Dictionary of Type (which must inherit from ISearch) and its measured searcj statistics
			Dictionary<Type, SearchStatistics> statistics = new();
			Dictionary<Type, SearchStatistics[]> detailedStatistics = new();

			//Blacklists
			HashSet<Type> blacklistedTypes = new HashSet<Type>()
			{
				typeof(Search.Common.SearchBase)
			};
			List<Type> blacklistedAttributes = new List<Type>()
			{
				typeof(Search.Attributes.UnstableAttribute),
				//typeof(Search.Attributes.ExperimentalAttribute),
				typeof(Search.Attributes.SlowAttribute),
			};
			foreach (Type type in ((TypeInfo[])assembly.DefinedTypes).Select(t => t.UnderlyingSystemType))
			{
				if
				(
					!type.IsClass                                                           //Skip if type is not a class
					|| type.IsAbstract                                                      //Skip if type is an abstract class
					|| !type.GetInterfaces().Contains(typeof(Search.Interfaces.ISearch))    //Skip if type doesn't implement ISearch
					|| blacklistedTypes.Contains(type)                                      //Skip blacklisted types, like base classes
					|| blacklistedAttributes.Any(x => type.IsDefined(x))                //Skip search algorithm class if marked with blacklisted Attribute
				)
				{
					continue;
				}
				//If statistics context doesn't contain SearchStatistics object, this is the first occurence, add new one
				if (!statistics.ContainsKey(type))
				{
					statistics.Add(type, new SearchStatistics());
				}
				if (!detailedStatistics.ContainsKey(type))
				{
					detailedStatistics.Add(type, new SearchStatistics[SearchTests.Count]);
					for(int i = 0; i < detailedStatistics[type].Length; ++i)
					{
						detailedStatistics[type][i] = new SearchStatistics();
					}
				}
			}

			Type[] searchTypes = statistics.Keys.Select(x => x).OrderBy(x => x.FullName, StringComparer.Ordinal).ToArray();

			for (int testIteration = 0; testIteration < SearchTests.Count; ++testIteration)
			{
				SearchTestParams test = SearchTests[testIteration];
				string testName = test.Name;

				for (int testSubIteration = 1; testSubIteration <= test.MaxIterations; ++testSubIteration)
				{
					test.Reset();

					ReadOnlyMemory<byte> testPattern = test.Pattern;
					ReadOnlyMemory<byte> testBuffer = test.Buffer;
					int patternSize = test.PatternSize;
					int bufferSize = test.BufferSize;

					List<int> referenceOffsets = new List<int>();
					referenceSearch.Init(testPattern, (int offset, Type caller) => { referenceOffsets.Add(offset); return true; });
					referenceSearch.Search(testBuffer, 0, bufferSize);

					Trace.WriteLine
					(
						$"{test.Name.PadRight(30)}#{testSubIteration}/{test.MaxIterations,-3}:"
						+ $" {nameof(patternSize)}={patternSize,16}"
						+ $", {nameof(bufferSize)}={bufferSize,16}"
						+ $", {nameof(referenceOffsets)}={referenceOffsets.Count,16}"
						+ $" @ {DateTime.Now.ToString(@"yyyy-MM-dd HH:mm:ss.ffffzzz")}"
					);

					/*
					//Compare generated vs. reference search gathered offsets
					//NOTE: this check should be skipped, if overlapping test strings or randomly generated sequences are used
					if(!EqualOffsets("generated", generatedOffsets, "reference", referenceOffsets))
					{
						Assert.Fail($"{nameof(generatedOffsets)} (Count={generatedOffsets.Count}) not equal to {nameof(referenceOffsets)} (Count={referenceOffsets.Count})");
					}
					*/

					Stopwatch initWatch = new();
					Stopwatch searchWatch = new();

					//Clear previous offsets and counters only. Keep accumulated timings.
					statistics.Keys.ToList().ForEach(k => statistics[k].Offsets.Clear());

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
						long elapsedInit = initWatch.Elapsed.Ticks;
						_ = statistics[type].IncrementInitializationTime(elapsedInit);
						_ = detailedStatistics[type][testIteration].IncrementInitializationTime(elapsedInit);

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
						long elapsedSearch = searchWatch.Elapsed.Ticks;
						_ = statistics[type].IncrementSearchTime(elapsedSearch);
						_ = detailedStatistics[type][testIteration].IncrementSearchTime(elapsedSearch);

						//Check if offsets of the found pattern returned by generic algorithm is equal to reference offsets of the pattern
						if (!type.Equals(referenceSearchType))
						{
							if (!EqualOffsets("generic", statistics[type].Offsets, "reference", referenceOffsets))
							{
								Assert.Fail($"generic (Count={statistics[type].Offsets.Count}) not equal to {nameof(referenceOffsets)} (Count={referenceOffsets.Count})");
							}
						}
						//Trace.WriteLine($"{type.FullName}: Init={initWatch.Elapsed.Ticks}, Search={searchWatch.Elapsed.Ticks}");
					} //END: foreach (Type type in searchTypes)
				} //END: for (int testSubIteration = 1; testSubIteration <= test.MaxIterations; ++testSubIteration)


				Dictionary<Type, SearchStatistics> detailedStatisticsMap = new Dictionary<Type, SearchStatistics>();
				foreach (Type type in searchTypes)
				{
					if(!detailedStatisticsMap.ContainsKey(type))
					{
						detailedStatisticsMap.Add(type, new SearchStatistics());
					}
					detailedStatisticsMap[type].InitTime += detailedStatistics[type][testIteration].InitTime;
					detailedStatisticsMap[type].SearchTime += detailedStatistics[type][testIteration].SearchTime;
				}
				IEnumerable<StatTimes> subList = detailedStatisticsMap.Values.Select(value => new StatTimes(value.InitTime, value.SearchTime, value.InitTime + value.SearchTime));
				StatTimes subTotals = subList.Aggregate((a, b) => new StatTimes(a.InitTime + b.InitTime, a.SearchTime + b.SearchTime, a.TotalTime + b.TotalTime));

				double subInit = TimeSpan.FromTicks(subTotals.InitTime).TotalMilliseconds;
				double subSearch = TimeSpan.FromTicks(subTotals.SearchTime).TotalMilliseconds;
				double subTotal = TimeSpan.FromTicks(subTotals.InitTime + subTotals.SearchTime).TotalMilliseconds;
				WriteStats(detailedStatisticsMap, referenceSearchType, subTotal);

			} //END: for (int testIteration = 0; testIteration < SearchTests.Count; ++testIteration)

			Trace.WriteLine(heavyDivider);
			//Trace.WriteLine(lightDivider);

			IEnumerable<StatTimes> totalsList = statistics.Values.Select(value => new StatTimes(value.InitTime, value.SearchTime, value.InitTime + value.SearchTime));
			StatTimes grandTotals = totalsList.Aggregate((a, b) => new StatTimes(a.InitTime + b.InitTime, a.SearchTime + b.SearchTime, a.TotalTime + b.TotalTime));

			double grandInit = TimeSpan.FromTicks(grandTotals.InitTime).TotalMilliseconds;
			double grandSearch = TimeSpan.FromTicks(grandTotals.SearchTime).TotalMilliseconds;
			double grandTotal = TimeSpan.FromTicks(grandTotals.InitTime + grandTotals.SearchTime).TotalMilliseconds;

			WriteStats(statistics, referenceSearchType, grandTotal);
			Trace.WriteLine(heavyDivider);
			//Trace.WriteLine(lightDivider);

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

