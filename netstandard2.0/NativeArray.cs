using Asparlose.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Asparlose.Memory
{
    public sealed class NativeArray<T> : SafeBuffer, IReadOnlyList<T> where T : struct
    {
        /// <summary>
        /// 要素のサイズを取得します。
        /// </summary>
        public static int ElementSize => Marshal.SizeOf<T>();

        private NativeArray() : base(true) { }

        /// <summary>
        /// ストリームの残りの内容を、新しい <see cref="NativeArray{T}"/> に読み込みます。
        /// </summary>
        /// <param name="stream">読み込み元のシーク可能な <see cref="Stream"/>。</param>
        /// <returns>ストリーム内容を読み込んだ、新しい <see cref="NativeArray{T}"/>。</returns>
        public static NativeArray<T> Load(Stream stream)
        {
            var sz = ((stream.Length - stream.Position) + ElementSize - 1) / ElementSize;
            NativeArray<T> z = null;
            try
            {
                z = Alloc((int)sz);
                using (var s = z.CreateStream())
                    stream.CopyTo(s);
                return z;
            }
            catch (Exception)
            {
                z?.Dispose();
                throw;
            }
        }


        /// <summary>
        /// ストリームの残りの内容から、指定した個数の要素を、新しい <see cref="NativeArray{T}"/> に読み込みます。
        /// </summary>
        /// <param name="stream">読み込み元の <see cref="Stream"/>。</param>
        /// <param name="count">読み込む要素の数。</param>
        /// <returns>ストリームの内容を読み込んだ、新しい <see cref="NativeArray{T}"/>。</returns>
        public static NativeArray<T> ReadStream(Stream stream, int count)
        {
            var sz = ElementSize * count;
            NativeArray<T> z = null;
            try
            {
                z = Alloc(sz);
                var buf = new byte[sz];
                stream.Read(buf, 0, sz);
                using (var s = z.CreateStream())
                    s.Write(buf, 0, sz);
                return z;
            }
            catch (Exception)
            {
                z?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 領域を確保します。
        /// </summary>
        /// <remarks>
        /// 確保される領域のサイズは、<paramref name="count"/>×<see cref="ElementSize"/>となります。
        /// </remarks>
        /// <param name="count">確保する要素の数。</param>
        /// <returns>確保された領域。</returns>
        public static NativeArray<T> Alloc(int count)
        {
            var tmp = new NativeArray<T>();

            RuntimeHelpers.PrepareConstrainedRegions();
            try { }
            finally
            {
                tmp.handle = Marshal.AllocCoTaskMem(count * ElementSize);
            }

            tmp.Initialize<T>((uint)count);

            tmp.Count = count;
            GC.AddMemoryPressure((long)tmp.ByteLength);

            return tmp;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            Marshal.FreeCoTaskMem(handle);
            GC.RemoveMemoryPressure((long)ByteLength);
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            lock (streams)
            {
                foreach (var s in streams)
                    s.Dispose();

                streams.Clear();
            }

            base.Dispose(disposing);
        }

        readonly WeakCollection<UnmanagedMemoryStream> streams = new WeakCollection<UnmanagedMemoryStream>();

        /// <summary>
        /// 確保されたメモリにアクセスする <see cref="UnmanagedMemoryStream"/> を作成します。
        /// </summary>
        /// <returns>確保されたメモリにアクセスする <see cref="UnmanagedMemoryStream"/>。</returns>
        public UnmanagedMemoryStream CreateStream()
        {
            if (IsClosed || IsInvalid)
                throw new ObjectDisposedException(ToString());

            lock (streams)
            {
                var stream = new UnmanagedMemoryStream(this, 0, (long)ByteLength, FileAccess.ReadWrite);
                streams.Add(stream);
                return stream;
            }
        }

        /// <inheritdoc />
        public int Count { private set; get; }

        /// <inheritdoc />
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();

                return Read<T>((ulong)(index * ElementSize));
            }
            set
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();

                Write((ulong)(index * ElementSize), value);
            }
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }
}
