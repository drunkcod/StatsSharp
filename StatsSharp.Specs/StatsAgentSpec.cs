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

		public void can_add_metric_before_flush() {
			Agent.Flushing += (sender, _) =>
				((StatsAgent)sender).Stats.Send(new Metric("MyLastMinuteStat", MetricValue.Gauge(1)));

			Agent.BeginCollection();
			Agent.Flush(DateTime.UtcNow);
			Check.That(() => Agent.CurrentStats.Any(x => Metric.GetName(x.Name) == "MyLastMinuteStat"));
		}

		public void handles_ElapsedTime_performance_counter() {
			Assume.That(() => Agent.AddPerformanceCounter("UpTime", @"\System\System Up Time"));
			Agent.BeginCollection();
			var uptime = TimeSpan.FromMilliseconds(Environment.TickCount);
			Agent.Read();
			Agent.Flush(DateTime.UtcNow);

			Check.That(() => TimeSpan.FromMilliseconds(Agent.CurrentStats["stats.gauges.UpTime"].Value) - uptime < TimeSpan.FromSeconds(1));
		}
	}
}