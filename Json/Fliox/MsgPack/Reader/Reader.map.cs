// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using static Friflo.Json.Fliox.MsgPack.MsgReaderState;
using static Friflo.Json.Fliox.MsgPack.MsgFormat;
using static System.Buffers.Binary.BinaryPrimitives;

// #pragma warning disable CS3002 // CS3002 : Return type of 'MsgReader.ReadKey()' is not CLS-compliant

// ReSharper disable ReplaceSliceWithRangeIndexer
// ReSharper disable once CheckNamespace
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
                case nil:
                    pos     = cur + 1;
                    length  = -1;
                    return false;
                case >= fixmap and <= fixmapMax:
                {
                    pos = cur + 1;
                    length  = (int)type & 0x0f;
                    return true;
                }
                case map16: {
                    pos     = cur + 3;       
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        length  = -1;
                        return false;
                    }
                    length  = ReadInt16BigEndian(data.Slice(cur + 1, 2));
                    return true;
                }
                case map32: {
                    pos     = cur + 5;       
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        length  = -1;
                        return false;
                    }
                    length  = ReadInt32BigEndian(data.Slice(cur + 1, 4));
                    return true;
                }
            }
            SetError(ExpectObject, type, cur);
            length = -1;
            return false;
        }
        
#pragma warning disable 3002  // Return type of '...' is not CLS-compliant
        /// <summary>Is called subsequently after <see cref="ReadObject"/></summary>
        public ulong ReadKey()
        {
            var cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return 0;
            }
            var type    = (MsgFormat)data[cur];
            switch (type) {
                case >= fixstr and <= fixstrMax: {
                    int len = (int)type & 0x1f;
                    pos     = cur + 1 + len;
                    if (pos <= data.Length) {
                        keyName = data.Slice(cur + 1, len);
                        return KeyAsLong(len, keyName);
                    }
                    SetEofErrorType(type, cur);
                    return 0;
                }
                case str8: {
                    if (cur + 1 < data.Length) {
                        int len = data[cur + 1];
                        keyName = read_str(cur + 2, len, type);
                        if (keyName == null) {
                            return 0;
                        }
                        return KeyAsLong(len, keyName);
                    }
                    SetEofErrorType(type, cur);
                    return 0;
                }
            }
            SetError(ExpectKeyString, type, cur);
            return 0;
        }
#pragma warning restore 3002
        
        private static ulong KeyAsLong(int len, in ReadOnlySpan<byte> name)
        {
            if (len > 8) {
                len = 8;
            }
            switch (len)
            {
                case 0: return 0;
                case 1: return name[0];
                case 2: return (ulong)(name[0] | name[1] << 8);
                case 3: return ReadUInt16LittleEndian(name.Slice(0, 2)) |
                               ((ulong)name[2] << 16);
                case 4: return ReadUInt32LittleEndian(name.Slice(0, 4));
                case 5: return ReadUInt32LittleEndian(name.Slice(0, 4)) |
                               ((ulong)name[4] << 32);
                case 6: return ReadUInt32LittleEndian(name.Slice(0, 4)) | 
                               ((ulong)ReadUInt16LittleEndian(name.Slice(4, 2)) << 32);
                case 7: return ReadUInt32LittleEndian(name.Slice(0, 4)) | 
                               (ulong)ReadUInt32LittleEndian(name.Slice(3, 4)) << 24;
                case 8: return ReadUInt64LittleEndian(name.Slice(0, 8));
                default:        throw new InvalidOperationException($"expect len <= 8. was: {len}");
            }
        }
    }
}