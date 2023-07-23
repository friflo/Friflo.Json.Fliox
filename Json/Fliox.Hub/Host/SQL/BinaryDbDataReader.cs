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
            if (field.required)
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
            switch (field.typeId)
            {
                case StandardTypeId.Boolean:    return new Var((bool?)      reader.GetBoolean   (ordinal));
                case StandardTypeId.String:     return new Var(             reader.GetString    (ordinal));
                //
                case StandardTypeId.Uint8:      return new Var((byte?)      reader.GetByte      (ordinal));
                case StandardTypeId.Int16:      return new Var((short?)     reader.GetInt16     (ordinal));
                case StandardTypeId.Int32:      return new Var((int?)       reader.GetInt32     (ordinal));
                case StandardTypeId.Int64:      return new Var((long?)      reader.GetInt64     (ordinal));
                //
                case StandardTypeId.Float:      return new Var((float?)     reader.GetFloat     (ordinal));
                case StandardTypeId.Double:     return new Var((double?)    reader.GetDouble    (ordinal));
                //
                case StandardTypeId.DateTime:   return new Var((DateTime?)  reader.GetDateTime  (ordinal));
                case StandardTypeId.Guid:       return new Var((Guid?)      reader.GetGuid      (ordinal));
            }
            throw new NotImplementedException($"GetVar() {field.typeId}");
        }
    }
}