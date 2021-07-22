using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace StatsSharp
{
	public class SampleAgent
	{
		readonly StatsCollection collectedStats = new();
		Thread worker = null;

		public StatsSummary CurrentStats = new(DateTime.UtcNow, Array.Empty<StatsValue>());
		public TimeSpan FlushInterval = TimeSpan.FromSeconds(10);
		public TimeSpan SampleInterval = TimeSpan.FromSeconds(1);
		public IStatsClient Stats => collectedStats;

		public event EventHandler<ErrorEventArgs> OnError;
		public event Action<IStatsClient> Flushing;
		public event Action<StatsSummary> Flushed;
		public event Action<IStatsClient> Sample;

		public void Start() {
			if (worker != null && worker.IsAlive)
				throw new InvalidOperationException("Already started.");
			if (worker == null)
				worker = new Thread(RunWorker) {
					IsBackground = true,
					Name = nameof(SampleAgent),
				};
			worker.Start(this);
		}

		static void RunWorker(object obj) {
			var self = (SampleAgent)obj;
			try {
				var sampleTime = new Stopwatch();
				var nextFlush = AlignToInterval(DateTime.UtcNow + self.FlushInterval, self.FlushInterval);
				while(self.worker != null) {
					sampleTime.Restart();
					self.ReadSample();

					if (DateTime.UtcNow >= nextFlush) {
						self.Flush(nextFlush);
						nextFlush += self.FlushInterval;
					}

					AwaitNextSample(self.SampleInterval, sampleTime.Elapsed);
				}
			}
			catch (Exception ex) {
				self.HandleError(ex);
			}
		}

		void ReadSample() => Invoke(Sample, Stats);
		
		static void AwaitNextSample(TimeSpan sampleInterval, TimeSpan sampleTime) {
			var delay = sampleInterval - sampleTime;
			if (delay <= TimeSpan.Zero)
				return;
			Thread.Sleep(delay);
		}

		public void Stop() {
			var x = worker;
			worker = null;
			x.Join();
		}

		public void Flush(DateTime lastFlush) {
			Invoke(Flushing, collectedStats);
			CurrentStats = collectedStats.Flush(lastFlush, FlushInterval);
			Invoke(Flushed, CurrentStats);
		}

		void Invoke<T>(Action<T> action, T args) {
			if(action == null)
				return;

			try { 
				action(args); 
			} catch(Exception ex) {
				HandleError(ex);
			}
		}

		internal void HandleError(Exception ex) {
			var err = OnError;
			if (err == null)
				return;
			var e = new ErrorEventArgs(ex);
			foreach (EventHandler<ErrorEventArgs> handler in err.GetInvocationList())
				try { handler(this, e); } catch { }
		}

		static DateTime AlignToInterval(DateTime now, TimeSpan interval) =>
			now.AddTicks(-(now.TimeOfDay.Ticks % interval.Ticks));
	}
}
