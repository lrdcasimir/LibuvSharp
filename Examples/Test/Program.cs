using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LibuvSharp;
using LibuvSharp.Threading;
using LibuvSharp.Threading.Tasks;

namespace Test
{
	class MainClass
	{

		public static async Task SimpleTestServerAsync<TEndPoint, TListener, TClient>(TEndPoint endPoint)
			where TListener : IBindable<TListener, TEndPoint>, IListener<TClient>, IDisposable, new()
			where TClient : IUVStream, IDisposable, new()
		{
			using (var server = new TListener()) {
				server.Bind(endPoint);
				server.Listen();
				using (var client = await server.AcceptAsync()) {
					var str = await client.ReadStringAsync();
					if (str != null && str == "PING") {
						client.Write("PONG");
						await client.ShutdownAsync();
					}
				}
			}
		}

		public static async Task SimpleTestClientAsync<TEndPoint, TClient>(TEndPoint endPoint)
			where TClient : IConnectable<TClient, TEndPoint>, IUVStream, IDisposable, new()
		{
			using (var client = new TClient()) {
				await client.ConnectAsync(endPoint);

				client.Write("PING");
				var str = await client.ReadStringAsync();
				if (str == null) {
					throw new Exception("Shouldn't be null");
				} else if (str != "PONG") {
					throw new Exception("Should be PONG");
				}
				await client.ShutdownAsync();
			}
		}

		public static async Task SimpleTestAsync<TEndPoint, TListener, TClient>(TEndPoint endPoint)
			where TListener : IBindable<TListener, TEndPoint>, IListener<TClient>, IDisposable, new()
			where TClient : IConnectable<TClient, TEndPoint>, IUVStream, IDisposable, new()
		{
			await Task.WhenAll(
				SimpleTestServerAsync<TEndPoint, TListener, TClient>(endPoint),
				SimpleTestClientAsync<TEndPoint, TClient>(endPoint)
			);
		}

		public static void Main(string[] args)
		{
			Loop.Default.Run(async () => {
				var name = "pipeserver";
				try {
					await Task.Run(() => System.IO.File.Delete(name));
				} catch { }
				await SimpleTestAsync<string, PipeListener, Pipe>(name);
			});
		}
	}
}
