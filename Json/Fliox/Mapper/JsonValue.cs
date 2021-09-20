// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Mapper
{
    public struct JsonValue
    {
        public JsonUtf8 json;
        
        public override string ToString() => json.AsString();
        
        public JsonValue (JsonUtf8 json) {
            this.json  = json;
        }
        
        /// <summary> Prefer using <see cref="JsonValue(JsonUtf8)"/> </summary>
        public JsonValue (string json) {
            this.json  = new JsonUtf8(json);
        }
        
        public JsonValue (byte[] utf8Array) {
            this.json  = new JsonUtf8(utf8Array);
        }
    }
}