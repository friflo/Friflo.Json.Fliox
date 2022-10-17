// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Tree
{
    // Could be a struct but this requires replacing the whole node to update field next
    public struct JsonAstNode {
        public              int         Next => next;
        
        public    readonly  JsonAstSpan key;
        public    readonly  JsonAstSpan value;
        public    readonly  JsonEvent   type;
        /// <summary>Is not -1 if node <see cref="type"/> is <see cref="JsonEvent.ObjectStart"/> or <see cref="JsonEvent.ArrayStart"/></summary> 
        public    readonly  int         child;
        /// <summary>Is not -1 if the node has a successor - an object member or an array element</summary> 
        internal            int         next;

        public    override  string      ToString() => GetString();

        internal JsonAstNode (JsonEvent type, in JsonAstSpan key, in JsonAstSpan value, int child, int next) {
            this.key    = key;
            this.value  = value;
            this.type   = type;
            this.child  = child;
            this.next   = next;
        }
        
        private string GetString() {
            var sb = new StringBuilder();
            sb.Append(type);
            if (type != JsonEvent.None) {
                if (child != -1) {
                    sb.Append(" child: ");
                    sb.Append(child);
                }
                if (next != -1) {
                    sb.Append(" next: ");
                    sb.Append(next);
                }
            }
            return sb.ToString();            
        }
    }
    
    public readonly struct JsonAstNodeDebug {
        private readonly    JsonAstNode node;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly    byte[]      buf;
        
        private string      Key     => node.key.start   == 0 ? null : Encoding.UTF8.GetString(buf, node.key.start,   node.key.len);
        private string      Value   => node.value.start == 0 ? null : Encoding.UTF8.GetString(buf, node.value.start, node.value.len);
        // ReSharper disable once InconsistentNaming - want listing at bottom in debugger 
        private int        _Child   => node.child;
        // ReSharper disable once InconsistentNaming - want listing at bottom in debugger 
        private int        _Next    => node.next;
        // ReSharper disable once InconsistentNaming - want listing at bottom in debugger 
        private JsonEvent  _Type    => node.type;

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
            if (node.next != -1 || node.child != -1) {
                sb.Append(' ');
                int len = sb.Length;
                for (int n = 30; n >= len; n--) {
                    sb.Append(' ');
                }
                if (node.child != -1) {
                    sb.Append(" child: ");
                    sb.Append(node.child);
                }
                if (node.next != -1) {
                    sb.Append(" next: ");
                    sb.Append(node.next);
                }
            }
            return sb.ToString();
        }
    }
}