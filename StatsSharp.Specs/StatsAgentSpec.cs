using System;
using System.IO;
using System.Linq;
using Cone;
using Cone.Helpers;

namespace StatsSharp.Specs
{
	[Describe(typeof(StatsAgent))]
	public class StatsAgentSpec
	{
		public StatsAgent Agent;

		[BeforeEach]
		public void given_a_StatsAgent() {
			Agent = new StatsAgent();
		}

		public void supports_three_part_paths() =>
			Check.That(() => Agent.AddPerformanceCounter("%CPU", @"\Processor(_Total)\% Processor Time"));

		public void raises_OnError_when_failing_to_add_performance_counter() {
			var onError = new EventSpy<ErrorEventArgs>();
			Agent.OnError += onError;
			Check.That(
				() => Agent.AddPerformanceCounter("PerfC", @"\Bougs(_Total)\Metric") == false,
				() => onError.HasBeenCalled);
		}

		public void gracefully_handles_OnError_raisng_exceptions() {
			var onError = new EventSpy<ErrorEventArgs>();
			Agent.OnError += (_,e) => { throw new Exception(); };
			Agent.OnError += onError;
			//force a failure
			Agent.AddPerformanceCounter("PerfC", @"\Bougs(_Total)\Metric");

			Check.That(() => onError.HasBeenCalled);
		}
	}
}