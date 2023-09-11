// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Fliox.MsgPack.Map
{
    internal static class MsgPackList
    {
        internal static void ReadMsg<T> (ref MsgReader reader, ref List<T> list)
        {
            if (!reader.ReadArray(out int length)) {
                list = null;
                return;
            }
            if (list == null) {
                list = new List<T>(length);
            } else {
                list.Clear();
            }
            T item = default;
            var read = MsgPackMapper<T>.Instance.read;
            for (int n = 0; n < length; n++) {
                read(ref reader, ref item);
                list.Add(item);
            }
        }
        
        internal static void WriteMsg<T> (ref MsgWriter writer, ref List<T> list) {
            if (list == null) {
                writer.WriteNull();
                return;
            }
            int length = list.Count;
            writer.WriteArray(length);
            var write = MsgPackMapper<T>.Instance.write;
            for (int n = 0; n < length; n++) {
                T item = list[n];
                write(ref writer, ref item);
            }
        }
    }
}