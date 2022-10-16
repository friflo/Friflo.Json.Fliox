// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;

namespace Friflo.Json.Fliox.Transform.Tree
{
    public readonly struct JsonAstSpan {
        internal  readonly  int         start;  // 0 => null
        internal  readonly  int         len;

        internal JsonAstSpan (int start, int len) {
            this.start  = start;
            this.len    = len;
        }

        public override string ToString() => start == 0 ? null : "set";

        // only for debugging
        public              string      Value (JsonAst ast) {
            if (start == 0)
                return null;
            return Encoding.UTF8.GetString(ast.Buf, start, len);
        }
    }
}