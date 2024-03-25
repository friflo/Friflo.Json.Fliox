// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.MsgPack.Map
{
    public static class MsgPackArray
    {
        // --- T[]
        public static void ReadMsg<T> (this ref MsgReader reader, ref T[] array)
        {
            var length = ReadStart(ref reader, ref array);
            T item = default;
            var read = MsgPackMapper<T>.Instance.read;
            for (int n = 0; n < length; n++) {
                read(ref reader, ref item);
                array[n] = item;
            }
        }
        
        public static void WriteMsg<T>(this ref MsgWriter writer, ref T[] array)
        {
            var length = WriteStart(ref writer, ref array);
            var write = MsgPackMapper<T>.Instance.write;
            for (int n = 0; n < length; n++) {
                write(ref writer, ref array[n]);
            }
        }

        // --- int[]
        public static void ReadMsg (this ref MsgReader reader, ref int[] array) {
            var length = ReadStart(ref reader, ref array);
            for (int n = 0; n < length; n++) {
                array[n] = reader.ReadInt32();
            }
        }
        
        public static void WriteMsg(this ref MsgWriter writer, ref int[] array) {
            var length = WriteStart(ref writer, ref array);
            for (int n = 0; n < length; n++) {
                writer.WriteInt32(array[n]);
            }
        }
        
        // --- utils
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
    }
}