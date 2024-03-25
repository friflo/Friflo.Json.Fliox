// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;

namespace Friflo.Json.Fliox.Transform.Query
{
    internal readonly struct AppendCx {
        public readonly StringBuilder   sb;
        
        public AppendCx (StringBuilder sb) {
            this.sb     = sb;
        }

        public override string ToString() => sb.ToString();

        public void Append(string str) {
            sb.Append(str);
        }
    }
}