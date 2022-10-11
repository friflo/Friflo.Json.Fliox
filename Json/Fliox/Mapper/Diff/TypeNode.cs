// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper.Map;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

namespace Friflo.Json.Fliox.Mapper.Diff
{
    internal enum NodeType {
        Root,
        Element,
        Key,
    }
    
    internal readonly struct TypeNode
    {
        /// <summary>Commonly the a property or field name (string). In case of a dictionary a dictionary key</summary>
                        public   readonly   object      key;
                        public   readonly   int         index;
        [Browse(Never)] public   readonly   TypeMapper  typeMapper;

                        public   override   string      ToString() => GetString();
                        
                        public              NodeType    NodeType => index == -1
                                ? ReferenceEquals(key, RootTag) ? NodeType.Root : NodeType.Key
                                : NodeType.Element;
                        
        internal static readonly            object      RootTag = "(Root)";

        internal TypeNode(object key, int index, TypeMapper typeMapper) {
            this.key        = key;
            this.index      = index;
            this.typeMapper = typeMapper;
        }

        private string GetString() {
            switch (NodeType) {
                case NodeType.Root:     return "(Root)";
                case NodeType.Key:      return key.ToString();
                case NodeType.Element:  return $"[{index}]";
                default:    throw new InvalidOperationException("unexpected case");
            }
        }
    }
}