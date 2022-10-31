// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Mapper.Map
{
    delegate void WriteDelegate<in T>(T obj, PropField[] fields, ref Writer writer, ref bool firstMember);

    partial struct Writer
    {
        // used specific name to avoid using it accidentally with a non class / struct type  
        public void WriteObj<T> (string name, PropField field, T value, ref bool firstMember) {
            if (value == null) {
                if (!writeNullMembers)
                    return;
                WriteFieldKey(field, ref firstMember);
                AppendNull();
                return;
            }
            WriteFieldKey(field, ref firstMember);
            var mapper = (TypeMapper<T>)field.fieldType;
            mapper.Write(ref this, value);
        }
        
        private void WriteKeyNull (PropField field, ref bool firstMember) {
            if (!writeNullMembers)
                return;
            WriteFieldKey(field, ref firstMember);
            AppendNull();
        }
        
        // ------------------------------------------- bool ---------------------------------------------
        public void Write (string name, PropField field, bool value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendBool(ref bytes, value);
        }
        
        // --- nullable
        public void Write (string name, PropField field, bool? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendBool(ref bytes, value.Value);
        }
        
        // ------------------------------------------- number ---------------------------------------------
        // --- integer
        public void Write (string name, PropField field, byte value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value);
        }
        
        public void Write (string name, PropField field, short value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value);
        }
        
        public void Write (string name, PropField field, int value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value);
        }
        
        public void Write (string name, PropField field, long value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendLong(ref bytes, value);
        }
        
        // --- floating point
        public void Write (string name, PropField field, float value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendFlt(ref bytes, value);
        }
        
        public void Write (string name, PropField field, double value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendDbl(ref bytes, value);
        }
        
        // -------------------------------- nullable number ------------------------------------------
        // --- integer
        public void Write (string name, PropField field, byte? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value.Value);
        }
        
        public void Write (string name, PropField field, short? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value.Value);
        }
        
        public void Write (string name, PropField field, int? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value.Value);
        }
        
        public void Write (string name, PropField field, long? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendLong(ref bytes, value.Value);
        }
        
        // --- floating point
        public void Write (string name, PropField field, float? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendFlt(ref bytes, value.Value);
        }
        
        public void Write (string name, PropField field, double? value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            format.AppendDbl(ref bytes, value.Value);
        }
        
        // ------------------------------------------- string ---------------------------------------------
        public void Write (string name, PropField field, string value, ref bool firstMember) {
            if (value == null) { WriteKeyNull(field, ref firstMember); return; }
            WriteFieldKey(field, ref firstMember);
            WriteString(value);
        }

        public void Write (string name, PropField field, in JsonKey value, ref bool firstMember) {
            switch (value.type) {
                case JsonKeyType.Long:
                    bytes.AppendChar('\"');
                    format.AppendLong(ref bytes, value.lng);
                    bytes.AppendChar('\"');
                    break;
                case JsonKeyType.String:
                    WriteString(value.str);
                    break;
                case JsonKeyType.Guid:
                    WriteGuid(value.guid);
                    break;
                case JsonKeyType.Null:
                    AppendNull();
                    break;
            }
        }
        
        // ------------------------------------------- any ---------------------------------------------
        public void Write (string name, PropField field, in JsonValue value, ref bool firstMember) {
            if (!value.IsNull())
                bytes.AppendArray(value);
            else
                AppendNull();
        }
    }
}
