namespace StatsSharp
{
	public readonly struct StatsValue
	{
		public readonly string Name;
		public readonly double Value;

		public StatsValue(string name, double value) {
			this.Name = name;
			this.Value = value;
		}
	}
}