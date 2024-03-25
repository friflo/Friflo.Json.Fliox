// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper.Map;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

namespace Friflo.Json.Fliox.Mapper.Diff
{
    internal enum NodeType {
        Root    = 1,
        Element = 2,
        Key     = 3,
    }
    
    internal readonly struct TypeNode
    {
        /// <summary>Either a <see cref="PropField"/> or the key of a <see cref="Dictionary{TKey,TValue}"/></summary>
                        public   readonly   object      key;
                        public   readonly   int         index;
        [Browse(Never)] public   readonly   TypeMapper  mapper;

                        public   override   string      ToString() => GetString();
                        
                        public              NodeType    NodeType => index == -1
                                ? ReferenceEquals(key, RootTag) ? NodeType.Root : NodeType.Key
                                : NodeType.Element;
                        
        internal static readonly            object      RootTag = "(Root)";

        internal TypeNode(object key, int index, TypeMapper mapper) {
            this.key    = key;
            this.index  = index;
            this.mapper = mapper;
        }

        private string GetString() {
            switch (NodeType) {
                case NodeType.Root:     return "(Root)";
                case NodeType.Key:      return key is PropField field ? field.name : key.ToString();
                case NodeType.Element:  return $"[{index}]";
                default:    throw new InvalidOperationException("unexpected case");
            }
        }
    }
}