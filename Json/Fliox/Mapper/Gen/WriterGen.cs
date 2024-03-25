// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper.Map.Val;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Mapper.Map
{
    delegate void WriteDelegate<T>(ref T obj, PropField[] fields, ref Writer writer, ref bool firstMember);
    
    // NON_CLS
    #pragma warning disable 3001  // Warning CS3001 : Argument type '...' is not CLS-compliant

    partial struct Writer
    {
        private void WriteKeyNull (PropField field, ref bool firstMember) {
            if (!writeNullMembers)
                return;
            WriteFieldKey(field, ref firstMember);
            AppendNull();
        }
        
        // ---------------------------------- object - class / struct  ----------------------------------
        public void WriteClass<T> (PropField field, T value, ref bool firstMember) where T : class {
            if (value == null) {
                WriteKeyNull(field, ref firstMember);
                return;
            }
            WriteFieldKey(field, ref firstMember);
            var mapper = (TypeMapper<T>)field.fieldType;
            mapper.Write(ref this, value);
        }
        
        public void WriteStruct<T> (PropField field, T value, ref bool firstMember) where T : struct {
            WriteFieldKey(field, ref firstMember);
            var mapper = (TypeMapper<T>)field.fieldType;
            mapper.Write(ref this, value);
        }
        
        public void WriteStructNull<T> (PropField field, T? value, ref bool firstMember) where T : struct {
            if (value == null) {
                WriteKeyNull(field, ref firstMember);
                return;
            }
            WriteFieldKey(field, ref firstMember);
            var mapper = (TypeMapper<T?>)field.fieldType;
            mapper.Write(ref this, value.Value);
        }
        
        // ------------------------------------------- bool ---------------------------------------------
        public void WriteBoolean (PropField field, bool value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendBool(ref bytes, value);
        }
        
        // --- nullable
        public void WriteBooleanNull (PropField field, bool? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendBool(ref bytes, value.Value);
        }
        
        // ------------------------------------------- number ---------------------------------------------
        // --- integer
        public void WriteByte (PropField field, byte value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value);
        }
        
        public void WriteInt16 (PropField field, short value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value);
        }
        
        public void WriteInt32 (PropField field, int value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value);
        }
        
        public void WriteInt64 (PropField field, long value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendLong(ref bytes, value);
        }
        
        // NON_CLS
        public void WriteSByte (PropField field, sbyte value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value);
        }
        
        public void WriteUInt16 (PropField field, ushort value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value);
        }
        
        public void WriteUInt32 (PropField field, uint value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendULong(ref bytes, value);
        }
        
        public void WriteUInt64 (PropField field, ulong value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendULong(ref bytes, value);
        }
        
        // --- floating point
        public void WriteSingle (PropField field, float value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendFlt(ref bytes, value);
        }
        
        public void WriteDouble (PropField field, double value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendDbl(ref bytes, value);
        }
        
        // -------------------------------- nullable number ------------------------------------------
        // --- integer
        public void WriteByteNull (PropField field, byte? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value.Value);
        }
        
        public void WriteInt16Null (PropField field, short? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value.Value);
        }
        
        public void WriteInt32Null (PropField field, int? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value.Value);
        }
        
        public void WriteInt64Null (PropField field, long? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendLong(ref bytes, value.Value);
        }
        
        // NON_CLS
        public void WriteSByteNull (PropField field, sbyte? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value.Value);
        }
        
        public void WriteUInt16Null (PropField field, ushort? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value.Value);
        }
        
        public void WriteUInt32Null (PropField field, uint? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendULong(ref bytes, value.Value);
        }
        
        public void WriteUInt64Null (PropField field, ulong? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendULong(ref bytes, value.Value);
        }
        
        // --- floating point
        public void WriteSingleNull (PropField field, float? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendFlt(ref bytes, value.Value);
        }
        
        public void WriteDoubleNull (PropField field, double? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendDbl(ref bytes, value.Value);
        }
        
        // ------------------------------------------- string ---------------------------------------------
        public void WriteString (PropField field, string value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            WriteString(value);
        }

        // --- JsonKey
        /// <see cref="JsonKeyMapper.Write"/>
        public void WriteJsonKey (PropField field, in JsonKey value, ref bool firstMember) {
            if (value.IsNull()) {
                WriteKeyNull(field, ref firstMember);
                return;
            }
            WriteFieldKey(field, ref firstMember);
            var obj = value.keyObj;
            if (obj == JsonKey.LONG) {
                bytes.AppendChar('\"');
                format.AppendLong(ref bytes, value.lng);
                bytes.AppendChar('\"');
                return;
            }
            if (obj is string) {
                WriteJsonKey(value);
                return;
            }
            if (obj == JsonKey.GUID) {
                WriteGuid(value.Guid);
            }
        }
        
        // --- ShortString
        /// <see cref="ShortStringMapper.Write"/>
        public void WriteShortString (PropField field, in ShortString value, ref bool firstMember) {
            if (value.IsNull()) {
                WriteKeyNull(field, ref firstMember);
                return;
            }
            WriteFieldKey(field, ref firstMember);
            WriteShortString(value);
        }
        
        // --- JsonValue
        /// <see cref="JsonValueMapper.Write"/>
        public void WriteJsonValue (PropField field, in JsonValue value, ref bool firstMember) {
            if (value.IsNull()) {
                WriteKeyNull(field, ref firstMember);
                return;
            }
            WriteFieldKey(field, ref firstMember);
            bytes.AppendArray(value);
        }
        
        // ------------------------------------------- enum ---------------------------------------------
        public void WriteEnum<T> (PropField field, T value, ref bool firstMember) where T : struct {
            WriteFieldKey(field, ref firstMember);
            var mapper = (EnumMapper<T>)field.fieldType;
            mapper.Write(ref this, value);
        }
        
        public void WriteEnumNull<T> (PropField field, T? value, ref bool firstMember) where T : struct {
            if (!value.HasValue) {
                WriteKeyNull(field, ref firstMember);
                return;
            }
            WriteFieldKey(field, ref firstMember);
            var mapper = (EnumMapper<T>)field.fieldType;
            mapper.Write(ref this, value.Value);
        }
    }
}
