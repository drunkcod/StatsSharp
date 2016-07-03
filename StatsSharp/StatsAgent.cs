﻿using System;
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
		StatsCollectionConfig config = new StatsCollectionConfig();
		StatsCollection collectedStats;
		Thread worker;
		readonly ConcurrentBag<KeyValuePair<string, Func<ulong>>> counters = new ConcurrentBag<KeyValuePair<string, Func<ulong>>>();

		public StatsSummary CurrentStats = new StatsSummary(DateTime.UtcNow, new StatsValue[0]);
		public TimeSpan FlushInterval = TimeSpan.FromSeconds(10);
		public TimeSpan SampleInterval = TimeSpan.FromSeconds(1);
		public IStatsClient Stats => (IStatsClient)collectedStats ?? NullStatsClient.Instance;

		public event EventHandler<ErrorEventArgs> OnError; 

		public bool AddPerformanceCounter(string name, string path) => AddPerformanceCounter(name, path, null);
		public bool AddPerformanceCounter(string name, string path, double? decimalScale) {
			var m = Regex.Match(path, @"\\(?<category>.+)\((?<instance>.+)\)\\(?<counter>.+)");
			return AddPerformanceCounter(name, 
				m.Groups["category"].Value,
				m.Groups["counter"].Value,
				m.Groups["instance"].Value, 
				decimalScale
			);
		}

		public bool AddPerformanceCounter(string name, string category, string counter, string instance, double? decimalScale = null) {
			try {
				var pc = new PerformanceCounter(category, counter, instance, true);
				var scale = decimalScale ?? 1.0;
				Func<ulong> takeSample = () => (ulong)(pc.NextValue() * scale);
				AddCounter(name, takeSample, decimalScale);

				return true;

			} catch (Exception ex) {
				OnError?.Invoke(this, new ErrorEventArgs(ex));
				return false;
			}
		}

		public void AddCounter(string name, Func<ulong> takeSample, double? decimalScale = null) {
			counters.Add(new KeyValuePair<string, Func<ulong>>(name, takeSample));
			if (decimalScale.HasValue)
				config.Scales[name] = 1.0/decimalScale.Value;
		}

		public void Start() {
			if(worker != null) 
				throw new InvalidOperationException("Agent already started.");

			worker = new Thread(() => {
				collectedStats = new StatsCollection(config);
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
				catch(Exception ex) {
					collectedStats = null;
					OnError?.Invoke(this, new ErrorEventArgs(ex));
				}
				worker = null;
			});
			worker.Start();
		}
	}}
