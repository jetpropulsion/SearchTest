﻿namespace SearchTest
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public class SearchTestParams
	{
		public delegate void OffsetGeneratorDelegate(int bufferSize, int patternSize, int minDistance, int maxDistance, out int[] offsets);
		public delegate void PatternGeneratorDelegate(int minPatternSize, int maxPatternSize, out int patternSize, out byte[] pattern);
		public delegate void BufferGeneratorDelegate(int minBufferSize, int maxBufferSize, int safetyMargin, out int bufferSize, out byte[] buffer);
		public delegate void BufferFillDelegate(ref byte[] buffer, int bufferSize);
		public delegate void BufferPatternFillDelegate(ref byte[] buffer, int bufferSize, in byte[] pattern, in IReadOnlyList<int> offsets);

		public OffsetGeneratorDelegate OffsetGenerator = DefaultOffsetGenerator;
		public PatternGeneratorDelegate PatternGenerator = DefaultPatternGenerator;
		public BufferGeneratorDelegate BufferGenerator = DefaultBufferGenerator;
		public BufferFillDelegate BufferFill = DefaultBufferFill;
		public BufferPatternFillDelegate BufferPatternFill = DefaultBufferPatternFill;

		public SearchTestParams
		(
			string name,
			int maxTestIterations,
			int minPatternSize, int maxPatternSize,
			int minBufferSize, int maxBufferSize,
			int minDistance, int maxDistance,
			OffsetGeneratorDelegate? offsetGenerator = null,
			PatternGeneratorDelegate? patternGenerator = null,
			BufferGeneratorDelegate? bufferGenerator = null,
			BufferFillDelegate? bufferFill = null,
			BufferPatternFillDelegate? bufferPatternFill = null
		)
		{
			this.Name = name;
			this.MaxIterations = maxTestIterations;
			this.MinPatternSize = minPatternSize;
			this.MaxPatternSize = maxPatternSize;
			this.MinBufferSize = minBufferSize;
			this.MaxBufferSize = maxBufferSize;
			this.MinDistance = minDistance;
			this.MaxDistance = maxDistance;
			this.Pattern = null;
			this.Buffer = null;
			this.Offsets = null;
			this.OffsetGenerator = offsetGenerator ?? this.OffsetGenerator;
			this.PatternGenerator = patternGenerator ?? this.PatternGenerator;
			this.BufferGenerator = bufferGenerator ?? this.BufferGenerator;
			this.BufferFill = bufferFill ?? this.BufferFill;
			this.BufferPatternFill = bufferPatternFill ?? this.BufferPatternFill;
		}

		public void Reset()
		{
			Assert.IsFalse(string.IsNullOrWhiteSpace(this.Name));
			Assert.IsNotNull(this.PatternGenerator);
			Assert.IsNotNull(this.BufferGenerator);
			Assert.IsNotNull(this.OffsetGenerator);
			Assert.IsNotNull(this.BufferFill);
			Assert.IsNotNull(this.BufferPatternFill);
			//TODO: uncomment assignments below and check if it works
			//this.PatternLocation = null;
			//this.Buffer = null;
			//this.PatternSize = 0;
			//this.BufferSize = 0;
			//this.Offsets = null;
			this.PatternGenerator(this.MinPatternSize, this.MaxPatternSize, out this.PatternSize, out this.Pattern);
			this.BufferGenerator(this.MinBufferSize, this.MaxBufferSize, this.PatternSize, out this.BufferSize, out this.Buffer);
			this.BufferFill(ref this.Buffer, this.BufferSize);
			this.Pattern.CopyTo(this.Buffer, this.BufferSize);	//Copy PatternLocation to the end of the Buffer
			this.OffsetGenerator(this.BufferSize, this.PatternSize, this.MinDistance, this.MaxDistance, out this.Offsets);
			this.BufferPatternFill(ref this.Buffer, this.BufferSize, this.Pattern, this.Offsets);
		}

		public string Name;
		public int MaxIterations;
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
		public int[] Offsets;

		#region Default Implementations

		public static void DefaultOffsetGenerator(int bufferSize, int patternSize, int minDistance, int maxDistance, out int[] offsets)
		{
			int lastOffset = bufferSize - patternSize;
			List<int> result = new List<int>();

			int next = 0;
			while (next <= lastOffset)
			{
				int distance = Random.Shared.Next(minDistance, maxDistance + 1);
				int nextMax = int.Min(next + distance, lastOffset);
				int offset = Random.Shared.Next(next, nextMax + 1);

				//Trace.WriteLine($"{nameof(offset)}={offset}, {nameof(next)}={next}, {nameof(patternSize)}={patternSize}, {nameof(distance)}={distance}, {nameof(nextMax)}={nextMax}, {nameof(lastOffset)}={lastOffset}");

				Assert.IsTrue(offset <= lastOffset);
				result.Add(offset);
				next = offset + patternSize; //set next generated offset's start boundary
			}
			//NOTE: do not sort result - this allows overlapping strings
			offsets = result.ToArray();
			Assert.IsTrue(offsets.Length > 0);
		}

		public static void DefaultPatternGenerator(int minPatternSize, int maxPatternSize, out int patternSize, out byte[] pattern)
		{
			patternSize = Random.Shared.Next(minPatternSize, maxPatternSize + 1);
			pattern = new byte[patternSize];
			Random.Shared.NextBytes(pattern);
		}

		public static void DefaultBufferGenerator(int minBufferSize, int maxBufferSize, int safetyMargin, out int bufferSize, out byte[] buffer)
		{
			bufferSize = Random.Shared.Next(minBufferSize, maxBufferSize + 1);
			buffer = new byte[bufferSize + safetyMargin];
		}

		public static void DefaultBufferFill(ref byte[] buffer, int bufferSize)
		{
			Assert.IsTrue(buffer.Length >= bufferSize);
			Random.Shared.NextBytes(buffer[0..bufferSize]);
		}

		public static void DefaultBufferPatternFill(ref byte[] buffer, int bufferSize, in byte[] pattern, in IReadOnlyList<int> offsets)
		{
			Assert.IsTrue(buffer.Length >= bufferSize);
			Assert.IsTrue(offsets.Count > 0);

			int patternSize = pattern.Length;
			int lastOffset = bufferSize - patternSize;
			for (int i = 0; i < offsets.Count; i++)
			{
				int offset = offsets[i];
				Assert.IsTrue(offset <= lastOffset);
				pattern.CopyTo(buffer, offset);
			}
		}

		#endregion

		#region Custom Buffer Fill methods

		public static void PatternMinusOneBufferPatternFill(ref byte[] buffer, int bufferSize, in byte[] pattern, in IReadOnlyList<int> offsets)
		{
			Assert.IsTrue(buffer.Length >= bufferSize);
			Assert.IsTrue(offsets.Count > 0);

			int patternSize = pattern.Length;
			int lastOffset = bufferSize - patternSize;

			int offset = 0;
			while(offset <= lastOffset)
			{
				pattern.CopyTo(buffer, offset);
				offset += patternSize - 1;
			}

			for (int i = 0; i < offsets.Count; i++)
			{
				offset = offsets[i];
				Assert.IsTrue(offset <= lastOffset);
				pattern.CopyTo(buffer, offset);
			}
		}

		public static void NonPatternByteBufferPatternFill(ref byte[] buffer, int bufferSize, in byte[] pattern, in IReadOnlyList<int> offsets)
		{
			Assert.IsTrue(offsets.Count > 0);
			Assert.IsTrue(buffer.Length >= bufferSize);
			Assert.IsTrue(pattern.Length < 256);

			int patternSize = pattern.Length;
			int lastOffset = bufferSize - patternSize;
			int[] freqs = Enumerable.Repeat<int>(0, 256).ToArray();
			for(int i = 0; i < pattern.Length; ++i)
			{
				++freqs[pattern[i]];
			}
			List<byte> patternExcludedBytes = new List<byte>();
			for(int i = 0; i < 256; ++i)
			{
				if (freqs[i] == 0)
				{
					patternExcludedBytes.Add((byte)i);
				}
			}
			int excludedCount = patternExcludedBytes.Count;
			for(int i = 0; i < bufferSize; ++i)
			{
				buffer[i] = patternExcludedBytes[ Random.Shared.Next(0, excludedCount) ];
			}
		}

		public static void RandomPatternSegmentBufferPatternFill(ref byte[] buffer, int bufferSize, in byte[] pattern, in IReadOnlyList<int> offsets)
		{
			Assert.IsTrue(offsets.Count > 0);
			Assert.IsTrue(buffer.Length >= bufferSize);

			int patternSize = pattern.Length;
			int lastOffset = bufferSize - patternSize;

			int offset = 0;
			while (offset <= lastOffset)
			{
				int start = Random.Shared.Next(0, patternSize / 2);
				int end = Random.Shared.Next(start + 1, patternSize);
				Span<byte> target = buffer[(offset + start)..(offset + end)];
				target = pattern[start..end];
				offset += 1 + end - start;
			}

			for (int i = 0; i < offsets.Count; i++)
			{
				offset = offsets[i];
				Assert.IsTrue(offset <= lastOffset);
				pattern.CopyTo(buffer, offset);
			}
		}

		#endregion

	};  //END: class SearchTestParams

};	//END: namespace


