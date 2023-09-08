// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Buffers.Binary;

namespace Friflo.Json.Fliox.MsgPack
{

    public ref partial struct MsgReader
    {
        /// <summary>Is called subsequently after <see cref="ReadObject"/> of unknown keys</summary>
        public void SkipTree() {
            int cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return;
            }
            var type = (MsgFormat)data[cur];
            switch (type) {
                // ----------------- ensure - subsequent cases end with: break; -----------------
                case <= MsgFormat.fixintPosMax:
                case >= MsgFormat.fixintNeg and <= MsgFormat.fixintNegMax: 
                    pos = cur + 1;
                    break;
                case MsgFormat.int8:
                case MsgFormat.uint8:
                    pos = cur + 2;
                    break;
                case MsgFormat.int16:
                case MsgFormat.uint16:
                    pos = cur + 3;
                    break;
                case MsgFormat.int32:
                case MsgFormat.uint32:
                case MsgFormat.float32:
                    pos = cur + 5;
                    break;
                case MsgFormat.int64:
                case MsgFormat.uint64:
                case MsgFormat.float64:
                    pos = cur + 9;
                    break;
                
                // --- string
                case >= MsgFormat.fixstr and <= MsgFormat.fixstrMax: {
                    int len = (int)type & 0x1f;
                    pos = cur + 1 + len;
                    break;
                }
                case MsgFormat.str8: {
                    pos = cur + 2;
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    int len = data[cur + 1];
                    pos += len;
                    break;
                }
                case MsgFormat.str16: {
                    pos = cur + 3;
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    int len = data[cur + 1] << 8 + data[cur + 2];
                    pos += len;
                    break;
                }
                case MsgFormat.str32: {
                    pos = cur + 5;
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    int len = BinaryPrimitives.ReadInt32BigEndian(data.Slice(cur + 1, 4));
                    pos += len;
                    break;
                }
                
                // ----------------- ensure - subsequent cases end with: return; -----------------
                // --- array
                case >= MsgFormat.fixarray and <= MsgFormat.fixarrayMax:
                    pos = cur + 1;
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    SkipArray(data[cur] & 0x0f);
                    return;
                case MsgFormat.array16: {
                    pos = cur + 3;
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    int len = data[cur + 1] << 8 | data[cur + 2];  
                    SkipArray(len);
                    return;
                }
                case MsgFormat.array32: {
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
                case >= MsgFormat.fixmap and <= MsgFormat.fixmapMax:
                    pos = cur + 1;
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    SkipMap(data[cur] & 0x0f);
                    return;
                case MsgFormat.map16: {
                    pos = cur + 3;
                    if (pos > data.Length) {
                        SetEofErrorType(type, cur);
                        return;
                    }
                    int len = data[cur + 1] << 8 | data[cur + 2];  
                    SkipMap(len);
                    return;
                }
                case MsgFormat.map32: {
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
                    SetTypeError("unsupported type", type, cur);
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