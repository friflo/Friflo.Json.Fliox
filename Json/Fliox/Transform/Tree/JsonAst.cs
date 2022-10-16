// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Tree
{
    public struct JsonAst
    {
        private readonly    List<JsonAstNode>   nodes;
        private             byte[]              buf;
        private             int                 pos;
        public              JsonAstNodeDebug[]  DebugNodes => GetDebugNodes();
        internal            byte[]              Buf => buf;
        
        internal JsonAst(List<JsonAstNode> nodes) {
            this.nodes      = nodes;
            buf             = new byte[32];
            pos             = -1;
        }
        
        internal void Init() {
            nodes.Clear();
            var constants   = JsonAstSerializer.NullTrueFalse;
            pos             = constants.Length;
            Buffer.BlockCopy(constants, 0, buf, 0, pos);
        }
        
        internal JsonAstSpan AddSpan (in Bytes bytes) {
            var len     = bytes.Len;
            int destPos = Reserve(len);
            Buffer.BlockCopy(bytes.buffer.array, bytes.start, buf, destPos, len);
            return new JsonAstSpan(destPos, len);
        }
        
        private int Reserve (int len) {
            int curPos  = pos;
            int newLen  = curPos + len;
            int bufLen  = buf.Length;
            if (curPos + len > bufLen) {
                var doubledLen = 2 * bufLen;
                if (newLen < doubledLen) {
                    newLen = doubledLen;
                }
                var newBuffer = new byte [newLen];
                Buffer.BlockCopy(buf, 0, newBuffer, 0, curPos);
                buf = newBuffer;
            }
            pos += len;
            return curPos;
        }
        
        private JsonAstNodeDebug[] GetDebugNodes() {
            var count       = nodes.Count;
            var debugNodes  = new JsonAstNodeDebug[count];
            for (int n = 0; n < count; n++) {
                debugNodes[n] = new JsonAstNodeDebug(nodes[n], buf);
            }
            return debugNodes;
        }
    }
}