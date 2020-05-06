using System;
using Cone;
using Cone.Helpers;
using Xunit;

namespace StatsSharp.Specs
{
	public class SampleAgentSpec
	{
		[Fact]
		public void handles_flush_error() {
			var error = new InvalidOperationException();
			var onError = new EventSpy<System.IO.ErrorEventArgs>((_, x) => Check.That(() => x.GetException() == error));
			var agent = new SampleAgent();

			agent.OnError += onError;
			agent.Flushed += _ => throw error;

			agent.Flush(DateTime.Now);
			Check.That(() => onError.HasBeenCalled);
		}

		[Fact]
		public void start_stop() {
			var agent = new SampleAgent();

			agent.Start();
			agent.Stop();
		}
	}
}
