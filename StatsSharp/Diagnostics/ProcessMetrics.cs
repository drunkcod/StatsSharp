using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace StatsSharp.Diagnostics
{
	public class ProcessMetrics : IDisposable
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

			public Metric Read() => new Metric(key, ReadNext());

			MetricValue ReadNext() {
				switch (counter.CounterType) {
					default: return MetricValue.Time(counter.NextValue());
					case PerformanceCounterType.NumberOfItems64: return MetricValue.Time(counter.NextSample().RawValue);
				}
			}

			public void Rebind(PerformanceCounter newCounter) {
				counter.Dispose();
				counter = newCounter;
			}

			static long GetTicks(CounterSample sample) {
				var ticks = sample.CounterTimeStamp - sample.RawValue;
				if (sample.CounterFrequency == TimeSpan.TicksPerSecond)
					return ticks;
				return TimeSpan.FromSeconds(1.0 * ticks / sample.CounterFrequency).Ticks;
			}
		}

		readonly Dictionary<string, ProcessCounter> counters = new Dictionary<string, ProcessCounter>();
		readonly int? pid;
		PerformanceCounter idProcess;

		ProcessMetrics(int? pid, PerformanceCounter idProcess) {
			this.pid = pid;
			this.idProcess = idProcess;
		}

		public bool Add(string metricName, string counterName) {
			if (counters.TryGetValue(metricName, out var found))
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

		public static ProcessMetrics Create() => Create(Process.GetCurrentProcess().Id);

		public static ProcessMetrics Create(int pid) =>
			new ProcessMetrics(pid, FindByPid(pid));

		public static ProcessMetrics Create(string name, string machine) =>
			new ProcessMetrics(null, new PerformanceCounter("Process", "ID Process", name, machine));

		static PerformanceCounter FindByPid(int pid) {
			string processName;
			using (var wmi = new ManagementObjectSearcher($"select * from Win32_PerfRawData_PerfProc_Process where IDProcess = {pid}"))
			{
				wmi.Options.Rewindable = false;
				wmi.Options.ReturnImmediately = true;
				processName = (string)wmi.Get().Cast<ManagementObject>().Single().Properties["Name"].Value;
			}
			return new PerformanceCounter("Process", "ID Process", processName, readOnly: true);
		}

		public string HostName => idProcess.MachineName == "." ? Environment.MachineName : idProcess.MachineName;

		public struct HostInfoItem
		{
			public readonly Type Type;
			public readonly object Value;

			public HostInfoItem(Type type, object value) {
				this.Type = type;
				this.Value = value;
			}
		}

		public HostInfoItem GetHostInfo(string key) {
			using (var wmi = new ManagementObjectSearcher($@"\\{idProcess.MachineName}\root\cimv2", $"select {key} from Win32_ComputerSystem"))
			{
				wmi.Options.Rewindable = false;
				wmi.Options.ReturnImmediately = true;
				var item = wmi.Get().Cast<ManagementObject>().Single().Properties.Cast<PropertyData>().Single();
				var type = ToType(item.Type);
				return new HostInfoItem(!item.IsArray ? type : type.MakeArrayType(), item.Value);
			}
		}

		static Type ToType(CimType type) {
			switch (type) {
				default: throw new NotImplementedException($"{type}");
				case CimType.Boolean: return typeof(bool);
				case CimType.SInt8: return typeof(sbyte);
				case CimType.UInt8: return typeof(byte);
				case CimType.DateTime: return typeof(DateTime);
				case CimType.SInt16: return typeof(short);
				case CimType.UInt16: return typeof(ushort);
				case CimType.SInt32: return typeof(int);
				case CimType.UInt32: return typeof(uint);
				case CimType.SInt64: return typeof(long);
				case CimType.UInt64: return typeof(ulong);
				case CimType.String: return typeof(string);
			}
		}

		public void Dispose() {
			foreach (var item in counters.Values)
				item.Dispose();
			counters.Clear();
		}
	}
}
