// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Data.Common;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public sealed class BinaryDbDataReader : BinaryReader
    { 
        private readonly    DbDataReader    reader;
        
        public BinaryDbDataReader(DbDataReader reader) {
            this.reader     = reader;
        }
        
        public void Init() {
            currentOrdinal = 0;
        }
        
        public override bool HasObject (TypeMapper mapper) {
            int ordinal = currentOrdinal++;
            bool result = reader.GetBoolean(ordinal);
            if (result) {
                return true;
            }
            currentOrdinal += mapper.PropFields.fields.Length;
            return false;
        }

        public override Var GetVar(PropField field)
        {
            int ordinal = currentOrdinal++;
            /* if (reader.IsDBNull(ordinal)) {
                return new Var(field.fieldType.varType.DefaultValue);
            } */
            if (!field.fieldType.isNullable)
            {
                switch (field.typeId)
                {
                    case StandardTypeId.Boolean:    return new Var(reader.GetBoolean    (ordinal));
                    case StandardTypeId.String:     return new Var(reader.GetString     (ordinal));
                    //
                    case StandardTypeId.Uint8:      return new Var(reader.GetByte       (ordinal));
                    case StandardTypeId.Int16:      return new Var(reader.GetInt16      (ordinal));
                    case StandardTypeId.Int32:      return new Var(reader.GetInt32      (ordinal));
                    case StandardTypeId.Int64:      return new Var(reader.GetInt64      (ordinal));
                    //
                    case StandardTypeId.Float:      return new Var(reader.GetFloat      (ordinal));
                    case StandardTypeId.Double:     return new Var(reader.GetDouble     (ordinal));
                    //
                    case StandardTypeId.DateTime:   return new Var(reader.GetDateTime   (ordinal));
                    case StandardTypeId.Guid:       return new Var(reader.GetGuid       (ordinal));
                }
                throw new NotImplementedException($"GetVar() {field.typeId}");
            }
            var value = reader.GetValue(ordinal);
            if (value == DBNull.Value) {
                return field.fieldType.varType.DefaultValue;
            }
            switch (field.typeId)
            {
                case StandardTypeId.Boolean:    return new Var((bool?)      value);
                case StandardTypeId.String:     return new Var(             value);
                //
                case StandardTypeId.Uint8:      return new Var((byte?)      value);
                case StandardTypeId.Int16:      return new Var((short?)     value);
                case StandardTypeId.Int32:      return new Var((int?)       value);
                case StandardTypeId.Int64:      return new Var((long?)      value);
                //
                case StandardTypeId.Float:      return new Var((float?)     value);
                case StandardTypeId.Double:     return new Var((double?)    value);
                //
                case StandardTypeId.DateTime:   return new Var((DateTime?)  value);
                case StandardTypeId.Guid:       return new Var((Guid?)      value);
            }
            throw new NotImplementedException($"GetVar() {field.typeId}");
        }
    }
}