// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Mapper
{
    public struct JsonValue
    {
        public Utf8Json json;
        
        public override string ToString() => json.AsString();
        
        public JsonValue (Utf8Json json) {
            this.json  = json;
        }
        
        /// <summary> Prefer using <see cref="JsonValue(Utf8Json)"/> </summary>
        public JsonValue (string json) {
            this.json  = new Utf8Json(json);
        }
        
        public JsonValue (byte[] utf8Array) {
            this.json  = new Utf8Json(utf8Array);
        }
    }
}