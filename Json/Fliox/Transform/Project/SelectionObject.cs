// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Project
{
    /// <summary>
    /// Contain the <see cref="name"/> for a GraphQL type.
    /// The <see cref="name"/> is returned for selection sets containing the field: __typename  
    /// </summary>
    public readonly struct SelectionObject
    {
        public    readonly  Utf8String          name;
        private   readonly  SelectionField[]    fields;
        internal  readonly  SelectionUnion[]    unions; // can be null

        public   override   string              ToString() {
            if (name.IsNull)
                return "<no object type>";
            if (fields == null)
                return $"name: {name.AsString()}, fields: null";
            return $"name: {name.AsString()}, fields: {fields.Length}";
        }

        public  SelectionObject (in Utf8String typeName, SelectionField[] fields, SelectionUnion[] unions) {
            this.name   = typeName;
            this.fields = fields;
            this.unions = unions;
        }
        
#if !UNITY_5_3_OR_NEWER
        public SelectionField FindField(in ReadOnlySpan<char> name) {
            if (fields == null) {
                return default;
            }
            for (int n = 0; n < fields.Length; n++) {
                var node  = fields[n];
                if (!node.name.AsSpan().SequenceEqual(name))
                    continue;
                return node;
            }
            return default;
        }
        
        public SelectionUnion FindUnion(in ReadOnlySpan<char> name) {
            if (unions == null) {
                return default;
            }
            for (int n = 0; n < unions.Length; n++) {
                var union  = unions[n];
                if (!union.typename.AsSpan().SequenceEqual(name))
                    continue;
                return union;
            }
            return default;
        }
#endif
    }
    
    public readonly struct SelectionField
    {
        public   readonly   string          name;
        public   readonly   SelectionObject objectType;
        
        public   override   string          ToString() {
            if (name == null)
                return "<no object field>";
            return $"{name} : {objectType.name.AsString()}";
        }

        public SelectionField (string fieldName, in SelectionObject objectType) {
            this.name       = fieldName;
            this.objectType = objectType;
        }
    }
    
    public readonly struct SelectionUnion
    {
        public   readonly   Utf8String      discriminant;
        public   readonly   Utf8String      typenameUtf8;
        public   readonly   string          typename;
        public   readonly   SelectionObject unionObject;

        public   override   string          ToString() => discriminant.AsString();

        public SelectionUnion (in Utf8String discriminant, in Utf8String typenameUtf8, string typename, in SelectionObject unionObject) {
            this.discriminant   = discriminant;
            this.typenameUtf8   = typenameUtf8;
            this.typename       = typename;
            this.unionObject    = unionObject;
        }
    }
}