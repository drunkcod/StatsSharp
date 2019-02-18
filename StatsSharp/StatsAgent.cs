using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace StatsSharp
{
	public class StatsAgent
	{
		readonly ConcurrentBag<KeyValuePair<string, Func<double>>> timers = new ConcurrentBag<KeyValuePair<string, Func<double>>>();
		readonly ConcurrentBag<KeyValuePair<string, Func<ulong>>> gauges = new ConcurrentBag<KeyValuePair<string, Func<ulong>>>();
		readonly SampleAgent sampler = new SampleAgent();

		public StatsSummary CurrentStats = new StatsSummary(DateTime.UtcNow, new StatsValue[0]);
		
		public TimeSpan FlushInterval {
			get => sampler.FlushInterval;
			set => sampler.FlushInterval = value;
		}
		
		public TimeSpan SampleInterval {
			get => sampler.SampleInterval;
			set => sampler.SampleInterval = value;
		}
		
		public IStatsClient Stats => sampler.Stats;

		public event EventHandler<ErrorEventArgs> OnError { 
			add => sampler.OnError += value;
			remove => sampler.OnError -= value;
		}

		public event EventHandler<EventArgs> Flushing {
			add => sampler.Flushing += _ => value(this, EventArgs.Empty);
			remove => sampler.Flushing -= _ => value(this, EventArgs.Empty);
		}

		public StatsAgent() {
			sampler.Sample += _ => Read();
			sampler.Flushed += summary => CurrentStats = summary;
		}

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
				sampler.HandleError(ex);
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

		public void Start() => sampler.Start();

		public void Read() {
			foreach(var item in timers)
				Stats.Send(new Metric(item.Key, MetricValue.Time(item.Value())));
			foreach(var item in gauges)
				Stats.Send(new Metric(item.Key, MetricValue.Gauge(item.Value())));
		}
	}
}
