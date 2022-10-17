// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Tree
{
    public struct JsonAstNode {
        public              int         Next => next;
        
        public    readonly  JsonAstSpan key;
        public    readonly  JsonAstSpan value;
        public    readonly  JsonEvent   type;
        internal            int         next;

        internal JsonAstNode (JsonEvent type, in JsonAstSpan key, in JsonAstSpan value, int next) {
            this.key    = key;
            this.value  = value;
            this.type   = type;
            this.next   = next;
        }
    }
    
    public readonly struct JsonAstNodeDebug {
        private readonly    JsonAstNode node;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly    byte[]      buf;
        
        private string      Key     => node.key.start   == 0 ? null : Encoding.UTF8.GetString(buf, node.key.start,   node.key.len);
        private string      Value   => node.value.start == 0 ? null : Encoding.UTF8.GetString(buf, node.value.start, node.value.len);
        // ReSharper disable once InconsistentNaming - want listing at bottom in debugger 
        private int         _Next   => node.next;
        // ReSharper disable once InconsistentNaming - want listing at bottom in debugger 
        private JsonEvent   _Type   => node.type;

        internal JsonAstNodeDebug (in JsonAstNode node, byte[] buf) {
            this.node   = node;
            this.buf    = buf;
        }

        public override string ToString() => GetString();
        
        private string GetTypeLabel() {
            switch (node.type) {
                case JsonEvent.None:        return "None";
                case JsonEvent.ArrayStart:  return "[";
                case JsonEvent.ArrayEnd:    return "]";
                case JsonEvent.ObjectStart: return "{";
                case JsonEvent.ObjectEnd:   return "}";
            }
            return "";
        }
        
        private string GetString() {
            if (node.type == JsonEvent.None)
                return "reserved";
            var typeStr = GetTypeLabel();
            var sb = new StringBuilder();
            if (Key != null) {
                sb.Append('"');
                sb.Append(Key);
                sb.Append("\": ");
            }
            sb.Append(' ');
            sb.Append(typeStr);
            if (Value != null) {
                if (node.type == JsonEvent.ValueString) {
                    sb.Append('"');
                    sb.Append(Value);
                    sb.Append('"');
                } else {
                    sb.Append(Value);
                }
            }
            if (node.next == -1) {
                // sb.Append("    last");    
            } else {
                sb.Append("    next: ");
                sb.Append(node.next);
            }
            return sb.ToString();
        }
    }
}