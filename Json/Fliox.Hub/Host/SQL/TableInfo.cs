// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public class ColumnInfo
    {
        public readonly     string          name;
        public readonly     StandardTypeId  typeId;

        public override     string          ToString() => $"{name} : {typeId}";

        public ColumnInfo (string name, StandardTypeId typeId) {
            this.name   = name;
            this.typeId = typeId;    
        }
    }
    
    public class TableInfo
    {
        public   readonly   ColumnInfo                      keyColumn;
        public   readonly   Dictionary<string, ColumnInfo>  columns;
        private  readonly   Dictionary<string, ColumnInfo>  indexes;
        
        public TableInfo(EntityDatabase database, string container) {
            columns     = new Dictionary<string, ColumnInfo>();
            indexes     = new Dictionary<string, ColumnInfo>();
            var type    = database.Schema.typeSchema.RootType.FindField(container).type;
            var fields  = type.Fields;
            foreach (var field in fields) {
                var typeId      = field.type.TypeId;
                if (typeId == StandardTypeId.None) {
                    continue;
                }
                var isScalar    = !field.isArray && !field.isDictionary;
                var column      = new ColumnInfo(field.name, typeId);
                if (isScalar) {
                    columns.Add(field.name, column);
                    indexes.Add(field.name, column);
                }
                if (type.KeyField == field) {
                    keyColumn = column;
                }
            }
        }
    }
}