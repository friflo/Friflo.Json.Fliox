// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Schema.Definition;

// ReSharper disable LoopCanBeConvertedToQuery
namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public sealed class ColumnInfo
    {
        public   readonly   int             ordinal;
        public   readonly   bool            isPrimaryKey;
        public   readonly   string          name;           // path: sub.title
        internal readonly   string          memberName;     // leaf: title
        internal readonly   Bytes           nameBytes;      // leaf: title
        public   readonly   StandardTypeId  typeId;

        public override     string          ToString() => $"{name} [{ordinal}] : {typeId}";

        public ColumnInfo (int ordinal, string name, string memberName, StandardTypeId typeId, bool isPrimaryKey) {
            this.ordinal        = ordinal;
            this.name           = name;
            this.memberName     = memberName;
            this.nameBytes      = new Bytes(name);
            this.typeId         = typeId;
            this.isPrimaryKey   = isPrimaryKey;
        }
    }
    
    public sealed class ObjectInfo
    {
        private  readonly   string                              memberName;
        internal readonly   ColumnInfo[]                        columns;
        private  readonly   ObjectInfo[]                        objects;
        private  readonly   Dictionary<BytesHash,ColumnInfo>    columnMap;
        private  readonly   Dictionary<BytesHash,ObjectInfo>    objectMap;

        public override     string                              ToString() => memberName;

        public ObjectInfo (string memberName, ColumnInfo[] columns, ObjectInfo[] objects) {
            this.memberName = memberName;
            this.columns    = columns;
            this.objects    = objects;
            columnMap       = new Dictionary<BytesHash, ColumnInfo>(columns.Length, BytesHash.Equality);
            foreach (var column in columns) {
                columnMap.Add(new BytesHash(new Bytes(column.memberName)), column);
            }
            objectMap       = new Dictionary<BytesHash, ObjectInfo>(objects.Length, BytesHash.Equality);
            foreach (var obj in objects) {
                objectMap.Add(new BytesHash(new Bytes(obj.memberName)), obj);
            }
        }
        
        public ColumnInfo FindColumn(in Bytes name) {
            var key = new BytesHash(name);
            columnMap.TryGetValue(key, out var result);
            return result;
        }
        
        public ObjectInfo FindObject(in Bytes name) {
            var key = new BytesHash(name);
            objectMap.TryGetValue(key, out var result);
            return result;
        }
    }
    
    public sealed class TableInfo
    {
        public   readonly   ColumnInfo[]                    columns;
        public   readonly   ColumnInfo                      keyColumn;
        // --- internal
        private  readonly   string                          container;
        private  readonly   Dictionary<string, ColumnInfo>  columnMap;
        // ReSharper disable once CollectionNeverQueried.Local
        private  readonly   Dictionary<string, ColumnInfo>  indexMap;
        // ReSharper disable once NotAccessedField.Local
        public   readonly   ObjectInfo                      root;

        public   override   string                          ToString() => container;

        public TableInfo(EntityDatabase database, string container) {
            this.container  = container;
            columnMap       = new Dictionary<string, ColumnInfo>();
            indexMap        = new Dictionary<string, ColumnInfo>();
            var type        = database.Schema.typeSchema.RootType.FindField(container).type;
            root            = AddTypeFields(type, null, "(Root)");
            keyColumn       = columnMap[type.KeyField.name];
            columns         = new ColumnInfo[columnMap.Count];
            foreach (var pair in columnMap) {
                var column = pair.Value;
                columns[column.ordinal] = column;
            }
        }
        
        private ObjectInfo AddTypeFields(TypeDef type, string prefix, string name) {
            var fields      = type.Fields;
            var columnList  = new List<ColumnInfo>();
            var objectList  = new List<ObjectInfo>();
            foreach (var field in fields) {
                var fieldPath   = prefix == null ? field.name : prefix + "." + field.name;
                var fieldType   = field.type;
                var isScalar    = !field.isArray && !field.isDictionary;
                if (isScalar && fieldType.IsClass) {
                    var obj = AddTypeFields(fieldType, fieldPath, field.name);
                    objectList.Add(obj);
                    continue;
                }
                var typeId      = fieldType.TypeId;
                if (typeId == StandardTypeId.None) {
                    continue;
                }
                if (isScalar) {
                    var isPrimaryKey = type.KeyField == field;
                    var column = new ColumnInfo(columnMap.Count, fieldPath, field.name, typeId, isPrimaryKey);
                    columnList.Add(column);
                    columnMap.Add(fieldPath, column);
                    indexMap.Add(fieldPath, column);
                }
            }
            return new ObjectInfo(name, columnList.ToArray(), objectList.ToArray());
        }
    }
}