using System.Collections.Generic;

namespace StatsSharp
{
	public class StatsCollectionConfig
	{
		public readonly List<double> Percentiles = new List<double>();
		public readonly Dictionary<string, double> Scales = new Dictionary<string, double>(); 

		public double GetScale(string metricName) {
			double scale;
			return Scales.TryGetValue(metricName, out scale) ? scale : 1.0;
		}
	}
}