using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace StatsSharp
{
	public class StatsAgent
	{
		readonly ConcurrentBag<KeyValuePair<string, Func<double>>> timers = new ConcurrentBag<KeyValuePair<string, Func<double>>>();
		readonly ConcurrentBag<KeyValuePair<string, Func<ulong>>> gauges = new ConcurrentBag<KeyValuePair<string, Func<ulong>>>();
		readonly StatsCollectionConfig config = new StatsCollectionConfig();

		Thread worker;
		StatsCollection collectedStats;

		public StatsSummary CurrentStats = new StatsSummary(DateTime.UtcNow, new StatsValue[0]);
		public TimeSpan FlushInterval = TimeSpan.FromSeconds(10);
		public TimeSpan SampleInterval = TimeSpan.FromSeconds(1);
		public IStatsClient Stats => (IStatsClient)collectedStats ?? NullStatsClient.Instance;

		public event EventHandler<ErrorEventArgs> OnError;
		public event EventHandler<EventArgs> Flushing;

		public bool AddPerformanceCounter(string name, string path) {
			var m = Regex.Match(path, @"\\(?<category>.+?)(\((?<instance>.+)\))?\\(?<counter>.+)");
			return AddPerformanceCounter(name, 
				m.Groups["category"].Value,
				m.Groups["counter"].Value,
				m.Groups["instance"].Value);
		}

		public bool AddPerformanceCounter(string name, string category, string counter, string instance) {
			try {
				var pc = new PerformanceCounter(category, counter, instance, true);
				if(pc.CounterType != PerformanceCounterType.ElapsedTime) {
					AddTimer(name, () => pc.NextValue());
				} else { 
					AddGauge(name, GetElapsedTimeSampler(pc));
				}
				return true;

			} catch (Exception ex) {
				HandleError(ex);
				return false;
			}
		}

		static Func<ulong> GetElapsedTimeSampler(PerformanceCounter pc) {
			var scale = 1.0 / 1000;
			return () => {
				var sample = pc.NextSample();
				return (ulong)((sample.CounterTimeStamp - sample.RawValue) / (scale * sample.CounterFrequency));
			};
		}

		public void AddGauge(string name, Func<ulong> takeSample) =>
			gauges.Add(new KeyValuePair<string, Func<ulong>>(name, takeSample));

		public void AddTimer(string name, Func<double> takeSample) =>
			timers.Add(new KeyValuePair<string, Func<double>>(name, takeSample));

		public void Start() {
			if(worker != null) 
				throw new InvalidOperationException("Agent already started.");
			BeginCollection();
			worker = new Thread(() => {
				try {
					Task nextSample;
					for (var lastFlush = AlignToInterval(DateTime.UtcNow, FlushInterval); ; nextSample.Wait()) {
						nextSample = Task.Delay(SampleInterval);
						Read();
						if (DateTime.UtcNow - lastFlush < FlushInterval)
							continue;

						lastFlush += FlushInterval;
						Flush(lastFlush);
					}
				} catch(Exception ex) {
					collectedStats = null;
					HandleError(ex);
				}
				worker = null;
			});
			worker.Start();
		}

		public void Read() {
			foreach(var item in timers)
				Stats.Send(new Metric(item.Key, MetricValue.Time(item.Value())));
			foreach(var item in gauges)
				Stats.Send(new Metric(item.Key, MetricValue.Gauge(item.Value())));
		}

		public void BeginCollection() {
			if(collectedStats == null)
			collectedStats = new StatsCollection(config);
		}

		public void Flush(DateTime lastFlush) {
			Flushing?.Invoke(this, EventArgs.Empty);
			CurrentStats = collectedStats.Flush(lastFlush, FlushInterval);
		}

		void HandleError(Exception ex) {
			var err = OnError;
			if(err == null)
				return;
			var e = new ErrorEventArgs(ex);
			foreach(EventHandler<ErrorEventArgs> handler in err.GetInvocationList())
				try { handler(this, e); } catch { }
		}

		static DateTime AlignToInterval(DateTime now, TimeSpan interval) =>
			now.AddTicks(-now.TimeOfDay.Ticks % interval.Ticks);
	}
}
