namespace StatsSharp
{
	public struct StatsPrefix
	{
		readonly string prefix;

		public StatsPrefix(string prefix) {
			this.prefix = prefix.EndsWith(".") 
				? prefix
				: prefix + '.'; 
		}

		public override string ToString() { return prefix.TrimEnd('.'); }

		public static string operator+(StatsPrefix lhs, string rhs) {return lhs.prefix + rhs; }
	}
}