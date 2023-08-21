namespace SearchTest
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public class SearchStatistics
	{
		public readonly List<int> Offsets;
		public long InitTime;
		public long SearchTime;

		public SearchStatistics(long initTime, long searchTime)
		{
			this.Offsets = new List<int>();
			this.InitTime = initTime;
			this.SearchTime = searchTime;
		}

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
	};  //END: class SearchStatistics

};	//END: namespace


