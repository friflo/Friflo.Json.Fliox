// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public sealed class SQL2Object : IDisposable
    {
        public StringBuilder        sb      = new StringBuilder();
        public BinaryDbDataReader   reader  = new BinaryDbDataReader();
        
        
        public void Dispose() { }
    }
}