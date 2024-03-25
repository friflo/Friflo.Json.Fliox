// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using static Friflo.Json.Fliox.MsgPack.MsgFormat;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.MsgPack
{
    public ref partial struct MsgReader
    {
        /// <summary>Is called subsequently after <see cref="ReadObject"/> of unknown keys</summary>
        public void SkipTree()
        {
            int cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return;
            }
            var type = (MsgFormat)data[cur];
            switch (type) {
                // ----------------- ensure - subsequent cases end with: break; -----------------
                case nil:
                case True:
                case False:
                    pos = cur + 1;
                    break;
                case <= fixintPosMax:
                case >= fixintNeg and <= fixintNegMax: 
                    pos = cur + 1;
                    break;
                // --- bin
                case bin8:
                {
                    if (cur + 1 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    int len = data[cur + 1];
                    pos = 1 + cur + 1 + len;
                    break;
                }
                case bin16: {
                    if (cur + 2 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    int len = BinaryPrimitives.ReadInt16BigEndian(data.Slice(cur + 1, 2));
                    pos = 1 + cur + 2 + len;
                    break;
                }
                case bin32: {
                    if (cur + 4 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    int len = BinaryPrimitives.ReadInt32BigEndian(data.Slice(cur + 1, 4));
                    pos = 1 + cur + 4 + len;
                    break;
                }
                case int8:
                case uint8:
                    pos = cur + 2;
                    break;
                case int16:
                case uint16:
                    pos = cur + 3;
                    break;
                case int32:
                case uint32:
                case float32:
                    pos = cur + 5;
                    break;
                case int64:
                case uint64:
                case float64:
                    pos = cur + 9;
                    break;
                case fixext1:
                    pos = cur + 3;
                    break;
                case fixext2:
                    pos = cur + 4;
                    break;
                case fixext4:
                    pos = cur + 6;
                    break;
                case fixext8:
                    pos = cur + 10;
                    break;
                case fixext16:
                    pos = cur + 18;
                    break;
                
                // --- string
                case >= fixstr and <= fixstrMax: {
                    int len = (int)type & 0x1f;
                    pos = cur + 1 + len;
                    break;
                }
                case str8: {
                    if (cur + 1 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    int len = data[cur + 1];
                    pos = 1 + cur + 1 + len;
                    break;
                }
                case str16: {
                    if (cur + 2 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    int len = (data[cur + 1] << 8) + data[cur + 2];
                    pos = 1 + cur + 2 + len;
                    break;
                }
                case str32: {
                    if (cur + 4 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    int len = BinaryPrimitives.ReadInt32BigEndian(data.Slice(cur + 1, 4));
                    pos = 1 + cur + 4 + len;
                    break;
                }
                
                // ----------------- ensure - subsequent cases end with: return; -----------------
                // --- array
                case >= fixarray and <= fixarrayMax:
                    pos = cur + 1;
                    SkipArray((int)type & 0x0f);
                    return;
                case array16: {
                    pos = cur + 3;
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    int len = data[cur + 1] << 8 | data[cur + 2];  
                    SkipArray(len);
                    return;
                }
                case array32: {
                    pos = cur + 5;
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    int len = BinaryPrimitives.ReadInt32BigEndian(data.Slice(cur + 1, 4));
                    SkipArray(len);
                    return;
                }
                
                // --- map
                case >= fixmap and <= fixmapMax: {
                    pos = cur + 1;
                    int len = (int)type & 0x0f;
                    SkipMap(len);
                    return;
                }
                case map16: {
                    pos = cur + 3;
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    int len = data[cur + 1] << 8 | data[cur + 2];  
                    SkipMap(len);
                    return;
                }
                case map32: {
                    pos = cur + 5;
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    int len = BinaryPrimitives.ReadInt32BigEndian(data.Slice(cur + 1, 4));  
                    SkipMap(len);
                    return;
                }
                default:
                    SetError(MsgReaderState.UnsupportedType, type, cur);
                    return;
            }
            if (pos > data.Length) {
                SetEofErrorType(type, cur);
            }
        }
        
        private void SkipArray(int len) {
            for (int n = 0; n < len; n++) {
                SkipTree(); // value
            }
        }
        
        private void SkipMap(int len) {
            for (int n = 0; n < len; n++) {
                SkipTree(); // key
                SkipTree(); // value
            }
        }
    }
}