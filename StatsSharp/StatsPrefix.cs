namespace StatsSharp
{
	public readonly struct StatsPrefix
	{
		readonly string prefix;

		public StatsPrefix(string prefix) {
			this.prefix = prefix.EndsWith(".")
			? prefix
			: prefix + '.'; 
		}

		public override string ToString() => prefix.TrimEnd('.');

		public static string operator+(StatsPrefix lhs, string rhs) => lhs.prefix + rhs;
	}
}