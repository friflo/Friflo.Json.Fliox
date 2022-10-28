// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Mapper.Map
{
    delegate void WriteDelegate<in T>(T obj, PropField[] fields, ref Writer writer, ref bool firstMember);

    partial struct Writer
    {
        public void Write (string name, PropField field, int value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            format.AppendInt(ref bytes, value);
        }
        
        public void Write<T> (string name, PropField<T> field, T value, ref bool firstMember) {
            WriteFieldKey(field, ref firstMember);
            field.mapper.Write(ref this, value);
        }
    }
}
