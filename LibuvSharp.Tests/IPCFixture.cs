﻿using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using LibuvSharp.Threading;
using LibuvSharp.Threading.Tasks;
using Xunit;
using Xunit.Extensions;

namespace LibuvSharp.Tests
{
	public class IPCFixture
	{
		[Fact]
		public void CanSendHandles()
		{
			TestCanSendHandles(Default.Pipename, Default.IPv4.IPEndPoint);
			TestCanSendHandles(Default.Pipename, Default.IPv6.IPEndPoint);

			TestCanSendHandlesMix(Default.Pipename, Default.IPv4.IPEndPoint);
			TestCanSendHandlesMix(Default.Pipename, Default.IPv6.IPEndPoint);
		}

		void TestCanSendHandles(string pipename, IPEndPoint ipep)
		{
			int count = 0;

			Loop.Default.Run(async () => {
				var handles = new Stack<Handle>();
				var pipelistener = new IPCPipeListener();
				pipelistener.Bind(pipename);
				var buffer = new ArraySegment<byte>(new byte[8 * 1024]);
				pipelistener.Connection += () => {
					var client = pipelistener.Accept();
					Action<Exception, int> OnData = null;
					OnData = (exception, nread) => {
						while (client.PendingCount > 0) {
							var handle = client.Accept();
							handles.Push(handle);
							count++;
							if (count == 3) {
								foreach (var h in handles) {
									h.Close();
								}
								pipelistener.Close();
							} else {
								client.Read(buffer, OnData);
							}
						}
					};
					client.Read(buffer, OnData);
				};
				pipelistener.Listen();

				var pipe = new IPCPipe();
				await pipe.ConnectAsync(pipename);

				var tcplistener = new TcpListener();
				tcplistener.Bind(ipep);
				tcplistener.Connection += () => {
					var client = tcplistener.Accept();
					pipe.Write(client, new byte[1], (ex) => {
						client.Close();
						tcplistener.Close();
					});
				};
				tcplistener.Listen();

				var tcp = new Tcp();
				await tcp.ConnectAsync(ipep);
				tcp.Write("HELLO WORLD");

				var udp = new Udp();
				udp.Bind(ipep);
				pipe.Write(udp, Encoding.Default.GetBytes("UDP"), (ex) => udp.Close());
				pipe.Write(pipe, Encoding.Default.GetBytes("pipe"), (ex) => pipe.Close());
			});

			Assert.Equal(3, count);
		}
		static void TestCanSendHandlesMix(string pipename, IPEndPoint ipep)
		{
			Loop.Default.Run(async () => {
				await Task.Run(() => File.Delete(pipename));

				var handles = new Stack<Handle>();
				var pipelistener = new IPCPipeListener();
				pipelistener.Bind(pipename);
				pipelistener.Connection += async () => {
					using (var client = pipelistener.Accept()) {
						var buffer = new ArraySegment<byte>(new byte[1024]);
						do {
							int n = await client.ReadAsync(buffer);
							var str = Encoding.Default.GetString(buffer.Take(n));
							var handle = client.Accept();
							Assert.Equal(str, handle.GetType().ToString().Split('.').Last());
							handles.Push(handle);
						} while (handles.Count != 3);
					}
					foreach (var handle in handles) {
						handle.Dispose();
					}
					pipelistener.Dispose();
				};
				pipelistener.Listen();

				using (var pipe = new IPCPipe()) {
					await pipe.ConnectAsync(pipename);

					var tcplistener = new TcpListener();
					tcplistener.Bind(ipep);
					tcplistener.Connection += async () => {
						using (var client = tcplistener.Accept()) {
							await pipe.WriteAsync(client, "Tcp");
						}
						tcplistener.Close();
					};
					tcplistener.Listen();

					var tcp = new Tcp();
					await tcp.ConnectAsync(ipep);
					tcp.Write("HELLO WORLD");

					using (var udp = new Udp()) {
						udp.Bind(ipep);
						await pipe.WriteAsync(udp, "Udp");
					}
					await pipe.WriteAsync(pipe, "Pipe");
				}
			});
		}

	}
}

