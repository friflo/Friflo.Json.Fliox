// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public sealed class ColumnInfo
    {
        public readonly     string          name;
        public readonly     StandardTypeId  typeId;

        public override     string          ToString() => $"{name} : {typeId}";

        public ColumnInfo (string name, StandardTypeId typeId) {
            this.name   = name;
            this.typeId = typeId;    
        }
    }
    
    public sealed class TableInfo
    {
        private  readonly   string                          container;
        public   readonly   ColumnInfo                      keyColumn;
        public   readonly   Dictionary<string, ColumnInfo>  columns;
        private  readonly   Dictionary<string, ColumnInfo>  indexes;

        public   override   string                          ToString() => container;

        public TableInfo(EntityDatabase database, string container) {
            this.container  = container;
            columns         = new Dictionary<string, ColumnInfo>();
            indexes         = new Dictionary<string, ColumnInfo>();
            var type        = database.Schema.typeSchema.RootType.FindField(container).type;
            AddTypeFields(type, null);
            keyColumn       = columns[type.KeyField.name];
        }
        
        private void AddTypeFields(TypeDef type, string prefix) {
            var fields  = type.Fields;
            foreach (var field in fields) {
                var fieldPath   = prefix == null ? field.name : prefix + "." + field.name;
                var fieldType   = field.type;
                if (fieldType.IsClass) {
                    AddTypeFields(fieldType, fieldPath);
                    continue;
                }
                var typeId      = fieldType.TypeId;
                if (typeId == StandardTypeId.None) {
                    continue;
                }
                var isScalar    = !field.isArray && !field.isDictionary;
                if (isScalar) {
                    var column      = new ColumnInfo(fieldPath, typeId);
                    columns.Add(fieldPath, column);
                    indexes.Add(fieldPath, column);
                }
            }
        }
    }
}