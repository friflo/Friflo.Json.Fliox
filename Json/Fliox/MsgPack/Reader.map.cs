// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;

// #pragma warning disable CS3002 // CS3002 : Return type of 'MsgReader.ReadKey()' is not CLS-compliant

// ReSharper disable ReplaceSliceWithRangeIndexer
namespace Friflo.Json.Fliox.MsgPack
{

    public ref partial struct MsgReader
    {
        /// <summary> Expect call calling <see cref="ReadKey"/> / <see cref="SkipTree"/>in a subsequent loop</summary>
        public bool ReadObject(out int length)
        {
            var cur = pos;
            if (cur >= data.Length) {
                length = -1;
                SetEofError(cur);
                return false;
            }
            var type    = (MsgFormat)data[cur];
            switch (type) {
                case MsgFormat.nil:
                    pos     = cur + 1;
                    length  = -1;
                    return false;
                case >= MsgFormat.fixmap and <= MsgFormat.fixmapMax:
                {
                    pos = cur + 1;
                    length  = (int)type & 0x0f;
                    return true;
                }
                case MsgFormat.map16: {
                    pos     = cur + 3;       
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        length  = -1;
                        return false;
                    }
                    length  = BinaryPrimitives.ReadInt16BigEndian(data.Slice(cur + 1, 2));
                    return true;
                }
                case MsgFormat.map32: {
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
            SetError(MsgReaderState.ExpectObject, type, cur);
            length = -1;
            return false;
        }
        
        /// <summary>Is called subsequently after <see cref="ReadObject"/></summary>
        public ulong ReadKey()
        {
            var cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return 0;
            }
            var type    = (MsgFormat)data[cur];
            /* if ((type & 0x80) == 0) {
                pos = cur + 1;
                return (ulong)(type & 0x7f);
            } */
            switch (type) {
                case >= MsgFormat.fixstr and <= MsgFormat.fixstrMax: {
                    int len = (int)type & 0x1f;
                    pos     = cur + 1 + len;
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        return 0;
                    }
                    keyName = data.Slice(cur + 1, len);
                    if (len <= 8) {
                        return KeyAsLong(len, keyName);
                    }
                    return KeyAsLong    (8,   keyName);
                }
                case MsgFormat.str8: {
                    if (cur + 1 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return 0;
                    }
                    int len = data[cur + 1];
                    if (!read_str(out keyName, cur + 2, len, type)) {
                        return 0;
                    }
                    if (len <= 8) {
                        return KeyAsLong(len, keyName);
                    }
                    return KeyAsLong    (8,   keyName);
                }
            }
            SetError(MsgReaderState.ExpectKeyString, type, cur);
            return 0;
        }
        
        private static ulong KeyAsLong(int len, in ReadOnlySpan<byte> name)
        {
            switch (len)
            {
                case 0: return 0;
                case 1: return name[0];
                case 2: return (ulong)(name[0] | name[1] << 8);
                case 3: return BinaryPrimitives.ReadUInt16LittleEndian(name.Slice(0, 2)) |
                               ((ulong)name[2] << 16);
                case 4: return BinaryPrimitives.ReadUInt32LittleEndian(name.Slice(0, 4));
                case 5: return BinaryPrimitives.ReadUInt32LittleEndian(name.Slice(0, 4)) |
                               ((ulong)name[4] << 32);
                case 6: return BinaryPrimitives.ReadUInt32LittleEndian(name.Slice(0, 4)) | 
                               ((ulong)BinaryPrimitives.ReadUInt16LittleEndian(name.Slice(4, 2)) << 32);
                case 7: return BinaryPrimitives.ReadUInt32LittleEndian(name.Slice(0, 4)) | 
                               (ulong)BinaryPrimitives.ReadUInt32LittleEndian(name.Slice(3, 4)) << 24;
                case 8: return BinaryPrimitives.ReadUInt64LittleEndian(name.Slice(0, 8));
                default: throw new InvalidOperationException($"expect len <= 8. was: {len}");
            }
        }
    }
}