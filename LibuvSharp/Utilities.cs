using System;
using System.Threading.Tasks;

namespace LibuvSharp.Utilities
{
	public static class UtilitiesExtensions
	{
		/*
		public static Task PumpAsync(this IUVStream readStream, IUVStream writeStream)
		{
			return HelperFunctions.Wrap(writeStream, readStream.Pump);
		}

		public static void Pump(this IUVStream readStream, IUVStream writeStream)
		{
			Pump(readStream, writeStream, null);
		}

		public static void Pump(this IUVStream readStream, IUVStream writeStream, Action<Exception> callback)
		{
			bool pending = false;
			bool done = false;

			Action<Exception> call = null;
			Action complete = () => call(null);

			call = (ex) => {
				if (done) {
					return;
				}

				readStream.Error -= call;
				readStream.Complete -= complete;

				done = true;
				if (callback != null) {
					callback(ex);
				}
			};

			readStream.Data += ((data) => {
				writeStream.Write(data, null);
				if (writeStream.WriteQueueSize > 0) {
					pending = true;
					readStream.Pause();
				}
			});

			writeStream.Drain += () => {
				if (pending) {
					pending = false;
					readStream.Resume();
				}
			};

			readStream.Error += call;
			readStream.Complete += complete;

			readStream.Resume();
		}
		*/
	}
}

