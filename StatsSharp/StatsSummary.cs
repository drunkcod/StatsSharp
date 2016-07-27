using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StatsSharp
{
	public struct StatsSummary : IEnumerable<StatsValue>
	{
		readonly StatsValue[] values;
		public readonly DateTime Timestamp;
		
		public int Count => values.Length;
		public StatsValue this[int index] => values[index];

		public StatsSummary(DateTime timestamp, StatsValue[] values) {
			this.values = values;
			this.Timestamp = timestamp;
		}

		public IEnumerator<StatsValue> GetEnumerator() => values.AsEnumerable().GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();
	}
}