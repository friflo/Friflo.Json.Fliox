// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;
using System.Text;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;
using static Friflo.Json.Fliox.MsgPack.MsgReaderState;
using static Friflo.Json.Fliox.MsgPack.MsgFormat;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.MsgPack
{
    public ref partial struct MsgReader
    {
        // --- private fields
        [Browse(Never)] private     ReadOnlySpan<byte>  data;
        [Browse(Never)] private     int                 pos;
        [Browse(Never)] private     ReadOnlySpan<byte>  keyName;
        [Browse(Never)] private     MsgReaderState      state;
        [Browse(Never)] private     MsgFormat           errorType;
        [Browse(Never)] private     int                 errorPos;
        
        // --- public properties
                        public      ReadOnlySpan<byte>  Data            => data;
                        public      int                 Pos             => pos;
        /// <summary><see cref="keyName"/> is set in <see cref="ReadKey"/></summary>
                        public      ReadOnlySpan<byte>  KeyName         => keyName;
                        public      string              KeyNameString   => keyName == null ? null : MsgPackUtils.SpanToString(keyName);
        
                        public      string              Error           => CreateErrorMessage(null);
                        public      MsgReaderState      State           => state;
                        public      override string     ToString()      => GetString();
        // --- const
                        private     const int           MsgError = int.MaxValue;
        
        public MsgReader(ReadOnlySpan<byte> data) {
            this.data   = data;
            pos         = 0;
            keyName     = default;
            state       = Ok;
            errorType   = root;
            errorPos    = 0;
        }
        
        public void Init(ReadOnlySpan<byte> data) {
            this.data   = data;
            pos         = 0;
            keyName     = default;
            state       = Ok;
            errorType   = root;
            errorPos    = 0;
        }
        
        /// <summary>
        /// Return true if the given <paramref name="key"/> is equal to <see cref="KeyName"/>
        /// </summary>
        public bool IsKeyEquals(byte[] key) {
            return keyName.SequenceEqual(key);
        }
        
        private string GetString() {
            if (state != Ok) {
                return CreateErrorMessage(null);
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
                case True:      pos = cur + 1; return true;
                case False:     pos = cur + 1; return false;
            }
            SetError(ExpectBool, type, cur);
            return false;
        }
        
        public string ReadString ()
        {
            var span = ReadStringSpan();
            if (span == null) {
                return null;
            }
            return MsgPackUtils.SpanToString(span);
        }
        
        public ReadOnlySpan<byte> ReadStringSpan ()
        {
            int cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return default;
            }
            var type = (MsgFormat)data[cur];
            switch (type) {
                case nil:
                    pos = cur + 1;
                    return default;
                case >= fixstr and <= fixstrMax: {
                    return read_str(cur + 1, (int)type & 0x1f, type);
                }
                case str8: {
                    if (cur + 1 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return default;
                    }
                    int len = data[cur + 1];
                    return read_str(cur + 2, len, type);
                }
                case str16: {
                    if (cur + 2 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return default;
                    }
                    int len = data[cur + 1] << 8 | data [cur + 2];
                    return read_str(cur + 3, len, type);
                }
                case str32: {
                    if (cur + 4 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return default;
                    }
                    var len = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(cur + 1, 4));
                    return read_str(cur + 5, (int)len, type);
                }
            }
            SetError(ExpectString, type, cur);
            return default;
        }
    }
}