// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Fliox.MsgPack.Map
{
    internal static class MsgPackList<T>
    {
        internal static void ReadMsg (ref List<T> list, ref MsgReader reader)
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
            for (int n = 0; n < length; n++) {
                MsgPackMapper<T>.Instance.read(ref item, ref reader);
                list.Add(item);
            }
        }
        
        internal static void WriteMsg(ref List<T> list, ref MsgWriter writer) {
            if (list == null) {
                writer.WriteNull();
                return;
            }
            int length = list.Count;
            writer.WriteArray(length);
            for (int n = 0; n < length; n++) {
                T item = list[n];
                MsgPackMapper<T>.Instance.write(ref item, ref writer);
            }
        }
    }
}