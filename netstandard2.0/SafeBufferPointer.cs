using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Asparlose.Memory
{
    public sealed class SafeBufferPointer : IDisposable
    {
        readonly SafeBuffer buffer;
        private SafeBufferPointer(SafeBuffer buffer)
        {
            this.buffer = buffer;

            unsafe
            {
                byte* p = null;
                buffer.AcquirePointer(ref p);
                Pointer = new IntPtr(p);
            }
        }

        public IntPtr Pointer { get; }

        public static SafeBufferPointer Acquire(SafeBuffer b) => new SafeBufferPointer(b);

        public void Dispose()
        {
            buffer.ReleasePointer();
        }

        ~SafeBufferPointer()
        {
            GC.SuppressFinalize(this);
            Dispose();
        }
    }
}
