// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Tree
{
    public readonly struct JsonAstNode {
        internal  readonly  JsonEvent   type;
        internal  readonly  Utf8String  key;
        internal  readonly  Utf8String  value;
        internal  readonly  int         next;

        internal JsonAstNode (JsonEvent type, Utf8String key, Utf8String value, int next) {
            this.type   = type;
            this.key    = key;
            this.value  = value;
            this.next   = next;
        }

        public override string ToString() => GetString();
        
        private string GetTypeLabel() {
            switch (type) {
                case JsonEvent.ArrayStart:  return "[";
                case JsonEvent.ArrayEnd:    return "]";
                case JsonEvent.ObjectStart: return "{";
                case JsonEvent.ObjectEnd:   return "}";
            }
            return "";
        }
        
        private string GetString() {
            var typeStr = GetTypeLabel();
            var sb = new StringBuilder();
            if (!key.IsNull) {
                sb.Append('"');
                sb.Append(key);
                sb.Append("\": ");
            }
            sb.Append(' ');
            sb.Append(typeStr);
            if (!value.IsNull) {
                if (type == JsonEvent.ValueString) {
                    sb.Append('"');
                    sb.Append(value.AsString());
                    sb.Append('"');
                } else {
                    sb.Append(value.AsString());
                }
            }
            if (next == -1) {
                sb.Append("    last");    
            } else {
                sb.Append("    next: ");
                sb.Append(next);
            }
            return sb.ToString();
        }
    }

    public readonly struct JsonAst
    {
        private readonly List<JsonAstNode> nodes;
        
        public JsonAst(List<JsonAstNode> nodes) {
            this.nodes = nodes;
        }
    }
}