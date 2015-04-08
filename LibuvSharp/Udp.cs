using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Runtime.InteropServices;

namespace LibuvSharp
{
	public class Udp : HandleBase, IDatagram<IPEndPoint>, ITrySend<IPEndPoint>
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void recv_start_callback_win(IntPtr handle, IntPtr nread, ref WindowsBufferStruct buf, IntPtr sockaddr, ushort flags);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void recv_start_callback_unix(IntPtr handle, IntPtr nread, ref UnixBufferStruct buf, IntPtr sockaddr, ushort flags);

		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		static extern int uv_udp_init(IntPtr loop, IntPtr handle);

		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		static extern int uv_udp_bind(IntPtr handle, ref sockaddr_in sockaddr, short flags);

		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		static extern int uv_udp_bind(IntPtr handle, ref sockaddr_in6 sockaddr, short flags);

		static recv_start_callback_win recv_start_cb_win;
		static recv_start_callback_unix recv_start_cb_unix;

		static Udp()
		{
			recv_start_cb_win = recv_start_callback_w;
			recv_start_cb_unix = recv_start_callback_u;
		}

		public Udp()
			: this(Loop.Constructor)
		{
		}

		public Udp(Loop loop)
			: base(loop, HandleType.UV_UDP, uv_udp_init)
		{
		}

