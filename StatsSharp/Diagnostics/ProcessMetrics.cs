using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace StatsSharp.Diagnostics
{
#if NET5_0_OR_GREATER
	using System.Runtime.Versioning;
	[SupportedOSPlatform("windows")]
#endif
	public sealed class ProcessMetrics : IDisposable
	{
		class ProcessCounter : IDisposable
		{
			readonly string key;
			PerformanceCounter counter;

			public ProcessCounter(string key, PerformanceCounter counter) {
				this.key = key;
				this.counter = counter;
			}

			public string CounterName => counter.CounterName;

			public void Dispose() => counter.Dispose();

			public Metric Read() => new(key, ReadNext());

			MetricValue ReadNext() =>
				counter.CounterType switch {
					PerformanceCounterType.NumberOfItems64 => MetricValue.Time(counter.NextSample().RawValue),
					_ => MetricValue.Time(counter.NextValue()),
				};

			public void Rebind(PerformanceCounter newCounter) {
				counter.Dispose();
				counter = newCounter;
			}
		}

		readonly Dictionary<string, ProcessCounter> counters = new();
		readonly int? pid;
		PerformanceCounter idProcess;

		ProcessMetrics(int? pid, PerformanceCounter idProcess) {
			this.pid = pid;
			this.idProcess = idProcess;
		}

		public bool Add(string metricName, string counterName) {
			if (counters.TryGetValue(metricName, out _))
				return false;
			var p = new ProcessCounter(metricName, CounterByName(counterName));
			counters.Add(metricName, p);
			return true;
		}

		public void Read(Action<IEnumerable<Metric>> send) {
			if (pid.HasValue && idProcess.NextSample().RawValue != pid) {
				idProcess = FindByPid(pid.Value);
				foreach (var item in counters.Values)
					item.Rebind(CounterByName(item.CounterName));
			}
			send(counters.Values.Select(x => x.Read()));
		}

		PerformanceCounter CounterByName(string name) => IsLocalCounter
			? new PerformanceCounter(idProcess.CategoryName, name, idProcess.InstanceName, readOnly: true)
			: new PerformanceCounter(idProcess.CategoryName, name, idProcess.InstanceName, idProcess.MachineName);

		bool IsLocalCounter => idProcess.MachineName == ".";

#if NET5_0_OR_GREATER
		public static ProcessMetrics Create() => Create(Environment.ProcessId);
#else
		public static ProcessMetrics Create() => Create(Process.GetCurrentProcess().Id);
#endif
		public static ProcessMetrics Create(int pid) => new(pid, FindByPid(pid));

		public static ProcessMetrics Create(string name, string machine) =>
			new(null, new PerformanceCounter("Process", "ID Process", name, machine));

		static PerformanceCounter FindByPid(int pid) {
			var processCounters = new PerformanceCounterCategory("Process");
			var values = processCounters.ReadCategory();

			return new PerformanceCounter("Process", "ID Process", values["ID Process"].Values.Cast<InstanceData>().Single(x => x.RawValue == pid).InstanceName , readOnly: true);
		}

		public string HostName => idProcess.MachineName == "." ? Environment.MachineName : idProcess.MachineName;

		public void Dispose() {
			foreach (var item in counters.Values)
				item.Dispose();
			counters.Clear();
		}
	}
}
