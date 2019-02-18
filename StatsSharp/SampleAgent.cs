using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace StatsSharp
{
	public class SampleAgent
	{
		readonly StatsCollection collectedStats = new StatsCollection();

		Thread worker = new Thread(RunWorker);

		public StatsSummary CurrentStats = new StatsSummary(DateTime.UtcNow, new StatsValue[0]);
		public TimeSpan FlushInterval = TimeSpan.FromSeconds(10);
		public TimeSpan SampleInterval = TimeSpan.FromSeconds(1);
		public IStatsClient Stats => collectedStats;

		public event EventHandler<ErrorEventArgs> OnError;
		public event Action<StatsSummary> Flushed;
		public event Action<IStatsClient> Sample;

		public void Start()
		{
			if (worker != null && worker.IsAlive)
				throw new InvalidOperationException("Already started.");
			if (worker == null)
				worker = new Thread(RunWorker);
			worker.Start(this);
		}

		static void RunWorker(object obj)
		{
			var self = (SampleAgent)obj;
			try
			{
				var sampleTime = new Stopwatch();
				var nextFlush = AlignToInterval(DateTime.UtcNow + self.FlushInterval, self.FlushInterval);
				for(; self.worker != null; AwaitNextSample(self.SampleInterval, sampleTime.Elapsed))
				{
					sampleTime.Restart();
					var doSample = self.Sample;
					if (doSample != null)
						doSample(self.Stats);

					if (DateTime.UtcNow < nextFlush)
						continue;

					self.Flush(nextFlush);
					nextFlush += self.FlushInterval;
				}
			}
			catch (Exception ex)
			{
				self.HandleError(ex);
			}
		}

		static void AwaitNextSample(TimeSpan sampleInterval, TimeSpan sampleTime)
		{
			var delay = sampleInterval - sampleTime;
			if (delay <= TimeSpan.Zero)
				return;
			Thread.Sleep(delay);
		}

		public void Stop()
		{
			var x = worker;
			worker = null;
			x.Join();
		}

		public void Flush(DateTime lastFlush)
		{
			CurrentStats = collectedStats.Flush(lastFlush, FlushInterval);
			Flushed?.Invoke(CurrentStats);
		}

		void HandleError(Exception ex)
		{
			var err = OnError;
			if (err == null)
				return;
			var e = new ErrorEventArgs(ex);
			foreach (EventHandler<ErrorEventArgs> handler in err.GetInvocationList())
				try { handler(this, e); } catch { }
		}

		static DateTime AlignToInterval(DateTime now, TimeSpan interval) =>
			now.AddTicks(-now.TimeOfDay.Ticks % interval.Ticks);
	}
}
