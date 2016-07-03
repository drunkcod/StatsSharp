using System.IO;
using Cone;
using Cone.Helpers;

namespace StatsSharp.Specs
{
	[Describe(typeof(StatsAgent))]
	public class StatsAgentSpec
	{
		public void raises_OnError_when_failing_to_add_performance_counter() {
			var agent = new StatsAgent();
			var onError = new EventSpy<ErrorEventArgs>();
			agent.OnError += onError;
			Check.That(
				() => agent.AddPerformanceCounter("PerfC", @"\Bougs(_Total)\Metric") == false,
				() => onError.HasBeenCalled);
		}
	}
}
