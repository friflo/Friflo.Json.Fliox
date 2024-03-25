// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;
using static Friflo.Json.Fliox.MsgPack.MsgReaderState;
using static Friflo.Json.Fliox.MsgPack.MsgFormat;

// ReSharper disable ReplaceSliceWithRangeIndexer
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.MsgPack
{

    public ref partial struct MsgReader
    {
        public ReadOnlySpan<byte> ReadBin()
        {
            var cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return default;
            }
            var type    = (MsgFormat)data[cur];
            switch (type)
            {
                case nil:
                    pos = cur + 1;
                    return default;
                case bin8:
                {
                    if (cur + 1 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return default;
                    }
                    int len = data[cur + 1];
                    return read_bin(cur + 2, len, type);
                }
                case bin16: {
                    if (cur + 2 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return default;
                    }
                    int len = BinaryPrimitives.ReadInt16BigEndian(data.Slice(cur + 1, 2));
                    return read_bin(cur + 3, len, type);
                }
                case bin32: {
                    if (cur + 4 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return default;
                    }
                    int len = BinaryPrimitives.ReadInt32BigEndian(data.Slice(cur + 1, 4));
                    return read_bin(cur + 5, len, type);
                }
            }
            SetError(ExpectByteArray, type, cur);
            return default;
        }
    }
}