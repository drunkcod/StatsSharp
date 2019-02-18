using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace StatsSharp.Diagnostics
{
	public class ProcessMetrics : IDisposable
	{
		class CounterValue : IDisposable
		{
			PerformanceCounter counter;
			long rawValue;

			public CounterValue(PerformanceCounter counter) {
				this.counter = counter;
			}

			public void Dispose() => counter.Dispose();

			public void Update() {
				rawValue = ReadNext();
			}

			public string CounterName => counter.CounterName;

			public object Value {
				get {
					switch (counter.CounterType) {
						default: return BitConverter.Int64BitsToDouble(rawValue);
						case PerformanceCounterType.NumberOfItems64: return rawValue;
						case PerformanceCounterType.ElapsedTime: return TimeSpan.FromTicks(rawValue);
					}
				}
			}

			long ReadNext() {
				switch (counter.CounterType) {
					default: return BitConverter.DoubleToInt64Bits(counter.NextValue());
					case PerformanceCounterType.NumberOfItems64: return counter.NextSample().RawValue;
					case PerformanceCounterType.ElapsedTime: return GetTicks(counter.NextSample());
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

		readonly Dictionary<string, CounterValue> counters = new Dictionary<string, CounterValue>();
		readonly int? pid;
		PerformanceCounter idProcess;

		ProcessMetrics(int? pid, PerformanceCounter idProcess) {
			this.pid = pid;
			this.idProcess = idProcess;
		}

		public object this[string name] {
			get {
				if (counters.TryGetValue(name, out var found))
					return found.Value;
				var p = new CounterValue(CounterByName(name));
				p.Update();
				counters.Add(name, p);
				return p.Value;
			}
		}

		public void Update() {
			if (pid.HasValue && idProcess.NextSample().RawValue != pid) {
				idProcess = FindByPid(pid.Value);
				foreach (var item in counters.Values)
					item.Rebind(CounterByName(item.CounterName));
			}
			foreach (var item in counters.Values)
				item.Update();
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
