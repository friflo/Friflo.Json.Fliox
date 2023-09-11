// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Fliox.MsgPack.Map
{
    internal static class MsgPackList
    {
        internal static void ReadMsg<T> (ref MsgReader reader, ref List<T> list)
        {
            var length  = ReadStart(ref reader, ref list);
            T item      = default;
            var read = MsgPackMapper<T>.Instance.read;
            for (int n = 0; n < length; n++) {
                read(ref reader, ref item);
                list.Add(item);
            }
        }
        
        private static int ReadStart<T> (ref MsgReader reader, ref List<T> list)
        {
            if (!reader.ReadArray(out int length)) {
                list = null;
                return 0;
            }
            if (list == null) {
                list = new List<T>(length);
            } else {
                list.Clear();
            }
            return length;
        }
        
        internal static void WriteMsg<T> (ref MsgWriter writer, ref List<T> list)
        {
            var length  = WriteStart (ref writer, ref list);
            var write   = MsgPackMapper<T>.Instance.write;
            for (int n = 0; n < length; n++) {
                T item = list[n];
                write(ref writer, ref item);
            }
        }
        
        private static int WriteStart<T> (ref MsgWriter writer, ref List<T> list)
        {
            if (list == null) {
                writer.WriteNull();
                return 0;
            }
            int length = list.Count;
            writer.WriteArray(length);
            return length;
        }
        
        internal static void ReadInt32 (ref MsgReader reader, ref List<int> list)
        {
            var length = ReadStart(ref reader, ref list);
            for (int n = 0; n < length; n++) {
                list.Add(reader.ReadInt32());
            }
        }
        
        internal static void WriteInt32 (ref MsgWriter writer, ref List<int> list)
        {
            var length = WriteStart (ref writer, ref list);
            for (int n = 0; n < length; n++) {
                writer.WriteInt32(list[n]);
            }
        }
    }
}