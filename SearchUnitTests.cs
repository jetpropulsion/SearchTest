using System.Drawing;

namespace SearchTest
{
	using Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns;
	using Microsoft.VisualStudio.TestPlatform.CrossPlatEngine;
	using Microsoft.VisualStudio.TestPlatform.ObjectModel;
	using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;

	using Search.Interfaces;

	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Xml.Linq;
	using static SearchTest.SearchTestParams;

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

		public class SmallPattern : SearchTestParams.PatternDefinition { public SmallPattern() : base(nameof(SmallPattern), 3, 16) { } };
		public class StandardPattern : SearchTestParams.PatternDefinition { public StandardPattern() : base(nameof(StandardPattern), 4, 273) { } };
		public class LargePattern : SearchTestParams.PatternDefinition { public LargePattern() : base(nameof(LargePattern), 1024, 4096) { } };

		public class SmallBuffer : SearchTestParams.BufferDefinition { public SmallBuffer() : base(nameof(SmallBuffer), 1048576, 1048576 * 2) { } };
		public class StandardBuffer : SearchTestParams.BufferDefinition { public StandardBuffer() : base(nameof(StandardBuffer), 1048576 * 4, 1048576 * 16) { } };
		public class LargeBuffer : SearchTestParams.BufferDefinition { public LargeBuffer() : base(nameof(LargeBuffer), 1048576 * 16, 1048576 * 24) { } };

		public class SmallDistance : SearchTestParams.DistanceDefinition { public SmallDistance() : base(nameof(SmallDistance), 0, 32) { } };
		public class StandardDistance : SearchTestParams.DistanceDefinition { public StandardDistance() : base(nameof(StandardDistance), 64, 4096) { } };
		public class LargeDistance : SearchTestParams.DistanceDefinition { public LargeDistance() : base(nameof(LargeDistance), 8192, 65536 * 2) { } };

