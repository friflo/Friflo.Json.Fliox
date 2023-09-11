// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.MsgPack.Map
{
    internal static class MsgPackArray
    {
        // --- T[]
        internal static void ReadMsg<T> (ref MsgReader reader, ref T[] array)
        {
            var length = ReadStart(ref reader, ref array);
            T item = default;
            var read = MsgPackMapper<T>.Instance.read;
            for (int n = 0; n < length; n++) {
                read(ref reader, ref item);
                array[n] = item;
            }
        }
        
        private static int WriteStart<T> (ref MsgWriter writer, ref T[] array)
        {
            if (array == null) {
                writer.WriteNull();
                return 0;
            }
            int length = array.Length;
            writer.WriteArray(length);
            return length;
        }
        
        // --- int[]
        internal static void ReadInt32 (ref MsgReader reader, ref int[] array) {
            var length = ReadStart(ref reader, ref array);
            for (int n = 0; n < length; n++) {
                array[n] = reader.ReadInt32();
            }
        }
        
        internal static void WriteInt32(ref MsgWriter writer, ref int[] array) {
            var length = WriteStart(ref writer, ref array);
            for (int n = 0; n < length; n++) {
                writer.WriteInt32(array[n]);
            }
        }
        
        // --- utils
        internal static void WriteMsg<T>(ref MsgWriter writer, ref T[] array)
        {
            var length = WriteStart(ref writer, ref array);
            var write = MsgPackMapper<T>.Instance.write;
            for (int n = 0; n < length; n++) {
                write(ref writer, ref array[n]);
            }
        }
        
        private static int ReadStart<T> (ref MsgReader reader, ref T[] array)
        {
            if (!reader.ReadArray(out int length)) {
                array = null;
                return 0;
            }
            if (array == null || array.Length != length) {
                array = new T[length];
            }
            return length;
        }
    }
}