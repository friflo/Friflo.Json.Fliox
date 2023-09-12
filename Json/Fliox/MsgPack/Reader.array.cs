// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Buffers.Binary;

// ReSharper disable ReplaceSliceWithRangeIndexer
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
                case MsgFormat.nil:
                    pos     = cur + 1;
                    length  = -1;
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                    }
                    return false;
                case >= MsgFormat.fixarray and <= MsgFormat.fixarrayMax:
                {
                    pos     = cur + 1;
                    if (pos >= data.Length) {
                        SetEofErrorType(type, cur);
                        length  = -1;
                        return false;
                    }
                    length  = (int)type & 0x0f;
                    return true;
                }
                case MsgFormat.array16: {
                    pos     = cur + 3;       
                    if (pos >= data.Length) {
                        SetEofErrorType(type, cur);
                        length  = -1;
                        return false;
                    }
                    length  = BinaryPrimitives.ReadInt16BigEndian(data.Slice(cur + 1, 2));
                    return true;
                }
                case MsgFormat.array32: {
                    pos     = cur + 5;       
                    if (pos >= data.Length) {
                        SetEofErrorType(type, cur);
                        length  = -1;
                        return false;
                    }
                    length  = BinaryPrimitives.ReadInt32BigEndian(data.Slice(cur + 1, 4));
                    return true;
                }
            }
            SetTypeError("expect array or null", type, cur);
            length = -1;
            return false;
        }
    }
}