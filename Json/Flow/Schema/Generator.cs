// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema
{
    public class Generator
    {
        public  readonly    IReadOnlyDictionary<Type, TypeMapper>   typeMappers;
        public  readonly    string                                  folder;
        
        public  readonly    Dictionary<TypeMapper, EmitResult>      emitTypes       = new Dictionary<TypeMapper, EmitResult>();
        public  readonly    Dictionary<string, List<EmitResult>>    namespaceTypes  = new Dictionary<string, List<EmitResult>>();
        public  readonly    Dictionary<string, string>              files           = new Dictionary<string, string>();

        public Generator (string folder, TypeStore typeStore) {
            this.folder     = folder;
            typeMappers     = typeStore.GetTypeMappers();
        }

        public void AddEmitType(EmitResult emit) {
            emitTypes.Add(emit.mapper, emit);
        }
        
        public void GroupTypesByNamespace() {
            foreach (var pair in emitTypes) {
                EmitResult  emit    = pair.Value;
                var         ns      = emit.mapper.type.Namespace;
                if (!namespaceTypes.TryGetValue(ns, out var list)) {
                    namespaceTypes.Add(ns, list = new List<EmitResult>());
                }
                list.Add(emit);
            }
        }
        
        public void CreateFiles(StringBuilder sb, Func<string, string> toFilename) {
            foreach (var pair in namespaceTypes) {
                string              ns      = pair.Key;
                List<EmitResult>    results = pair.Value;
                sb.Clear();
                foreach (var result in results) {
                    sb.AppendLine(result.content);
                }
                var filename = toFilename(ns);
                files.Add(filename, sb.ToString());
            }
        }
        
        public void WriteFiles() {
            foreach (var file in files) {
                var filename    = file.Key;
                var content     = file.Value;
                var path = $"{folder}/{filename}";
                var lastSlash = path.LastIndexOf("/", StringComparison.InvariantCulture);
                var fileFolder = lastSlash == -1 ? folder : path.Substring(0, lastSlash);
                Directory.CreateDirectory(fileFolder);
                File.WriteAllText(path, content, Encoding.UTF8);
            }
        }
        
    }
}