		public class SmallIterations : SearchTestParams.IterationsDefinition { public SmallIterations() : base(nameof(SmallIterations), 1, 10) {} };
		public class StandardIterations : SearchTestParams.IterationsDefinition { public StandardIterations() : base(nameof(StandardIterations), 10, 100) { } };

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
		const int MinSmallBuffer = 65536 * 4;
		const int MaxSmallBuffer = 1048576;
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
				name: "Small Distance, Small Pattern, Small Buffer",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinSmallPattern,
				maxPatternSize: MaxSmallPattern,
				minBufferSize: MinSmallBuffer,
				maxBufferSize: MaxSmallBuffer,
				minDistance: MinSmallDistance,
				maxDistance: MaxSmallDistance
			),
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
				name: "Small Distance, Standard Pattern, Large Buffer",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinStandardPattern,
				maxPatternSize: MaxStandardPattern,
				minBufferSize: MinLargeBuffer,
				maxBufferSize: MaxLargeBuffer,
				minDistance: MinSmallDistance,
				maxDistance: MaxSmallDistance
			),
			new SearchTestParams
			(
				name: "Small Distance, Standard Pattern, Large Buffer, Buffer is Pattern minus 1 (end byte)",
				maxTestIterations: DefaultMaxIterations,
				minPatternSize: MinStandardPattern,
				maxPatternSize: MaxStandardPattern,
				minBufferSize: MinLargeBuffer,
				maxBufferSize: MaxLargeBuffer,
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

			return false;
		}


		[TestMethod]
		[Timeout(86400 * 365)]    //Test timeout is one year
		
		public void TestAllDerivates()
		{
			Trace.AutoFlush = true;

			Assembly assembly = typeof(Search.Interfaces.ISearch).Assembly;
			ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));

			//Reference search algorithm is now BruteForce, choosen over its simplicity, correctness (and slowness)
			ISearch referenceSearch = new Search.Algorithms.BruteForce();
			Type referenceSearchType = referenceSearch.GetType();

			//Blacklists
			HashSet<Type> blacklistedTypes = new HashSet<Type>()
			{
				typeof(Search.Common.SearchBase)
			};
			List<Type> blacklistedAttributes = new List<Type>()
			{
				typeof(Search.Attributes.UnstableAttribute),
				//typeof(Search.Attributes.ExperimentalAttribute),
				//typeof(Search.Attributes.SlowAttribute),
			};

			string stateFilePath = string.Empty;
			//string stateFilePath = @"C:\Work3\SearchTest\bin\x64\Debug\net7.0\20230826-222556-4417-FastSearch.state";
			if (!string.IsNullOrWhiteSpace(stateFilePath) && File.Exists(stateFilePath))
			{
				FileStreamOptions fso = new FileStreamOptions();
				fso.Share = FileShare.None;
				fso.Access = FileAccess.Read;
				fso.Mode = FileMode.Open;
				fso.BufferSize = 65536;
				fso.PreallocationSize = 0;
				//fso.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
				using (FileStream fs = File.Open(stateFilePath, fso))
				using (BufferedStream bs = new BufferedStream(fs))
				using (BinaryReader br = new BinaryReader(bs, Encoding.UTF8, false))
				{
					int schemaVersion = br.ReadInt32();     //this binary schema version, if element is added or removed, this should be increased and handled
					Trace.WriteLine($"{nameof(schemaVersion)}={schemaVersion}");
					int version = br.ReadInt32();
					/*
														int majorVersion = 1;
														int minorVersion = 0;
														int buildNumber = 11;
														int version = majorVersion;
														version <<= 8;
														version |= minorVersion;
														version <<= 16;
														version |= buildNumber;
														bw.Write(version);

					 */
					int buildNumber = version & 0xffff;
					int minorVersion = (version >> 16) & 0xff;
					int majorVersion = (version >> 24) & 0xff;
					Trace.WriteLine($"{nameof(version)}={version}");
					Trace.WriteLine($"{nameof(majorVersion)}={majorVersion}");
					Trace.WriteLine($"{nameof(minorVersion)}={minorVersion}");
					Trace.WriteLine($"{nameof(buildNumber)}={buildNumber}");

					string processArchitecture = br.ReadString();
					Trace.WriteLine($"{nameof(processArchitecture)}={processArchitecture}");
					string osArchitecture = br.ReadString();
					Trace.WriteLine($"{nameof(osArchitecture)}={osArchitecture}");
					string frameworkDescription = br.ReadString();
					Trace.WriteLine($"{nameof(frameworkDescription)}={frameworkDescription}");
					string runtimeIdentifier = br.ReadString();
					Trace.WriteLine($"{nameof(runtimeIdentifier)}={runtimeIdentifier}");

					string restoredTypeName = br.ReadString();
					Trace.WriteLine($"{nameof(restoredTypeName)}={restoredTypeName}");
					string restoredReferenceTypeName = br.ReadString();
					Trace.WriteLine($"{nameof(restoredReferenceTypeName)}={restoredReferenceTypeName}");

					int restoredPatternLength = br.ReadInt32();
					ReadOnlyMemory<byte> restoredPattern = br.ReadBytes(restoredPatternLength);
					Trace.WriteLine($"{nameof(restoredPattern)}={restoredPattern.Length}");

					int restoredBufferSize = br.ReadInt32();
					int restoredBufferLength = br.ReadInt32();
					Trace.WriteLine($"{nameof(restoredBufferSize)}={restoredBufferSize}");
					Trace.WriteLine($"{nameof(restoredBufferLength)}={restoredBufferLength}");
					Memory<byte> restoredBuffer = br.ReadBytes(restoredBufferLength);

					int restoredGeneratedOffsetsCount = br.ReadInt32();
					Trace.WriteLine($"{nameof(restoredGeneratedOffsetsCount)}={restoredGeneratedOffsetsCount}");
					int[] restoredGeneratedOffsets = new int[restoredGeneratedOffsetsCount];
					for (int i = 0; i < restoredGeneratedOffsetsCount; ++i)
					{
						int offset = br.ReadInt32();
						if(offset < 0 || offset > restoredBufferSize - restoredPatternLength)
						{
							Trace.WriteLine($"{nameof(restoredGeneratedOffsets)}: wrong offset at index {i}, offset is {offset}");
						}
						else if(!restoredBuffer.Slice(offset, restoredPatternLength).Span.SequenceEqual(restoredPattern.Span))
						{
							Trace.WriteLine($"{nameof(restoredGeneratedOffsets)}: wrong offset at index {i}, offset is {offset}, pattern is absent");
						}
						restoredGeneratedOffsets[i] = offset;
					}
					Trace.Flush();

					int restoredReferenceOffsetsCount = br.ReadInt32();
					Trace.WriteLine($"{nameof(restoredReferenceOffsetsCount)}={restoredReferenceOffsetsCount}");
					int[] restoredReferenceOffsets = new int[restoredReferenceOffsetsCount];
					for (int i = 0; i < restoredReferenceOffsetsCount; ++i)
					{
						int offset = br.ReadInt32();
						if (offset < 0 || offset > restoredBufferSize - restoredPatternLength)
						{
							Trace.WriteLine($"{nameof(restoredReferenceOffsets)}: OFFSET OUT OF BOUNDS at index {i}, offset is {offset}");
						}
						else if (!restoredBuffer.Slice(offset, restoredPatternLength).Span.SequenceEqual(restoredPattern.Span))
						{
							Trace.WriteLine($"{nameof(restoredReferenceOffsets)}: PATTERN IS ABSENT for offset at index {i}, offset is {offset}");
						}
						restoredReferenceOffsets[i] = offset;
					}
					Trace.Flush();

					int restoredOffsetsCount = br.ReadInt32();
					Trace.WriteLine($"{nameof(restoredOffsetsCount)}={restoredOffsetsCount}");
					int[] restoredOffsets = new int[restoredOffsetsCount];
					for (int i = 0; i < restoredOffsetsCount; ++i)
					{
						int offset = br.ReadInt32();
						if (offset < 0 || offset > restoredBufferSize - restoredPatternLength)
						{
							Trace.WriteLine($"{nameof(restoredOffsets)}: wrong offset at index {i}, offset is {offset}");
						}
						else if (!restoredBuffer.Slice(offset, restoredPatternLength).Span.SequenceEqual(restoredPattern.Span))
						{
							Trace.WriteLine($"{nameof(restoredOffsets)}: pattern is absent for offset at index {i}, offset is {offset}");
						}
						restoredOffsets[i] = offset;
					}
					Trace.Flush();

					ISearch restoredSearch = assembly.CreateInstance(restoredTypeName) as ISearch ?? throw new ArgumentNullException(nameof(restoredSearch));
					List<int> recalculatedOffsets = new List<int>();
					restoredSearch.Init(restoredPattern, (offset, caller) =>
					{
						recalculatedOffsets.Add(offset);
						return true;
					});
					restoredSearch.FixSearchBuffer(ref restoredBuffer, restoredBufferSize, restoredPattern);
					restoredSearch.Search(restoredBuffer, 0, restoredBufferSize);

					for (int i = 0; i < recalculatedOffsets.Count; ++i)
					{
						int offset = recalculatedOffsets[i];
						if (offset < 0 || offset > restoredBufferSize - restoredPatternLength)
						{
							Trace.WriteLine($"{nameof(recalculatedOffsets)}: wrong offset at index {i}, offset is {offset}");
						}
						else if (!restoredBuffer.Slice(offset, restoredPatternLength).Span.SequenceEqual(restoredPattern.Span))
						{
							Trace.WriteLine($"{nameof(recalculatedOffsets)}: pattern is absent for offset at index {i}, offset is {offset}");
						}
					}
					Trace.Flush();

					List<(string name, IReadOnlyList<int> list)> listOfLists = new();
					listOfLists.Add((name: nameof(recalculatedOffsets), list: recalculatedOffsets));
					listOfLists.Add((name: nameof(restoredGeneratedOffsets), list: restoredGeneratedOffsets));
					listOfLists.Add((name: nameof(restoredReferenceOffsets), list: restoredReferenceOffsets));
					listOfLists.Add((name: nameof(restoredOffsets), list: restoredOffsets));

					for (int i = 0; i < listOfLists.Count; ++i)
					{
						for(int j = i + 1; j < listOfLists.Count; ++j)
						{
							string firstName = listOfLists[i].name;
							string secondName = listOfLists[j].name;
							IReadOnlyList<int> first = listOfLists[i].list;
							IReadOnlyList<int> second = listOfLists[j].list;
							bool equal = first.Count == second.Count && first.SequenceEqual(second);
							if(!equal)
							{
								Trace.WriteLine($"{firstName} ({first.Count}) not equal to {secondName} ({second.Count})");
							}
						}
					}
					Trace.Flush();
					Debugger.Break();

				} //END: using(BinaryReader...
				Trace.Flush();

				Assert.Fail();
				Debugger.Break();
				Process.GetCurrentProcess().Kill();	//This is the end
			}

			Type patternType = typeof(SearchTestParams.PatternDefinition);
			Type bufferType = typeof(SearchTestParams.BufferDefinition);
			Type distanceType = typeof(SearchTestParams.DistanceDefinition);
			Type interationsType = typeof(SearchTestParams.IterationsDefinition);

			Type[] assemblyTypes = this.GetType().Assembly.GetTypes();

			Type[] patternDefinitions = assemblyTypes.Where(x => !x.Equals(patternType) && x.IsAssignableTo(patternType)).ToArray();
			Type[] bufferDefinitions = assemblyTypes.Where(x => !x.Equals(bufferType) && x.IsAssignableTo(bufferType)).ToArray();
			Type[] distanceDefinitions = assemblyTypes.Where(x => !x.Equals(distanceType) && x.IsAssignableTo(distanceType)).ToArray();
			Type[] iterationDefinitions = assemblyTypes.Where(x => !x.Equals(interationsType) && x.IsAssignableTo(interationsType)).ToArray();
			SearchTestParams.BufferPatternFillDelegate[] delegateDefinitions =
			{
				SearchTestParams.DirectBufferPatternFill,
				SearchTestParams.PatternMinusOneBufferPatternFill,
				SearchTestParams.RandomPatternSegmentBufferPatternFill
			};
			int[] currentLengths = new int[]
			{
				patternDefinitions.Length,
				bufferDefinitions.Length,
				distanceDefinitions.Length,
				iterationDefinitions.Length,
				delegateDefinitions.Length
			};
			int[] currentCounter = Enumerable.Repeat<int>(0, currentLengths.Length).ToArray();

			List<(Type[] types, SearchTestParams.BufferPatternFillDelegate patternFill)> combinations = new List<(Type[], SearchTestParams.BufferPatternFillDelegate)>();
			combinations.Add
			(
				(
					new Type[]
					{
						patternDefinitions[currentCounter[0]],
						bufferDefinitions[currentCounter[1]],
						distanceDefinitions[currentCounter[2]],
						iterationDefinitions[currentCounter[3]]
					},
					delegateDefinitions[currentCounter[4]]
				)
			);
			bool overflow = false;
			int currentIndex = 0;
			while (true)
			{
				int curr = currentCounter[currentIndex];
				int next = (++curr) % currentLengths[currentIndex];
				currentCounter[currentIndex] = next;
				overflow = next < curr;
				if (overflow)
				{
					if (currentIndex == currentCounter.Length - 1)
					{
						break;
					}
					++currentIndex;
				}
				else
				{
					combinations.Add
					(
						(
							new Type[]
							{
								patternDefinitions[currentCounter[0]],
								bufferDefinitions[currentCounter[1]],
								distanceDefinitions[currentCounter[2]],
								iterationDefinitions[currentCounter[3]]
							},
							delegateDefinitions[currentCounter[4]]
						)
					);

					currentIndex = 0;
				}
			}

			SearchTests.Clear();

			for (int i = 0; i < combinations.Count; ++i)
			{
				string[] definitionTypes =
				{
							$"pattern={combinations[i].types[0].Name!}",
							$"buffer={combinations[i].types[1].Name!}",
							$"distance={combinations[i].types[2].Name!}",
							$"iteration={combinations[i].types[3].Name!}",
							$"delegate={combinations[i].patternFill.Method.Name}"
				};
				string name = string.Join(@", ", definitionTypes);
				Assembly thisAssembly = this.GetType().Assembly;
				PatternDefinition pattern = thisAssembly.CreateInstance(combinations[i].types[0].FullName!) as PatternDefinition ?? throw new ArgumentNullException();
				BufferDefinition buffer = thisAssembly.CreateInstance(combinations[i].types[1].FullName!) as BufferDefinition ?? throw new ArgumentNullException();
				DistanceDefinition distance = thisAssembly.CreateInstance(combinations[i].types[2].FullName!) as DistanceDefinition ?? throw new ArgumentNullException();
				IterationsDefinition iteration = thisAssembly.CreateInstance(combinations[i].types[3].FullName!) as IterationsDefinition ?? throw new ArgumentNullException();
				SearchTestParams.BufferPatternFillDelegate? patternFillDelegate = combinations[i].patternFill;
				SearchTests.Add(new SearchTestParams(name: name, iteration.Max, pattern.Min, pattern.Max, buffer.Min, buffer.Max, distance.Min, distance.Max, bufferPatternFill: patternFillDelegate));
			}


			Trace.WriteLine(heavyDivider);

			//Dictionary of Type (which must inherit from ISearch) and its measured search statistics
			Dictionary<Type, SearchStatistics[]> statistics = new();

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

					string[] testIterationInfo = new string[]
					{
						$"{DateTime.Now.ToString(@"yyyy-MM-dd HH:mm:ss.ffffzzz")}"
						, test.MaxIterations == 1 ? string.Empty : $"#{(testSubIteration + 1)}/{test.MaxIterations}"
						, $"{nameof(test.PatternSize)}={test.PatternSize}"
						, $"{nameof(test.BufferSize)}={test.BufferSize}"
						, $"{nameof(referenceOffsets)}={referenceOffsets.Count}"
					};
					Trace.WriteLine(string.Join(@", ", testIterationInfo.Where(x => !string.IsNullOrEmpty(x))));

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

						//If algorithm needs buffer extension (to be able to stop), it will override FixSearchBuffer() in its implementation
						try
						{
							genericSearch.FixSearchBuffer(ref testBuffer, bufferSize, testPattern);
						}
						catch (Exception ex)
						{
							Assert.Fail($"[FIX EXCEPTION] Type={type}, Details:{ex}", ex);
						}

						//Accumulate duration of search for each generic search algorithm
						try
						{
							searchWatch.Restart();
							genericSearch.Search(testBuffer, 0, bufferSize);
						}
						catch (Exception ex)
						{
							Assert.Fail($"[SEARCH EXCEPTION] Type={type}, Details:{ex}", ex);
						}
						searchWatch.Stop();
						_ = statistics[type][testIteration].IncrementSearchTime(searchWatch.Elapsed.Ticks);

						//If algorithm enlarged the buffer, compact it again to ensure that next algorithm would not reuse the existing enlargement (and cover possible lack of fixing)
						if(testBuffer.Length > bufferSize)
						{
							byte[] newTestBuffer = new byte[bufferSize];
							testBuffer.Slice(0, bufferSize).CopyTo(newTestBuffer);
							testBuffer = newTestBuffer;
						}

						//Check if offsets of the found pattern returned by generic algorithm (other than reference) is equal to reference offsets
						if (!type.Equals(referenceSearchType) && !EqualOffsets(type.FullName!, statistics[type][testIteration].Offsets, referenceSearchType.FullName!, referenceOffsets))
						{
								//If mismatched number of offsets or their values differ, dump all the relevant states to a file
								List<int> offsets = statistics[type][testIteration].Offsets;

								string path = $"{DateTime.Now.ToString(@"yyyyMMdd-HHmmss-ffff")}-{type.Name}.state";
								//Path.Combine()
								using(FileStream fs = File.Create(path))
								using(BufferedStream bs = new BufferedStream(fs))
								using(BinaryWriter bw = new BinaryWriter(bs, Encoding.UTF8, false))
								{
									List<string> stateMessages = new List<string>();

									stateMessages.Add($"Dumping state path: \"{path}\" for {type.FullName!}");

									int schemaVersion = 1;
									bw.Write(schemaVersion);

									int majorVersion = 1;
									int minorVersion = 0;
									int buildNumber = 11;
									int version = majorVersion;
									version <<= 8;
									version |= minorVersion;
									version <<= 16;
									version |= buildNumber;
									bw.Write(version);

									string processArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
									bw.Write(processArchitecture);
									string osArchitecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();
									bw.Write(osArchitecture);
									string frameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
									bw.Write(frameworkDescription);
									string runtimeIdentifier = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;
									bw.Write(runtimeIdentifier);

									bw.Write(type.FullName!);
									bw.Write(referenceSearchType.FullName!);

									bw.Write(testPattern.Length);
									stateMessages.Add($"pattern length: {testPattern.Length}");
									bw.Write(testPattern.Span);

									bw.Write(bufferSize);
									stateMessages.Add($"buffer size: {bufferSize}");
									bw.Write(testBuffer.Length);
									stateMessages.Add($"buffer length: {testBuffer.Length}");
									bw.Write(testBuffer.Span);

									//Generated offsets
									stateMessages.Add($"generated offsets: {test.Offsets.Length}");
									bw.Write(test.Offsets.Length);
									test.Offsets.ToList().ForEach(x => bw.Write(x));

									//Reference ISearch offsets (any search algorithm instance assigned to referenceSearch, used in reference timing calculation)
									stateMessages.Add($"reference ({referenceSearchType.FullName!}) offsets: {referenceOffsets.Count}");
									bw.Write(referenceOffsets.Count);
									referenceOffsets.ForEach(x => bw.Write(x));

									//Current ISearch offsets
									stateMessages.Add($"current ({type.FullName}) offsets: {test.Offsets.Length}");
									bw.Write(offsets.Count);
									offsets.ForEach(x => bw.Write(x));

									bw.Flush();
									bs.Flush();
									fs.Flush();
									bw.Close();

									Trace.WriteLine(string.Join(System.Environment.NewLine, stateMessages));
								}
								Assert.Fail($"{type.FullName} (Count={offsets.Count}) not equal to {nameof(referenceOffsets)} (Count={referenceOffsets.Count})");
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

