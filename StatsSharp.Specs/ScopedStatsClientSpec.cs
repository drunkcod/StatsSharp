using System;
using System.Collections.Generic;
using Cone;
using Cone.Helpers;
using Xunit;

namespace StatsSharp.Specs
{
	public class ScopedStatsClientSpec
	{
		class MyStatsClient : IStatsClient
		{
			public Action<Metric> HandleSend = _ => { };

			void IStatsClient.Send(Metric metric) => HandleSend(metric);

			void IStatsClient.Send(IEnumerable<Metric> metrics) {
				foreach(var item in metrics)
					HandleSend(item);
			}
		}

		[Fact]
		public void adds_prefix_to_metrics() {
			var baseClient = new MyStatsClient();
			var scoped = baseClient.Scope("MyStats.");

			var send = new ActionSpy<Metric>(m => Check.That(() => m.Name.StartsWith("MyStats.")));
			baseClient.HandleSend = send;

			scoped.Send(new Metric("MyMetric", MetricValue.Gauge(1)));
			Check.That(() => send.HasBeenCalled);
		}

		[Fact]
		public void adds_dot_separator_if_missing() {
			var baseClient = new MyStatsClient();
			var scoped = baseClient.Scope("MyStats");

			var send = new ActionSpy<Metric>(m => Check.That(() => m.Name.StartsWith("MyStats.")));
			baseClient.HandleSend = send;

			scoped.Send(new Metric("MyMetric", MetricValue.Gauge(1)));
			Check.That(() => send.HasBeenCalled);

		}
	}
}