		bool dualstack = false;
		void Bind(IPAddress ipAddress, int port, short flags)
		{
			CheckDisposed();

			Ensure.AddressFamily(ipAddress);

			dualstack = (flags & (short)uv_udp_flags.UV_UDP_IPV6ONLY) == 0
				&& ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;

			int r;
			if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
				sockaddr_in address = UV.ToStruct(ipAddress.ToString(), port);
				r = uv_udp_bind(NativeHandle, ref address, 0);
			} else {
				sockaddr_in6 address = UV.ToStruct6(ipAddress.ToString(), port);
				r = uv_udp_bind(NativeHandle, ref address , 0);
			}
			Ensure.Success(r);
		}
		public void Bind(int port)
		{
			Bind(IPAddress.IPv6Any, port, 0);
		}
		public void Bind(IPAddress ipAddress, int port)
		{
			Ensure.ArgumentNotNull(ipAddress, "ipAddress");

			short flags;

			switch (ipAddress.AddressFamily) {
			case System.Net.Sockets.AddressFamily.InterNetworkV6:
				flags = (short)uv_udp_flags.UV_UDP_IPV6ONLY;
			break;
			default:
				flags = 0;
				break;
			}
			Bind(ipAddress, port, flags);
		}
		public void Bind(string ipAddress, int port)
		{
			Ensure.ArgumentNotNull(ipAddress, "ipAddress");
			Bind(IPAddress.Parse(ipAddress), port);
		}
		public void Bind(IPEndPoint endPoint)
		{
			Ensure.ArgumentNotNull(endPoint, "endPoint");
			Bind(endPoint.Address, endPoint.Port);
		}

		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		internal extern static int uv_udp_send(IntPtr req, IntPtr handle, WindowsBufferStruct[] bufs, int nbufs, ref sockaddr_in addr, callback callback);
		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		internal extern static int uv_udp_send(IntPtr req, IntPtr handle, UnixBufferStruct[] bufs, int nbufs, ref sockaddr_in addr, callback callback);

		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		internal extern static int uv_udp_send(IntPtr req, IntPtr handle, WindowsBufferStruct[] bufs, int nbufs, ref sockaddr_in6 addr, callback callback);
		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		internal extern static int uv_udp_send(IntPtr req, IntPtr handle, UnixBufferStruct[] bufs, int nbufs, ref sockaddr_in6 addr, callback callback);

		public void Send(IPEndPoint ipEndPoint, ArraySegment<byte> data, Action<Exception> callback)
		{
			CheckDisposed();

			Ensure.ArgumentNotNull(ipEndPoint, "ipEndPoint");
			Ensure.AddressFamily(ipEndPoint.Address);

			GCHandle datagchandle = GCHandle.Alloc(data.Array, GCHandleType.Pinned);
			CallbackPermaRequest cpr = new CallbackPermaRequest(RequestType.UV_UDP_SEND);
			cpr.Callback = (status, cpr2) => {
				datagchandle.Free();
				Ensure.Success(status, callback);
			};

			var ptr = (IntPtr)(datagchandle.AddrOfPinnedObject().ToInt64() + data.Offset);

			int r;
			if (UV.isUnix) {
				UnixBufferStruct[] buf = new UnixBufferStruct[1];
				buf[0] = new UnixBufferStruct(ptr, data.Count);

				if (ipEndPoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
					sockaddr_in address = UV.ToStruct(ipEndPoint.Address.ToString(), ipEndPoint.Port);
					r = uv_udp_send(cpr.Handle, NativeHandle, buf, 1, ref address, CallbackPermaRequest.CallbackDelegate);
				} else {
					sockaddr_in6 address = UV.ToStruct6(ipEndPoint.Address.ToString(), ipEndPoint.Port);
					r = uv_udp_send(cpr.Handle, NativeHandle, buf, 1, ref address, CallbackPermaRequest.CallbackDelegate);
				}
			} else {
				WindowsBufferStruct[] buf = new WindowsBufferStruct[1];
				buf[0] = new WindowsBufferStruct(ptr, data.Count);

				if (ipEndPoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
					sockaddr_in address = UV.ToStruct(ipEndPoint.Address.ToString(), ipEndPoint.Port);
					r = uv_udp_send(cpr.Handle, NativeHandle, buf, 1, ref address, CallbackPermaRequest.CallbackDelegate);
				} else {
					sockaddr_in6 address = UV.ToStruct6(ipEndPoint.Address.ToString(), ipEndPoint.Port);
					r = uv_udp_send(cpr.Handle, NativeHandle, buf, 1, ref address, CallbackPermaRequest.CallbackDelegate);
				}
			}
			Ensure.Success(r);
		}

		[DllImport("uv", EntryPoint = "uv_udp_recv_start", CallingConvention = CallingConvention.Cdecl)]
		extern static int uv_udp_recv_start_win(IntPtr handle, alloc_callback_win alloc_callback, recv_start_callback_win callback);

		[DllImport("uv", EntryPoint = "uv_udp_recv_start", CallingConvention = CallingConvention.Cdecl)]
		extern static int uv_udp_recv_start_unix(IntPtr handle, alloc_callback_unix alloc_callback, recv_start_callback_unix callback);

		static void recv_start_callback_w(IntPtr handlePointer, IntPtr nread, ref WindowsBufferStruct buf, IntPtr sockaddr, ushort flags)
		{
			var handle = FromIntPtr<Udp>(handlePointer);
			handle.recv_start_callback(handlePointer, nread, sockaddr, flags);
		}
		static void recv_start_callback_u(IntPtr handlePointer, IntPtr nread, ref UnixBufferStruct buf, IntPtr sockaddr, ushort flags)
		{
			var handle = FromIntPtr<Udp>(handlePointer);
			handle.recv_start_callback(handlePointer, nread, sockaddr, flags);
		}
		void recv_start_callback(IntPtr handle, IntPtr size, IntPtr sockaddr, ushort flags)
		{
			long nread = size.ToInt64();

			if (nread == 0) {
				return;
			} else {
				var req = readRequests.Dequeue();
				req.gchandle.Free();
				if (nread < 0) {
					if (UVException.Map((int)nread) == UVErrorCode.EOF) {
						if (req.ucb != null) {
							req.ucb(null, null);
						}
					} else {
						if (req.ucb != null) {
							req.ucb(Ensure.Map((int)nread), null);
						}
					}
					Close();
				} else {
					if (req.ucb != null) {
						var ep = UV.GetIPEndPoint(sockaddr);

						if (dualstack && ep.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) {
							var data = ep.Address.GetAddressBytes();
							if (IsMapping(data)) {
								ep = new IPEndPoint(GetMapping(data), ep.Port);
							}
						}

						req.ucb(
							null,
							new UdpReceiveMessage(
								UV.GetIPEndPoint(sockaddr),
								new ArraySegment<byte>(req.buf.Array, req.buf.Offset, size.ToInt32()),
								(flags & (short)uv_udp_flags.UV_UDP_PARTIAL) > 0
							)
						);
					}
				}

				if (readRequests.Count <= 0) {
					Pause();
				}
			}
		}

		public void Receive(ArraySegment<byte> buffer, Action<Exception, UdpMessage> message)
		{
			CheckDisposed();

			readRequests.Enqueue(new ReadRequest() {
				buf = buffer,
				ucb = message
			});

			if (readRequests.Count == 1) {
				// TODO: quick hack, investigate this, Fibonacci example
				// is crashing if I do not have this
				try { Resume(); } catch { }
			}
		}

		bool IsMapping(byte[] data)
		{
			if (data.Length != 16) {
				return false;
			}

			for (int i = 0; i < 10; i++) {
				if (data[i] != 0) {
					return false;
				}
			}

			return data[10] == data[11] && data[11] == 0xff;
		}

		IPAddress GetMapping(byte[] data)
		{
			var ip = new byte[4];
			for (int i = 0; i < 4; i++) {
				ip[i] = data[12 + i];
			}
			return new IPAddress(data);
		}

		void Resume()
		{
			CheckDisposed();

			int r;
			if (UV.isUnix) {
				r = uv_udp_recv_start_unix(NativeHandle, alloc_unix, recv_start_cb_unix);
			} else {
				r = uv_udp_recv_start_win(NativeHandle, alloc_win, recv_start_cb_win);
			}
			Ensure.Success(r);
		}

		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		internal extern static int uv_udp_recv_stop(IntPtr handle);

		void Pause()
		{
			Invoke(uv_udp_recv_stop);
		}

		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		static extern int uv_udp_set_ttl(IntPtr handle, int ttl);

		public byte TTL
		{
			set {
				Invoke(uv_udp_set_ttl, (int)value);
			}
		}

		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		static extern int uv_udp_set_broadcast(IntPtr handle, int on);

		public bool Broadcast {
			set {
				Invoke(uv_udp_set_broadcast, value ? 1 : 0);
			}
		}

		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		static extern int uv_udp_set_multicast_ttl(IntPtr handle, int ttl);

		public byte MulticastTTL {
			set {
				Invoke(uv_udp_set_multicast_ttl, (int)value);
			}
		}

		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		static extern int uv_udp_set_multicast_loop(IntPtr handle, int on);

		public bool MulticastLoop {
			set {
				Invoke(uv_udp_set_multicast_loop, value ? 1 : 0);
			}
		}


		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		static extern int uv_udp_getsockname(IntPtr handle, IntPtr addr, ref int length);

		public IPEndPoint LocalAddress {
			get {
				CheckDisposed();

				return UV.GetSockname(this, uv_udp_getsockname);
			}
		}

		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		internal extern static int uv_udp_try_send(IntPtr handle, WindowsBufferStruct[] bufs, int nbufs, ref sockaddr_in addr);
		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		internal extern static int uv_udp_try_send(IntPtr handle, UnixBufferStruct[] bufs, int nbufs, ref sockaddr_in addr);

		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		internal extern static int uv_udp_try_send(IntPtr handle, WindowsBufferStruct[] bufs, int nbufs, ref sockaddr_in6 addr);
		[DllImport("uv", CallingConvention = CallingConvention.Cdecl)]
		internal extern static int uv_udp_try_send(IntPtr handle, UnixBufferStruct[] bufs, int nbufs, ref sockaddr_in6 addr);

		unsafe public int TrySend(IPEndPoint ipEndPoint, ArraySegment<byte> data)
		{
			Ensure.ArgumentNotNull(ipEndPoint, "ipEndPoint");
			Ensure.AddressFamily(ipEndPoint.Address);

			fixed (byte* bytePtr = data.Array) {
				var ptr = (IntPtr)bytePtr + data.Offset;
				int r;
				if (UV.isUnix) {
					UnixBufferStruct[] buf = new UnixBufferStruct[1];
					buf[0] = new UnixBufferStruct(ptr, data.Count);

					if (ipEndPoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
						sockaddr_in address = UV.ToStruct(ipEndPoint.Address.ToString(), ipEndPoint.Port);
						r = uv_udp_try_send(NativeHandle, buf, 1, ref address);
					} else {
						sockaddr_in6 address = UV.ToStruct6(ipEndPoint.Address.ToString(), ipEndPoint.Port);
						r = uv_udp_try_send(NativeHandle, buf, 1, ref address);
					}
				} else {
					WindowsBufferStruct[] buf = new WindowsBufferStruct[1];
					buf[0] = new WindowsBufferStruct(ptr, data.Count);

					if (ipEndPoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
						sockaddr_in address = UV.ToStruct(ipEndPoint.Address.ToString(), ipEndPoint.Port);
						r = uv_udp_try_send(NativeHandle, buf, 1, ref address);
					} else {
						sockaddr_in6 address = UV.ToStruct6(ipEndPoint.Address.ToString(), ipEndPoint.Port);
						r = uv_udp_try_send(NativeHandle, buf, 1, ref address);
					}
				}
				return r;
			}
		}
	}
}

