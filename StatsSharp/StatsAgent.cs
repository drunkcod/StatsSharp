using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace StatsSharp
{
	public class StatsAgent
	{
		StatsCollection collectedStats;
		Thread worker;
		readonly ConcurrentBag<KeyValuePair<string, Func<ulong>>> counters = new ConcurrentBag<KeyValuePair<string, Func<ulong>>>();

		public StatsSummary CurrentStats = new StatsSummary(DateTime.UtcNow, new StatsValue[0]);
		public TimeSpan FlushInterval = TimeSpan.FromSeconds(10);
		public TimeSpan SampleInterval = TimeSpan.FromSeconds(1);
		public IStatsClient Stats => (IStatsClient)collectedStats ?? NullStatsClient.Instance;

		public void AddPerformanceCounter(string name, string path) {
			var m = Regex.Match(path, @"\\(?<category>.+)\((?<instance>.+)\)\\(?<counter>.+)");
			AddPerformanceCounter(name, 
				m.Groups["category"].Value,
				m.Groups["counter"].Value,
				m.Groups["instance"].Value
			);
		}

		public void AddPerformanceCounter(string name, string category, string counter, string instance) {
			var pc = new PerformanceCounter(category, counter, instance, true);
			counters.Add(new KeyValuePair<string,Func<ulong>>(name, () => (ulong)pc.NextValue()));
		}

		public void Start() {
			if(worker != null) 
				throw new InvalidOperationException("Agent already started.");

			worker = new Thread(() => {
				collectedStats = new StatsCollection();
				try {
					Task nextSample;
					for (var lastFlush = DateTime.UtcNow; ; nextSample.Wait()) {
						nextSample = Task.Delay(SampleInterval);
						foreach(var item in counters)
							Stats.Timer(item.Key, item.Value());

						if (DateTime.UtcNow - lastFlush < FlushInterval)
							continue;

						lastFlush = DateTime.UtcNow;
						CurrentStats = collectedStats.Flush(FlushInterval);
					}
				}
				catch {
					collectedStats = null;
				}
				worker = null;
			});
			worker.Start();
		}
	}}
