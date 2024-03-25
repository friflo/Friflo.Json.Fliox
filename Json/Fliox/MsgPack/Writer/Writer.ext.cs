// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;


// ReSharper disable CommentTypo
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.MsgPack
{
    public partial struct MsgWriter
    {
        // --- ext1
        public void WriteExt1(byte type, byte val) {
            var data    = Reserve(10);               // type/val: 10
            Write_ext_fix_pos(0, data, pos, type, val);
        }
        
        public void WriteKeyExt1(int keyLen, long key, byte type, byte val) {
            var cur     = pos;
            var data    = Reserve(1 + 8 + 10);       // key: 1 + 8,  type/val: 10
            WriteKeyFix(data, cur, keyLen, key);
            Write_ext_fix_pos(0, data, cur + 1 + keyLen, type, val);
        }
       
        public void WriteKeyExt1(ReadOnlySpan<byte> key, byte type, byte val) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 10);  // key: 2 + keyLen,  type/val: 10
            WriteKeySpan(data, ref cur, key);
            Write_ext_fix_pos(0, data, cur, type, val);
        }
        
        
        // --- ext2
        public void WriteExt2(byte type, short val) {
            var data    = Reserve(10);               // type/val: 10
            Write_ext_fix_pos(1, data, pos, type, val);
        }
        
        public void WriteKeyExt2(int keyLen, long key, byte type, short val) {
            var cur     = pos;
            var data    = Reserve(1 + 8 + 10);       // key: 1 + 8,  type/val: 10
            WriteKeyFix(data, cur, keyLen, key);
            Write_ext_fix_pos(1, data, cur + 1 + keyLen, type, val);
        }
        
        public void WriteKeyExt2(ReadOnlySpan<byte> key, byte type, short val) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 10);  // key: 2 + keyLen,  type/val: 10
            WriteKeySpan(data, ref cur, key);
            Write_ext_fix_pos(1, data, cur, type, val);
        }
        
        
        // --- ext4
        public void WriteExt4(byte type, int val) {
            var data    = Reserve(10);               // type/val: 10
            Write_ext_fix_pos(2, data, pos, type, val);
        }
        
        public void WriteKeyExt4(int keyLen, long key, byte type, int val) {
            var cur     = pos;
            var data    = Reserve(1 + 8 + 10);       // key: 1 + 8,  type/val: 10
            WriteKeyFix(data, cur, keyLen, key);
            Write_ext_fix_pos(2, data, cur + 1 + keyLen, type, val);
        }
        
        public void WriteKeyExt4(ReadOnlySpan<byte> key, byte type, int val) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 10);  // key: 2 + keyLen,  type/val: 10
            WriteKeySpan(data, ref cur, key);
            Write_ext_fix_pos(2, data, cur, type, val);
        }
        
        
        // --- ext8
        public void WriteExt8(byte type, long val) {
            var data    = Reserve(10);               // type/val: 10
            Write_ext_fix_pos(3, data, pos, type, val);
        }
        
        public void WriteKeyExt8(int keyLen, long key, byte type, long val) {
            var cur     = pos;
            var data    = Reserve(1 + 8 + 10);       // key: 1 + 8,  type/val: 10
            WriteKeyFix(data, cur, keyLen, key);
            Write_ext_fix_pos(3, data, cur + 1 + keyLen, type, val);
        }
        
        public void WriteKeyExt8(ReadOnlySpan<byte> key, byte type, long val) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 10);  // key: 2 + keyLen,  type/val: 10
            WriteKeySpan(data, ref cur, key);
            Write_ext_fix_pos(3, data, cur, type, val);
        }
        
        
        // --- ext16
        public void WriteExt16(byte type, long val0, long val1) {
            var data    = Reserve(18);               // type/val: 18
            Write_ext_fix16_pos(data, pos, type, val0, val1);
        }
        
        public void WriteKeyExt16(int keyLen, long key, byte type, long val0, long val1) {
            var cur     = pos;
            var data    = Reserve(1 + 8 + 18);       // key: 1 + 8,  type/val: 18
            WriteKeyFix(data, cur, keyLen, key);
            Write_ext_fix16_pos(data, cur + 1 + keyLen, type, val0, val1);
        }
        
        public void WriteKeyExt16(ReadOnlySpan<byte> key, byte type, long val0, long val1) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 18);  // key: 2 + keyLen,  type/val: 18
            WriteKeySpan(data, ref cur, key);
            Write_ext_fix16_pos(data, cur, type, val0, val1);
        }
        
        
        // ------------------------------------------- utils -------------------------------------------
        private void Write_ext_fix_pos(byte ext, byte[] data, int cur, byte type, long val)
        {
            data[cur]       = (byte)(MsgFormat.fixext1 + ext);
            data[cur + 1]   = type;
            BinaryPrimitives.WriteInt64LittleEndian (new Span<byte>(data, cur + 2, 8), val);
            pos             = cur + 2 + (1 << ext);
        }
        
        private void Write_ext_fix16_pos(byte[] data, int cur, byte type, long val0, long val1)
        {
            data[cur]       = (byte)MsgFormat.fixext16;
            data[cur + 1]   = type;
            BinaryPrimitives.WriteInt64LittleEndian (new Span<byte>(data, cur +  2, 8), val0);
            BinaryPrimitives.WriteInt64LittleEndian (new Span<byte>(data, cur + 10, 8), val1);
            pos             = cur + 2 + 16;
        }
    }
}