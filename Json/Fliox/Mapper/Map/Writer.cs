// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Runtime.CompilerServices;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map
{
    enum OutputType {
        ByteList    = 1,
        ByteWriter  = 2,
    }
    
    [CLSCompliant(true)]
    public partial struct Writer : IDisposable
    {
        /// <summary>Caches type meta data per thread and provide stats to the cache utilization</summary>
        public readonly     TypeCache           typeCache;
        public              Bytes               bytes;
        /// <summary>Can be used for custom mappers append a number while creating the JSON payload</summary>
        public              ValueFormat         format;
        internal            Bytes               @null;
        public              char[]              charBuf;
        public              int                 level;
        public              int                 maxDepth;
        public              bool                pretty;
        public              bool                writeNullMembers;

        internal            OutputType          outputType;
        public              IBytesWriter        bytesWriter;
        // public           int                 GetLevel() => level;

        public Writer(TypeStore typeStore) {
            bytes           = new Bytes(128);
            format          = new ValueFormat();
            format. InitTokenFormat();
            @null           = new Bytes("null");
            charBuf         = new char[128];
            typeCache       = new TypeCache(typeStore);
            level           = 0;
            maxDepth        = Utf8JsonParser.DefaultMaxDepth;
            outputType      = OutputType.ByteList;
            pretty          = false;
            writeNullMembers= true;
            bytesWriter     = null;
        }
        
        public void Dispose() {
            typeCache.Dispose();
            @null.Dispose();
            format.Dispose();
            bytes.Dispose();
        }
        
        // --- WriteUtils
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(string str) {
            Utf8JsonWriter.AppendEscString(ref bytes, str.AsSpan());
        }
        
        public void WriteDateTime(in DateTime dateTime) {
            Span<char> chars = stackalloc char[Bytes.DateTimeLength]; 
            bytes.AppendChar('"');
            bytes.AppendDateTime(dateTime, chars);
            bytes.AppendChar('"');
        }
        
        public void WriteJsonKey(in JsonKey value) {
            var obj = value.keyObj;
            if (obj != JsonKey.STRING_SHORT) {
                var str = (string)obj;
                Utf8JsonWriter.AppendEscString(ref bytes, str.AsSpan());
                return;
            }
            int valueLength = value.GetShortLength() + 2; // <value> + 2 * "
            bytes.EnsureCapacityAbs(bytes.end + valueLength);
            bytes.buffer[bytes.end++] = (byte)'"';
            bytes.AppendShortString(value.lng, value.lng2);
            bytes.buffer[bytes.end++] = (byte)'"';
        }
        
        public void WriteShortString(in ShortString value) {
            var str = value.str;
            if (str != null) {
                Utf8JsonWriter.AppendEscString(ref bytes, str.AsSpan());
                return;
            }
            int valueLength = value.GetShortLength() + 2; // <value> + 2 * "
            bytes.EnsureCapacityAbs(bytes.end + valueLength);
            bytes.buffer[bytes.end++] = (byte)'"';
            bytes.AppendShortString(value.lng, value.lng2);
            bytes.buffer[bytes.end++] = (byte)'"';
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendNull() {
            bytes.AppendBytes(@null);
            FlushFilledBuffer();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IncLevel() {
            if (level++ < maxDepth)
                return level;
            throw new InvalidOperationException($"JsonParser: maxDepth exceeded. maxDepth: {maxDepth}");
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DecLevel(int expectedLevel) {
            if (level-- != expectedLevel)
                throw new InvalidOperationException($"Unexpected level in Write() end. Expect {expectedLevel}, Found: {level + 1}");
        }
        
        // --- indentation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDelimiter(int pos) {
            if (pos > 0) {
                bytes.EnsureCapacityAbs(bytes.end + 1);
                bytes.buffer[bytes.end++] = (byte)',';
            }
            if (pretty)
                IndentBegin();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArrayBegin() {
            bytes.EnsureCapacityAbs(bytes.end + 1);
            bytes.buffer[bytes.end++] = (byte)'[';
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArrayEnd() {
            if (pretty)
                IndentEnd();
            bytes.EnsureCapacityAbs(bytes.end + 1);
            bytes.buffer[bytes.end++] = (byte)']';
        }
        
        public void IndentBegin() {
            Utf8JsonWriter.IndentJsonNode(ref bytes, this.level);
        }
        
        public void IndentEnd() {
            int decLevel = this.level - 1;
            Utf8JsonWriter.IndentJsonNode(ref bytes, decLevel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteObjectEnd(bool emptyObject) {
            if (pretty)
                IndentEnd();

            if (emptyObject) {
                bytes.EnsureCapacityAbs(bytes.end + 2);
                bytes.buffer[bytes.end++] = (byte)'{';
                bytes.buffer[bytes.end++] = (byte)'}';
            } else {
                bytes.EnsureCapacityAbs(bytes.end + 1);
                bytes.buffer[bytes.end++] = (byte)'}';
            }
        }
        
        // --- member keys
        public void WriteDiscriminator(TypeMapper baseMapper, TypeMapper mapper, ref bool firstMember) {
            var factory = baseMapper.instanceFactory;
            if (factory != null && factory.discriminator == null)
                return;
            
            bytes.AppendChar('{');
            if (pretty)
                IndentBegin();
            // --- discriminator
            bytes.AppendChar('"');
            bytes.AppendString(factory.discriminator);
            bytes.AppendChar('"');
            bytes.AppendChar(':');
            
            // --- discriminant
            bytes.AppendChar('"');
            bytes.AppendString(mapper.discriminant);
            bytes.AppendChar('\"');
            FlushFilledBuffer();
            firstMember = false;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteKey<T>(KeyMapper<T> keyMapper, T key, int pos) {
            WriteDelimiter(pos);
            keyMapper.WriteKey(ref this, key);
            if (!pretty)
                bytes.AppendChar(':');
            else
                bytes.AppendChar2(':', ' ');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFieldKey(PropField field, ref bool firstMember) {
            if (!pretty) {
                if (firstMember)
                    bytes.AppendBytes(field.firstMember);
                else
                    bytes.AppendBytes(field.subSeqMember);
            } else {
                bytes.AppendChar(firstMember ? '{' : ',');
                IndentBegin();
                bytes.AppendChar('"');
                bytes.AppendBytes(field.nameBytes);
                bytes.AppendChar('"');
                bytes.AppendChar2(':', ' ');
            } 
            firstMember = false;
        }
        
        // --- Flush
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlushFilledBuffer() {
            if (bytes.end < 4096)
                return;
            Flush();
        }
        
        public void Flush() {
            switch (outputType) {
                case OutputType.ByteList:
                    return; // ByteList mode does not support streaming
                case OutputType.ByteWriter:
#if JSON_BURST
                    NonBurstWriter.WriteNonBurst(writerHandle, ref bytes.buffer, bytes.end);
#else
                    bytesWriter.Write(bytes.buffer, bytes.end);
#endif                    
                    bytes.Clear();
                    break;
            }
        }
        
        // --- array element
        public void WriteElement<T>(TypeMapper<T> mapper, ref T value) {
            mapper.Write(ref this, value);
        }
        
        public void WriteGuid (in Guid guid) {
            bytes.AppendChar('\"');
            bytes.AppendGuid(guid, charBuf);
            bytes.AppendChar('\"');
        }
    }
}
