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
	using System.Runtime.InteropServices;
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
		*** Remove the assembly firstOnly to Microsoft.VisualStudio.QualityTools.UnitTestFramework from your unit test project.
		*** Add NuGet package references to MSTestV2 including the MSTest.TestFramework and the MSTest.TestAdapter packages on nuget.org. 
		*** You can install packages in the NuGet Package Manager Console with the following commands:
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

#if DEBUG
		const int DefaultMaxIterations = 1;
#else
		const int DefaultMaxIterations = 1;
#endif
		const int MinSmallPattern = 3;
		const int MaxSmallPattern = 16;
		const int MinStandardPattern = 4;
		const int MaxStandardPattern = 273;
		const int MinLargePattern = 1024;
		const int MaxLargePattern = 4096;
		const int MinStandardBuffer = 1048576;
		const int MaxStandardBuffer = 1048576 * 2;
		const int MinLargeBuffer = 1048576 * 16;
		const int MaxLargeBuffer = 1048576 * 16 * 24;
		const int MinLargeDistance = 8192;
		const int MaxLargeDistance = 65536 * 2;
		const int MinSmallDistance = 0;
		const int MaxSmallDistance = 32;
		const int MinStandardDistance = 64;
		const int MaxStandardDistance = 4096;

		public List<SearchTestParams> SearchTests = new List<SearchTestParams>()
		{
			new SearchTestParams
			(
				name: "Standard Distance, Standard Pattern, Standard Buffer",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinStandardPattern,
				maxPatternSize: MaxStandardPattern,
				minBufferSize: MinStandardBuffer,
				maxBufferSize: MaxStandardBuffer,
				minDistance: MinStandardDistance,
				maxDistance: MaxStandardDistance
			),
			new SearchTestParams
			(
				name: "Standard Distance, Standard Pattern, Standard Buffer, Buffer is Pattern minus 1 (end byte)",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinStandardPattern,
				maxPatternSize: MaxStandardPattern,
				minBufferSize: MinStandardBuffer,
				maxBufferSize: MaxStandardBuffer,
				minDistance: MinStandardDistance,
				maxDistance: MaxStandardDistance,
				bufferPatternFill: SearchTestParams.PatternMinusOneBufferPatternFill
			),
			new SearchTestParams
			(
				name: "Small Distance, Standard Pattern, Standard Buffer, Buffer is Pattern minus 1 (end byte)",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinStandardPattern,
				maxPatternSize: MaxStandardPattern,
				minBufferSize: MinStandardBuffer,
				maxBufferSize: MaxStandardBuffer,
				minDistance: MinSmallDistance,
				maxDistance: MaxSmallDistance,
				bufferPatternFill: SearchTestParams.PatternMinusOneBufferPatternFill
			),
			new SearchTestParams
			(
				name: "Small Distance, Small Pattern, Standard Buffer, Buffer is Pattern minus 1 (end byte)",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinSmallPattern,
				maxPatternSize: MaxSmallPattern,
				minBufferSize: MinStandardBuffer,
				maxBufferSize: MaxStandardBuffer,
				minDistance: MinSmallDistance,
				maxDistance: MaxSmallDistance,
				bufferPatternFill: SearchTestParams.PatternMinusOneBufferPatternFill
			),
			new SearchTestParams
			(
				name: "Standard Distance, Standard Pattern, Standard Buffer, Non-Pattern Byte Buffer Fill (use Pattern shorter than 256)",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinSmallPattern,
				maxPatternSize: MaxSmallPattern,
				minBufferSize: MinStandardBuffer,
				maxBufferSize: MaxStandardBuffer,
				minDistance: MinStandardDistance,
				maxDistance: MaxStandardDistance,
				bufferPatternFill: SearchTestParams.NonPatternByteBufferPatternFill
			),
			new SearchTestParams
			(
				name: "Small Distance, Small Pattern, Standard Buffer",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinSmallPattern,
				maxPatternSize: MaxSmallPattern,
				minBufferSize: MinStandardBuffer,
				maxBufferSize: MaxStandardBuffer,
				minDistance: MinSmallDistance,
				maxDistance: MaxSmallDistance
			),
			new SearchTestParams
			(
				name: "Small Distance, Small Pattern, Standard Buffer, Non Pattern Byte Buffer Pattern Fill (use Pattern shorter than 256)",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinSmallPattern,
				maxPatternSize: MaxSmallPattern,
				minBufferSize: MinStandardBuffer,
				maxBufferSize: MaxStandardBuffer,
				minDistance: MinSmallDistance,
				maxDistance: MaxSmallDistance,
				bufferPatternFill: SearchTestParams.NonPatternByteBufferPatternFill
			),
			new SearchTestParams
			(
				name: "Small Distance, Large Pattern, Large Buffer",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinLargePattern,
				maxPatternSize: MaxLargePattern,
				minBufferSize: MinLargeBuffer,
				maxBufferSize: MaxLargeBuffer,
				minDistance: MinSmallDistance,
				maxDistance: MaxSmallDistance
			),
			new SearchTestParams
			(
				name: "Small Distance, Large Pattern, Large Buffer, Random Pattern Segment Fill",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinLargePattern,
				maxPatternSize: MaxLargePattern,
				minBufferSize: MinLargeBuffer,
				maxBufferSize: MaxLargeBuffer,
				minDistance: MinSmallDistance,
				maxDistance: MaxSmallDistance,
				bufferPatternFill: SearchTestParams.RandomPatternSegmentBufferPatternFill
			),
			new SearchTestParams
			(
				name: "Large Distance, Small Pattern, Large Buffer, Random Pattern Segment Fill",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinSmallPattern,
				maxPatternSize: MaxSmallPattern,
				minBufferSize: MinLargeBuffer,
				maxBufferSize: MaxLargeBuffer,
				minDistance: MinLargeDistance,
				maxDistance: MaxLargeDistance,
				bufferPatternFill: SearchTestParams.RandomPatternSegmentBufferPatternFill
			),
			new SearchTestParams
			(
				name: "Large Distance, Small Pattern, Large Buffer, Random Pattern Segment Fill",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinSmallPattern,
				maxPatternSize: MaxSmallPattern,
				minBufferSize: MinLargeBuffer,
				maxBufferSize: MaxLargeBuffer,
				minDistance: MinLargeDistance,
				maxDistance: MaxLargeDistance,
				bufferPatternFill: SearchTestParams.RandomPatternSegmentBufferPatternFill
			),
			new SearchTestParams
			(
				name: "Large Distance, Large Pattern, Large Buffer, Random Pattern Segment Fill",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinLargePattern,
				maxPatternSize: MaxLargePattern,
				minBufferSize: MinLargeBuffer,
				maxBufferSize: MaxLargeBuffer,
				minDistance: MinLargeDistance,
				maxDistance: MaxLargeDistance,
				bufferPatternFill: SearchTestParams.RandomPatternSegmentBufferPatternFill
			),
			new SearchTestParams
			(
				name: "Large Distance, Large Pattern, Large Buffer",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinLargePattern,
				maxPatternSize: MaxLargePattern,
				minBufferSize: MinLargeBuffer,
				maxBufferSize: MaxLargeBuffer,
				minDistance: MinLargeDistance,
				maxDistance: MaxLargeDistance
			),
			new SearchTestParams
			(
				name: "Large Distance, Large Pattern, Large Buffer, Random Pattern Segment Fill",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinLargePattern,
				maxPatternSize: MaxLargePattern,
				minBufferSize: MinLargeBuffer,
				maxBufferSize: MaxLargeBuffer,
				minDistance: MinLargeDistance,
				maxDistance: MaxLargeDistance,
				bufferPatternFill: SearchTestParams.RandomPatternSegmentBufferPatternFill
			)
		};

		public void WriteStats(IReadOnlyDictionary<Type, SearchStatistics> statistics, Type referenceSearchType, SearchStatistics total)
		{
			double referenceTime = statistics[referenceSearchType].TotalMilliseconds;
			const string stringRef = @" [Ref]";
			int maxName = statistics.Keys.Max(t => t.Name!.Length);

			List<string[]> statColumns = new List<string[]>();

			//Write test report, ordered by Total time (Initialization + Search)
			foreach (Type type in statistics.Keys.OrderBy(t => statistics[t].InitTime + statistics[t].SearchTime))
			{
				SearchStatistics stats = statistics[type];

				List<string> statColumn = new List<string>();

				if (type.Equals(referenceSearchType))
				{
					statColumn.Add(string.Concat(type.Name!, stringRef).PadRight(maxName + stringRef.Length));
				}
				else
				{
					statColumn.Add(type.Name!.PadRight(maxName + stringRef.Length));
				}

				statColumn.Add($"{stats.InitMilliseconds:##0.000}");
				statColumn.Add($"{stats.InitMilliseconds * 100.0 / total.TotalMilliseconds:##0.00}");
				statColumn.Add($"{stats.SearchMilliseconds:##0.000}");
				statColumn.Add($"{stats.SearchMilliseconds * 100.0 / total.TotalMilliseconds:##0.00}");
				statColumn.Add($"{stats.TotalMilliseconds:##0.000}");
				statColumn.Add($"{stats.TotalMilliseconds * 100.0 / total.TotalMilliseconds:##0.00}");
				statColumn.Add($"{stats.TotalMilliseconds * 100.0 / referenceTime:##0.00}");

				statColumns.Add(statColumn.ToArray());
			}

			//Calculate maximum display column length
			int[] maxColumnLengths = Enumerable.Repeat<int>(0, statColumns[0].Length).ToArray();
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
				string[] c = statColumns[i];

				int j = 0;
				string[] statStrings =
				{
						c[j++],
						$"Init {c[j++]} ms ({c[j++]}%)",
						$"Search {c[j++]} ms ({c[j++]}%)",
						$"Total {c[j++]} ms ({c[j++]}%)",
						$"Ref.% {c[j++]}"
					};

				Trace.WriteLine
				(
					string.Join
					(
						string.Concat(" ", stringLightVerticalLine, " "),
						statStrings
							.Select(v => string.Concat(v, ""))
							.ToArray()
					)
				);
			}
		}

		public void AggregateStatistics(IReadOnlyCollection<Type> searchTypes, IReadOnlyDictionary<Type, SearchStatistics[]> statistics, int testStart, int testCount, out Dictionary<Type, SearchStatistics> aggregatedTypes, out SearchStatistics aggregated)
		{
			aggregatedTypes = new Dictionary<Type, SearchStatistics>();
			foreach (Type type in searchTypes)
			{
				IReadOnlyCollection<SearchStatistics> statistic = statistics[type];
				for (int i = testStart; i < testStart + testCount; ++i)
				{
					long initTime = statistics[type][i].InitTime;
					long searchTime = statistics[type][i].SearchTime;
					if (!aggregatedTypes.ContainsKey(type))
					{
						aggregatedTypes[type] = new SearchStatistics(initTime, searchTime);
					}
					else
					{
						aggregatedTypes[type].IncrementInitializationTime(initTime);
						aggregatedTypes[type].IncrementSearchTime(searchTime);
					}
				}
			}

			aggregated = aggregatedTypes.Values.Aggregate((a, b) => new SearchStatistics(a.InitTime + b.InitTime, a.SearchTime + b.SearchTime));
		}

		//Method will terminate execution if offset collections are different
		public static bool EqualOffsets(string firstName, IReadOnlyCollection<int> first, string secondName, IReadOnlyCollection<int> second)
		{
			if (first.Count == second.Count && first.SequenceEqual(second))
			{
				return true;
			}

			IEnumerable<int> intersect = first.Intersect(second);
			IEnumerable<int> firstOnly = first.Except(intersect);
			IEnumerable<int> secondOnly = second.Except(intersect);

			Trace.WriteLine($"{firstName} count: {firstOnly.Count()}");
			Trace.WriteLine($"{secondName} count: {secondOnly.Count()}");
#if TRACE
			for (int i = 0; i < firstOnly.Count(); ++i)
			{
				Trace.WriteLine($"{firstName} only at position {i}: {firstOnly.ElementAt(i)}");
			}
			for (int i = 0; i < secondOnly.Count(); ++i)
			{
				Trace.WriteLine($"{secondName} only at position {i}: {secondOnly.ElementAt(i)}");
			}
#endif
			return false;
		}


		[TestMethod]
		[Timeout(86400 * 365)]    //Test timeout is one year
		
		public void TestAllDerivates()
		{
			Trace.AutoFlush = true;

			Assembly assembly = typeof(Search.Interfaces.ISearch).Assembly;
			ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));

			Trace.WriteLine(heavyDivider);

			//Reference search algorithm is now BruteForce, choosen over its simplicity (and slowness)
			ISearch referenceSearch = new Search.Algorithms.Naive();	//Raita(); //NotSoNaive();	// BruteForce();
			Type referenceSearchType = referenceSearch.GetType();

			//Dictionary of Type (which must inherit from ISearch) and its measured searcj statistics
			Dictionary<Type, SearchStatistics[]> statistics = new();

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
					|| !type.GetInterfaces().Contains(typeof(Search.Interfaces.ISearch))    //Skip if type doesn't implement ISearch
					|| type.IsAbstract                                                      //Skip if type is an abstract class
					|| blacklistedTypes.Contains(type)                                      //Skip blacklisted types, like base classes
					|| blacklistedAttributes.Any(x => type.IsDefined(x))                //Skip search algorithm class if marked with blacklisted Attribute
				)
				{
					continue;
				}
				//If statistics context doesn't contain SearchStatistics object, this is the first occurence, add new one
				if (!statistics.ContainsKey(type))
				{
					statistics.Add(type, new SearchStatistics[SearchTests.Count]);
					for (int i = 0; i < statistics[type].Length; ++i)
					{
						statistics[type][i] = new SearchStatistics();
					}
				}
			}

			Type[] searchTypes = statistics.Keys.Select(x => x).OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();

			for (int testIteration = 0; testIteration < SearchTests.Count; ++testIteration)
			{
				SearchTestParams test = SearchTests[testIteration];
				string testName = test.Name;

				string[] testInfo = new string[]
				{
						$"{nameof(test.MinPatternSize)}={test.MinPatternSize}"
						, $"{nameof(test.MaxPatternSize)}={test.MaxPatternSize}"
						, $"{nameof(test.MinBufferSize)}={test.MinBufferSize}"
						, $"{nameof(test.MaxBufferSize)}={test.MaxBufferSize}"
						, $"{nameof(test.MinDistance)}={test.MinDistance}"
						, $"{nameof(test.MaxDistance)}={test.MaxDistance}"
						, $"{nameof(test.MaxIterations)}={test.MaxIterations}"
				};
				Trace.WriteLine(string.Concat($"### {testName} ###", System.Environment.NewLine, string.Join(@", ", testInfo)));

				for (int testSubIteration = 0; testSubIteration < test.MaxIterations; ++testSubIteration)
				{
					//Call to Reset() will cause PatternLocation and Buffer members of SearchTestParams class to be re-created
					test.Reset();

					ReadOnlyMemory<byte> testPattern = test.Pattern;
					Memory<byte> testBuffer = test.Buffer;
					int patternSize = testPattern.Length;
					int bufferSize = testBuffer.Length;

					List<int> referenceOffsets = new List<int>();
					referenceSearch.Init
					(
						testPattern,
						(int offset, Type caller) =>
						{
							referenceOffsets.Add(offset);
							return true;
						}
					);
					referenceSearch.FixSearchBuffer(ref testBuffer, bufferSize, testPattern);
					referenceSearch.Search(testBuffer, 0, bufferSize);
					//Assert.IsTrue(referenceOffsets.Count > 0);
					if(referenceOffsets.Count <= 0)
					{
						throw new Exception($"{nameof(referenceOffsets)}.Count == 0 (Initial search)");
					}

					string[] testIterationInfo = new string[]
					{
						$"{DateTime.Now.ToString(@"yyyy-MM-dd HH:mm:ss.ffffzzz")}"
						, $"#{(testSubIteration + 1)}/{test.MaxIterations}"
						, $"{nameof(test.PatternSize)}={test.PatternSize}"
						, $"{nameof(test.BufferSize)}={test.BufferSize}"
						, $"{nameof(referenceOffsets)}={referenceOffsets.Count}"
					};
					Trace.WriteLine(string.Join(@", ", testIterationInfo));

					//Compare generated vs. firstOnly search gathered offsets
					//NOTE: this check should be skipped, if overlapping test strings or randomly generated sequences are used
					//if (!EqualOffsets("generated", test.Offsets, "reference", referenceOffsets))
					//{
					//	Assert.Fail($"{nameof(test.Offsets)} (Count={test.Offsets.Length}) not equal to {nameof(referenceOffsets)} (Count={referenceOffsets.Count})");
					//}


					Stopwatch initWatch = new();
					Stopwatch searchWatch = new();

					//Clear previous offsets and counters only. Keep accumulated timings.
					statistics.Keys.ToList().ForEach(k => statistics[k][testIteration].Offsets.Clear());

					//Automatically instantiate ISearch derivates with Type and custom Attribute filtering
					foreach (Type type in searchTypes)
					{
						if (referenceOffsets.Count <= 0)
						{
							throw new Exception($"{nameof(referenceOffsets)}.Count == 0 (inside foreach (Type type in searchTypes))");
						}

						//Create instance of generic search algorithm exposing ISearch interface
						Assembly searchAssembly = type.Assembly;
						ISearch genericSearch = (ISearch)(searchAssembly.CreateInstance(type.FullName!, false) ?? throw new ApplicationException(type.FullName));

						//Accumulate duration of initialization for each generic search algorithm
						initWatch.Restart();

						//Lambda inline function advises search algorithm implementation what is the search pattern and the delegate whic receives the offset when pattern is found.
						genericSearch.Init
						(
							testPattern,
							(int offset, Type caller) =>
							{
								statistics[caller][testIteration].Offsets.Add(offset);
								return true;
							}
						);

						//Stop the timer, and increment initialization time statistics
						initWatch.Stop();
						_ = statistics[type][testIteration].IncrementInitializationTime(initWatch.Elapsed.Ticks);

						//Accumulate duration of search for each generic search algorithm
						try
						{
							searchWatch.Restart();
							genericSearch.FixSearchBuffer(ref testBuffer, bufferSize, testPattern);
							genericSearch.Search(testBuffer, 0, bufferSize);
						}
						catch (Exception ex)
						{
							Assert.Fail($"[SEARCH EXCEPTION] Type={type}, Details:{ex}", ex);
						}
						searchWatch.Stop();
						_ = statistics[type][testIteration].IncrementSearchTime(searchWatch.Elapsed.Ticks);

						//Check if offsets of the found pattern returned by generic algorithm is equal to firstOnly offsets of the pattern
						if (!type.Equals(referenceSearchType))
						{
							if (!EqualOffsets(type.FullName!, statistics[type][testIteration].Offsets, referenceSearchType.FullName!, referenceOffsets))
							{
								Assert.Fail($"{type.FullName} (Count={statistics[type][testIteration].Offsets.Count}) not equal to {nameof(referenceOffsets)} (Count={referenceOffsets.Count})");
							}
						}
					} //END: foreach (Type type in searchTypes)

				} //END: for (int testSubIteration = 1; testSubIteration <= test.MaxIterations; ++testSubIteration)
				Trace.WriteLine(lightDivider);			//"####"

				Dictionary<Type, SearchStatistics> subTotalTypes;
				SearchStatistics subTotal;
				AggregateStatistics(searchTypes, statistics, testIteration, 1, out subTotalTypes, out subTotal);
				WriteStats(subTotalTypes, referenceSearchType, subTotal);
				Trace.WriteLine(lightDivider);      //"#@@#"

			} //END: for (int testIteration = 0; testIteration < SearchTests.Count; ++testIteration)

			Trace.WriteLine(heavyDivider);
			Trace.WriteLine(heavyDivider);

			Dictionary<Type, SearchStatistics> totalTypes;
			SearchStatistics totalTotal;
			AggregateStatistics(searchTypes, statistics, testStart: 0, SearchTests.Count, out totalTypes, out totalTotal);
			WriteStats(totalTypes, referenceSearchType, totalTotal);

			Trace.WriteLine(lightDivider);

			//Display cumulative timings
			string detailed = string.Join(System.Environment.NewLine, new string[]
			{
				$"Global Init   {totalTotal.InitMilliseconds,16:###,###,##0.000} ms",
				$"Global Search {totalTotal.SearchMilliseconds,16:###,###,##0.000} ms",
				$"Global Total  {totalTotal.TotalMilliseconds,16:###,###,##0.000} ms"
			});
			Trace.WriteLine(detailed);

			Trace.WriteLine(heavyDivider);
		}

	};  //END: class SearchUnitTests
};

