// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using static Friflo.Json.Fliox.MsgPack.MsgReaderState;
using static Friflo.Json.Fliox.MsgPack.MsgFormat;

// ReSharper disable ReplaceSliceWithRangeIndexer
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.MsgPack
{

    public ref partial struct MsgReader
    {
        public bool ReadArray(out int length)
        {
            var cur = pos;
            if (cur >= data.Length) {
                length = -1;
                SetEofError(cur);
                return false;
            }
            var type    = (MsgFormat)data[cur];
            switch (type)
            {
                case nil:
                    pos     = cur + 1;
                    length  = -1;
                    return false;
                case >= fixarray and <= fixarrayMax:
                {
                    pos     = cur + 1;
                    length  = (int)type & 0x0f;
                    return true;
                }
                case array16: {
                    pos     = cur + 3;       
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        length  = -1;
                        return false;
                    }
                    length  = BinaryPrimitives.ReadInt16BigEndian(data.Slice(cur + 1, 2));
                    return true;
                }
                case array32: {
                    pos     = cur + 5;       
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        length  = -1;
                        return false;
                    }
                    length  = BinaryPrimitives.ReadInt32BigEndian(data.Slice(cur + 1, 4));
                    return true;
                }
            }
            SetError(ExpectArray, type, cur);
            length = -1;
            return false;
        }
    }
}