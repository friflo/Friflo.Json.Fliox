// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Schema.Definition;

// ReSharper disable LoopCanBeConvertedToQuery
namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    /// <summary>
    /// Same value ids as in <see cref="StandardTypeId"/> + enum value <see cref="Array"/>
    /// </summary>
    public enum ColumnType
    {
        None        =  0,
        //
        Boolean     =  1,
        String      =  2,
        // --- integer
        Uint8       =  3,
        Int16       =  4,
        Int32       =  5,
        Int64       =  6,
        // --- NON_CLS integer
        Int8        =  7,
        UInt16      =  8,
        UInt32      =  9,
        UInt64      = 10,
        // --- floating point
        Float       = 11,
        Double      = 12,
        // --- specialized
        BigInteger  = 13,
        DateTime    = 14,
        Guid        = 15,
        JsonValue   = 16,
        JsonKey     = 17,
        JsonEntity  = 19,
        Enum        = 20,
    //  JsonTable   = 21,
        //
        Object      = 22,
        Array       = 23,
    }
    
    public interface IObjectMember { }
    
    public sealed class ColumnInfo : IObjectMember
    {
        public   readonly   int             ordinal;
        public   readonly   bool            isPrimaryKey;
        public   readonly   string          name;           // path: sub.title
        internal readonly   string          memberName;     // leaf: title
        public   readonly   Bytes           nameBytes;      // leaf: title
        public   readonly   ColumnType      type;

        public override     string          ToString() => $"{name} [{ordinal}] : {type}";

        internal ColumnInfo (int ordinal, string name, string memberName, ColumnType type, bool isPrimaryKey) {
            this.ordinal        = ordinal;
            this.name           = name;
            this.memberName     = memberName;
            this.nameBytes      = new Bytes(memberName);
            this.type           = type;
            this.isPrimaryKey   = isPrimaryKey;
        }
    }
    
    public sealed class ObjectInfo : IObjectMember
    {
        private  readonly   string                              memberName;
        internal readonly   Bytes                               nameBytes;  // leaf
        internal readonly   int                                 ordinal;
        //
        private  readonly   Dictionary<BytesHash,ColumnInfo>    columnMap;  // columns in SQL table
        //
        internal readonly   IObjectMember[]                     members;    // JSON members - either of type object or scalar (column value)
        private  readonly   Dictionary<BytesHash,ObjectInfo>    objectMap;  // JSON members of type object { ... }

        public override     string                              ToString() => memberName;

        public ObjectInfo (
            string              memberName,
            List<ColumnInfo>    columns,
            List<ObjectInfo>    objects,
            List<IObjectMember> members,
            int                 ordinal)
        {
            this.ordinal    = ordinal;
            nameBytes       = new Bytes(memberName);
            this.memberName = memberName;
            this.members    = members.ToArray();
            columnMap       = new Dictionary<BytesHash, ColumnInfo>(columns.Count, BytesHash.Equality);
            foreach (var column in columns) {
                columnMap.Add(new BytesHash(new Bytes(column.memberName)), column);
            }
            objectMap       = new Dictionary<BytesHash, ObjectInfo>(objects.Count, BytesHash.Equality);
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
        public   readonly   TableType                       tableType;
        public   readonly   string                          container;
        public   readonly   TypeDef                         type;
        // --- internal
        private  readonly   Dictionary<string, ColumnInfo>  columnMap;
        // ReSharper disable once CollectionNeverQueried.Local
        private  readonly   Dictionary<string, ColumnInfo>  indexMap;
        // ReSharper disable once NotAccessedField.Local
        public   readonly   ObjectInfo                      root;
        public   readonly   char                            colStart;
        public   readonly   char                            colEnd;

        public   override   string                          ToString() => container;

        public TableInfo(
            EntityDatabase  database,
            string          container,
            char            colStart,
            char            colEnd,
            TableType       tableType)
        {
            this.colStart   = colStart;
            this.colEnd     = colEnd;
            this.tableType  = tableType;
            this.container  = container;
            columnMap       = new Dictionary<string, ColumnInfo>();
            indexMap        = new Dictionary<string, ColumnInfo>();
            type            = database.Schema.typeSchema.RootType.FindField(container).type;
            root            = AddTypeFields(type, null, "(Root)", -1);
            keyColumn       = columnMap[type.KeyField.name];
            columns         = new ColumnInfo[columnMap.Count];
            foreach (var pair in columnMap) {
                var column = pair.Value;
                columns[column.ordinal] = column;
            }
        }
        
        private ObjectInfo AddTypeFields(TypeDef type, string prefix, string name, int objOrdinal) {
            var fields      = type.Fields;
            var columnList  = new List<ColumnInfo>();
            var objectList  = new List<ObjectInfo>();
            var memberList  = new List<IObjectMember>();
            foreach (var field in fields) {
                var fieldPath   = prefix == null ? field.name : prefix + "." + field.name;
                var fieldType   = field.type;
                var isScalar    = !field.isArray && !field.isDictionary;
                if (isScalar && fieldType.IsClass) {
                    var objField = new ColumnInfo(columnMap.Count, fieldPath, field.name, ColumnType.Object, false);
                    columnMap.Add(fieldPath, objField);
                    var obj = AddTypeFields(fieldType, fieldPath, field.name, objField.ordinal);
                    objectList.Add(obj);
                    memberList.Add(obj);
                    continue;
                }
                var columnType = (ColumnType)fieldType.TypeId;
                if (tableType == TableType.JsonColumn && !isScalar) {
                    continue;
                }
                if (field.isArray) {
                    columnType = ColumnType.Array;
                } else if (field.isDictionary) {
                    continue;
                }
                var isPrimaryKey = type.KeyField == field;
                var column = new ColumnInfo(columnMap.Count, fieldPath, field.name, columnType, isPrimaryKey);
                columnList.Add(column);
                memberList.Add(column);
                columnMap.Add(fieldPath, column);
                indexMap.Add(fieldPath, column);
            }
            return new ObjectInfo(name, columnList, objectList, memberList, objOrdinal);
        }
    }
}