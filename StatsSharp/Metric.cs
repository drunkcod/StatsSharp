namespace StatsSharp
{
	public struct Metric
	{
		public readonly string Name;
		public readonly MetricValue Value;

		public Metric(string name, MetricValue value) {
			this.Name = name;
			this.Value = value;
		}
	}
}