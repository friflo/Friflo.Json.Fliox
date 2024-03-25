// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.GraphQL;
using Friflo.Json.Fliox.Transform.Project;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal readonly struct Query
    {
        internal  readonly  string          name;
        internal  readonly  QueryType       type;
        internal  readonly  ShortString     container;
        internal  readonly  SelectionNode   selection;
        internal  readonly  int             taskIndex;
        internal  readonly  SyncRequestTask task;
        internal  readonly  bool            selectAll;
        internal  readonly  QueryError?     error;
        // ReSharper disable once NotAccessedField.Local
        private   readonly  string          resolverName; // debug only
        
        public    override  string          ToString()  => $"{type}: {name}";

        internal Query(string resolverName, string alias, QueryType type, in ShortString container, in SelectionNode selection, int taskIndex, in QueryRequest queryRequest) {
            this.name           = alias ?? resolverName;
            this.resolverName   = resolverName;
            this.type           = type;
            this.container      = container;
            this.selection      = selection;
            this.taskIndex      = taskIndex;
            this.task           = queryRequest.task;
            this.selectAll      = queryRequest.selectAll;
            this.error          = queryRequest.error;
        }
    }
    
    internal readonly struct QueryResolver
    {
        internal  readonly  ShortString     name;
        internal  readonly  QueryType       queryType;
        internal  readonly  SelectionObject resultObject;
        
        /// <summary> only: <see cref="QueryType.Query"/> and <see cref="QueryType.Read"/> </summary>
        internal  readonly  ShortString     container;
        /// <summary> only: <see cref="QueryType.Message"/> and <see cref="QueryType.Command"/> </summary>
        internal  readonly  bool            hasParam;
        /// <summary> only: <see cref="QueryType.Message"/> and <see cref="QueryType.Command"/> </summary>
        internal  readonly  bool            paramRequired;  // message / command only

        public    override  string          ToString() => $"{queryType}: {name.AsString()}";
        
        /// <summary> constructor for database messages / commands </summary>
        internal QueryResolver(string name, QueryType queryType, FieldDef param, TypeDef resultType) {
            this.name       = new ShortString(name);
            this.queryType  = queryType;
            this.container  = default;
            hasParam        = param != null;
            paramRequired   = param != null && param.required;
            resultObject    = resultType != null ? CreateSelectionObject(resultType.nameUtf8, resultType) : default;
        }
        
        /// <summary> constructor for container methods </summary>
        internal QueryResolver(string name, QueryType queryType, string container, TypeDef entityType, IUtf8Buffer buffer) {
            this.name       = new ShortString(Gql.MethodName(name, container));
            this.queryType  = queryType;
            this.container  = new ShortString(container);
            hasParam        = false;
            paramRequired   = false;
            resultObject    = CreateResultType(queryType, container, entityType, buffer);
        }
        
        private static SelectionObject CreateResultType(QueryType queryType, string container, TypeDef entityType, IUtf8Buffer buffer) {
            switch (queryType) {
                case QueryType.Query:
                    var resultName      = Gql.MethodResult("query", container);
                    var resultNameUtf8  = buffer.Add(resultName);
                    var itemsType       = CreateSelectionObject(entityType.nameUtf8, entityType);
                    var fields          = new [] { new SelectionField("items", itemsType) };
                    var unions          = CreateUnionsOfType (entityType);
                    return new SelectionObject(resultNameUtf8, fields, unions);
                case QueryType.Read:
                    return CreateSelectionObject(entityType.nameUtf8, entityType);
                case QueryType.Count:
                    return default;
                case QueryType.Create:
                case QueryType.Upsert:
                case QueryType.Delete:
                    var errorType = buffer.GetOrAdd(nameof(EntityError));
                    return new SelectionObject(errorType, null, null);
                default:
                    throw new InvalidOperationException($"unknown queryType: {queryType}");
            }
        }
        
        private static SelectionObject CreateSelectionObject(in Utf8String typeName, TypeDef type) {
            if (type == null) {
                return default;
            }
            if (!type.IsClass) {
                return default;
            }
            var typeFields      = type.Fields;
            var selectionFields = new List<SelectionField>(typeFields.Count);
            foreach (var fieldDef in typeFields) {
                var fieldType = fieldDef.type; 
                if (!fieldType.IsClass)
                    continue;
                var fieldSelectionType  = CreateSelectionObject(fieldType.nameUtf8, fieldType);
                selectionFields.Add(new SelectionField(fieldDef.name, fieldSelectionType));
            }
            var selectionFieldsArray = selectionFields.Count == 0 ? null : selectionFields.ToArray();
            var unions = CreateUnionsOfType (type);
            return new SelectionObject(typeName, selectionFieldsArray, unions);
        }
        
        private static SelectionUnion[] CreateUnionsOfType(TypeDef type) {
            var unionType = type.UnionType;
            if (unionType == null)
                return null;
            var types   = unionType.types;
            var result  = new  SelectionUnion[types.Count];
            for (int n = 0; n < types.Count; n++) {
                var unionItem   = types[n];
                var unionObject = CreateSelectionObject(unionItem.typeDef.nameUtf8, unionItem.typeDef);
                var typeDef     = unionItem.typeDef;
                result[n]       = new SelectionUnion (unionItem.discriminantUtf8, typeDef.nameUtf8, typeDef.Name, unionObject);
            }
            return result;
        }
    }
    
    internal enum QueryType {
        Query,
        Count,
        Read,
        Create,
        Upsert,
        Delete,
        Command,
        Message
    }
}
