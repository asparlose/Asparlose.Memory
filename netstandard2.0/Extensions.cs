using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Asparlose.Memory
{
    public static class Extensions
    {
        public static SafeBufferPointer Acquire(this SafeBuffer safeBuffer)
            => SafeBufferPointer.Acquire(safeBuffer);
    }
}
