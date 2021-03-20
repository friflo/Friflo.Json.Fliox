// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Diff
{
    public class Patcher : IDisposable
    {
        public  readonly    TypeCache       typeCache;
        private readonly    JsonReader      jsonReader;
        private             string          json;
        private             int             pathPos;
        private readonly    List<string>    pathNodes = new List<string>();
        
        public Patcher(JsonReader jsonReader) {
            this.jsonReader = jsonReader;
            this.typeCache = jsonReader.TypeCache;
        }

        public void Dispose() {

        }

        public void Patch<T>(TypeMapper<T> mapper, object root, Patch patch) {
            var replace = (PatchReplace) patch;
            json = replace.value.json;
            pathPos = 0;
            pathNodes.Clear();
            string path = patch.Path;
            int last = 1;
            int len = path.Length;
            for (int n = 1; n < len; n++) {
                if (path[n] == '/') {
                    var pathNode = patch.Path.Substring(last, n - last);
                    pathNodes.Add(pathNode);
                    last = n + 1;
                }
            }
            var lastNode = patch.Path.Substring(last, len - last);
            pathNodes.Add(lastNode);
            
            mapper.PatchObject(this, root);
        }

        public bool Walk(PropField propField, object obj, out object value) {
            if (!propField.name.Equals(pathNodes[pathPos])) {
                value = null;
                return false;
            }

            if (++pathPos >= pathNodes.Count) {
                value = jsonReader.ReadObject(json, propField.fieldType.type);
                return true;
            }
            value = propField.GetField(obj);
            propField.fieldType.PatchObject(this, value);
            return true;
        }
    }
}