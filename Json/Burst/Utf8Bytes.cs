// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace Friflo.Json.Burst
{
    public readonly struct Utf8Bytes {
        private   readonly  byte[]              buffer;
        public    readonly  int                 start;
        public    readonly  int                 len;

        public    override  string              ToString()      => AsString();
        public              ReadOnlySpan<byte>  ReadOnlySpan    => new ReadOnlySpan<byte> (buffer, start, len);
        
        public Utf8Bytes (byte[] buffer, int start, int len) {
            this.buffer = buffer;
            this.start  = start;
            this.len    = len;
        }
        
        public Utf8Bytes (in Utf8Bytes bytes, int start, int len) {
            this.buffer = bytes.buffer;
            this.start  = start;
            this.len    = len;
        }
        
        public Utf8Bytes(in ReadOnlySpan<char> value) {
            len         = Encoding.UTF8.GetByteCount(value);
            buffer      = new byte[len];
            Encoding.UTF8.GetBytes(value, buffer);
            start       = 0;
        }
        
        public bool IsEqual (in Utf8Bytes value) {
            if (len != value.len)
                return false;
            return ReadOnlySpan.SequenceEqual(value.ReadOnlySpan);
        }
        
        public string AsString() {
            return Utf8Buffer.Utf8.GetString(buffer, start, len);  
        }
    }
}


