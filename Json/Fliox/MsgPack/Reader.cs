// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;
using System.Text;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace Friflo.Json.Fliox.MsgPack
{
    public enum MsgReaderState
    {
        Ok                  = 0,
        //
        UnexpectedEof       = 1,
        //
        ExpectArrayError    = 2,
        ExpectByteArray     = 3,
        ExpectBool          = 4,
        ExpectString        = 5,
        ExpectObject        = 6,
        ExpectKeyString     = 7,
        //
        ExpectUint8         = 8,
        ExpectInt16         = 9,
        ExpectInt32         = 10,
        ExpectInt64         = 11,
        ExpectFloat32       = 12,
        ExpectFloat64       = 13,
        //
        RangeError          = 14,
        UnsupportedType     = 15,
    }

    public ref partial struct MsgReader
    {
                        private     ReadOnlySpan<byte>  data;
        [Browse(Never)] private     int                 pos;
        [Browse(Never)] private     ReadOnlySpan<byte>  keyName;
        [Browse(Never)] private     MsgReaderState      state;
        [Browse(Never)] private     string              error;
        
                        public      int                 Pos => pos;
        /// <summary><see cref="keyName"/> is set in <see cref="ReadKey"/></summary>
                        public      ReadOnlySpan<byte>  KeyName         => keyName;
                        public      string              KeyNameString   => keyName == null ? null : MsgPackUtils.SpanToString(keyName);
        
                        public      string              Error           => error;
                        public      MsgReaderState      State           => state;
                        public      override string     ToString()      => GetString();

                        public      const int           MsgError = int.MaxValue;
        
        public MsgReader(ReadOnlySpan<byte> data) {
            this.data   = data;
            pos         = 0;
            keyName     = default;
            state       = MsgReaderState.Ok;
            error       = null;
        }
        
        public void Init(ReadOnlySpan<byte> data) {
            this.data   = data;
            pos         = 0;
            keyName     = default;
            state       = MsgReaderState.Ok;
            error       = null;
        }
        
        private string GetString() {
            if (pos == MsgError) {
                return error;
            }
            var sb = new StringBuilder();
            sb.Append($"pos: {pos} ");
            if (keyName == null) {
                sb.Append("(root)");
            } else {
                sb.Append($", last key: '{KeyNameString}'");
            }
            return sb.ToString();
        }
        
        public bool ReadBool ()
        {
            int cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return false;
            }
            var type = (MsgFormat)data[cur];
            switch (type) {
                case MsgFormat.True:    return true;
                case MsgFormat.False:   return false;
            }
            SetError(MsgReaderState.ExpectBool, type, cur);
            return false;
        }
        
        public string ReadString ()
        {
            int cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return null;
            }
            var type = (MsgFormat)data[cur];
            switch (type) {
                case MsgFormat.nil:
                    pos = cur + 1;
                    return null;
                case >= MsgFormat.fixstr and <= MsgFormat.fixstrMax: {
                    if (!read_str(out var span, cur + 1, (int)type & 0x1f, type)) {
                        return null;
                    }
                    return MsgPackUtils.SpanToString(span);
                }
                case MsgFormat.str8: {
                    if (cur + 1 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return null;
                    }
                    if (!read_str(out var span, cur + 2, data[cur + 1], type)) {
                        return null;
                    }
                    return MsgPackUtils.SpanToString(span);
                }
                case MsgFormat.str16: {
                    if (cur + 2 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return null;
                    }
                    int len = data[cur + 1] << 8 | data [cur + 2];
                    if (!read_str(out var span, cur + 3, len, type)) {
                        return null;
                    }
                    return MsgPackUtils.SpanToString(span);
                }
                case MsgFormat.str32: {
                    if (cur + 4 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return null;
                    }
                    var len = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(cur + 1, 4));
                    if (!read_str(out var span, cur + 5, (int)len, type)) {
                        return null;
                    }
                    return MsgPackUtils.SpanToString(span);
                }
            }
            SetError(MsgReaderState.ExpectString, type, cur);
            return null;
        }
    }
}