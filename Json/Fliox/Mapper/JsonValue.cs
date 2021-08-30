// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Mapper
{
    public struct JsonValue
    {
        public string       json;
        
        public override string ToString() => json ?? "null";
    }
}