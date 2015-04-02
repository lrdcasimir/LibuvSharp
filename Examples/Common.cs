using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using LibuvSharp;
using LibuvSharp.Threading;
using LibuvSharp.Threading.Tasks;
using System.Security.Cryptography;

public static class Default
{
	public static IPEndPoint IPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7000);
}

static class AsyncExtensions
{
	public static async Task<string> ReadStringAsync(this IUVStream stream)
	{
		return await ReadStringAsync(stream, Encoding.Default);
	}

	public static async Task<string> ReadStringAsync(this IUVStream stream, Encoding encoding)
	{
		if (encoding == null) {
			throw new ArgumentException("encoding");
		}

		var buf = new byte[1024];
		var n = await stream.ReadAsync(buf);
		if (n == 0) {
			return null;
		}
		return encoding.GetString(buf, 0, n);
	}
}

public static class EncodingExtensions
{
	public static string GetString(this Encoding encoding, ArraySegment<byte> segment)
	{
		return encoding.GetString(segment.Array, segment.Offset, segment.Count);
	}
}

static class TcpClientExtensions
{
	public static async Task ConnectAsync(this TcpClient client, IPEndPoint ipEndPoint)
	{
		await client.ConnectAsync(ipEndPoint.Address, ipEndPoint.Port);
	}
}

public static class HexExtensions
{
	public static string ToHex(this byte[] bytes)
	{
		return String.Join(string.Empty, Array.ConvertAll(bytes, x => x.ToString("x2")));
	}

	public static string ToHex(this ArraySegment<byte> segment)
	{
		return String.Join(String.Empty, segment.Select((x) => x.ToString("x2")));
	}
}

public static class HashAlgorithmExtensions
{
	public static void TransformBlock(this HashAlgorithm hashAlgorithm, byte[] input, byte[] outputBuffer, int outputOffset)
	{
		hashAlgorithm.TransformBlock(input, 0, input.Length, outputBuffer, outputOffset);
	}

	public static void TransformBlock(this HashAlgorithm hashAlgorithm, byte[] input, byte[] outputBuffer)
	{
		hashAlgorithm.TransformBlock(input, 0, input.Length, outputBuffer, 0);
	}

	public static void TransformBlock(this HashAlgorithm hashAlgorithm, ArraySegment<byte> input, byte[] outputBuffer, int outputOffset)
	{
		hashAlgorithm.TransformBlock(input.Array, input.Offset, input.Count, outputBuffer, outputOffset);
	}

	public static void TransformBlock(this HashAlgorithm hashAlgorithm, ArraySegment<byte> input)
	{
		hashAlgorithm.TransformBlock(input, null, 0);
	}

	public static void TransformFinalBlock(this HashAlgorithm hashAlgorithm, ArraySegment<byte> input)
	{
		hashAlgorithm.TransformFinalBlock(input.Array, input.Offset, input.Count);
	}

	static byte[] emptyBuffer = new byte[0];

	public static void TransformFinalBlock(this HashAlgorithm hashAlgorithm)
	{
		hashAlgorithm.TransformFinalBlock(emptyBuffer, 0, 0);
	}

	public static void TransformFinalBlock(this HashAlgorithm hashAlgorithm, byte[] buffer)
	{
		hashAlgorithm.TransformFinalBlock(buffer, 0, buffer.Length);
	}
}

public static class ArraySegmentExtensions
{
	public static bool IsEmpty<T>(this ArraySegment<T> segment)
	{
		return segment == default(ArraySegment<T>);
	}

	public static ArraySegment<T> Skip<T>(this ArraySegment<T> segment, int count)
	{
		if (segment.Count - count == 0) {
			return default(ArraySegment<T>);
		}

		return new ArraySegment<T>(segment.Array, segment.Offset + count, segment.Count - count);
	}

	public static ArraySegment<T> SkipLast<T>(this ArraySegment<T> segment, int count)
	{
		return new ArraySegment<T>(segment.Array, segment.Offset, segment.Count - count);
	}

	public static ArraySegment<T> Take<T>(this ArraySegment<T> segment, int count)
	{
		return new ArraySegment<T>(segment.Array, segment.Offset, count);
	}

	public static ArraySegment<T> Take<T>(this ArraySegment<T> segment, int skip, int count)
	{
		return segment.Skip(skip).Take(count);
	}

	public static ArraySegment<T> TakeLast<T>(this ArraySegment<T> segment, int count)
	{
		return new ArraySegment<T>(segment.Array, segment.Offset + segment.Count - count, count);
	}

	public static ArraySegment<T> ToArraySegment<T>(this T[] array)
	{
		return new ArraySegment<T>(array);
	}

	public static ArraySegment<T> ToArraySegment<T>(this T[] array, int offset = -1, int count = -1)
	{
		if (offset == -1) {
			offset = 0;
		}
		if (count == -1) {
			count = array.Length;
		}
		return new ArraySegment<T>(array, offset, count);
	}
}
