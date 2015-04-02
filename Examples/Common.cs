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

	public static bool IsEqual(this ArraySegment<byte> segment1, byte[] array)
	{
		return segment1.IsEqual(array.ToArraySegment());
	}

	public static bool IsEqual<T>(this ArraySegment<T> segment1, ArraySegment<T> segment2)
	{
		int n = segment1.Count;
		if (n != segment2.Count) {
			return false;
		}

		for (int i = 0; i < n; i++) {
			var i1 = segment1.Offset + i;
			var i2 = segment2.Offset + i;
			if (!segment1.Array[i1].Equals(segment2.Array[i2])) {
				return false;
			}
		}
		return true;
	}

	public static bool IsPrefixOf<T>(this ArraySegment<T> segment1, ArraySegment<T> segment2)
	{
		return segment2.Take(segment1.Count).IsEqual(segment1);
	}

	public static bool IsPrefixOf<T>(this T[] array, ArraySegment<T> segment)
	{
		return IsPrefixOf<T>(array.ToArraySegment<T>(), segment);
	}

	static void Swap<T>(ref T first, ref T second)
	{
		var tmp = first;
		first = second;
		second = tmp;
	}

	public static ArraySegment<T> Reverse<T> (this ArraySegment<T> segment)
	{
		for (int i = 0; i < segment.Count/2; i++) {
			Swap(ref segment.Array[segment.Offset + i], ref segment.Array[segment.Offset + segment.Count - 1 - i]);
		}
		return segment;
	}

	public static IEnumerable<ArraySegment<T>> Slice<T>(this ArraySegment<T> segment, int size)
	{
		while (segment.Count >= size) {
			yield return segment.Take(size);
			segment = segment.Skip(size);
		}

		if (!segment.IsEmpty()) {
			yield return segment;
		}
	}

	public static bool ContainsOnly(this ArraySegment<byte> segment, byte value)
	{
		foreach (var s in segment) {
			if (s != value) {
				return false;
			}
		}
		return true;
	}

	public static int FindFirst(this ArraySegment<byte> segment, byte value)
	{
		for (int i = 0; i < segment.Count; i++) {
			if (segment.Array[segment.Offset + i] == value) {
				return i;
			}
		}
		return -1;
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
