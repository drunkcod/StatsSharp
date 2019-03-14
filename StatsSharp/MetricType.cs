namespace StatsSharp
{
	public enum MetricType : byte
	{
		Gauge = 0,
		GaugeDelta = 1,
		Counter = 2,
		Time = 3,
		MetricTypeMask = 3,
		Float = 4,
	}
}