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
    public class Package
    {
        public  readonly    List<EmitResult>    emitResults = new List<EmitResult>();
        public  readonly    HashSet<Type>       imports = new HashSet<Type>();
        public              string              header;
    }
    
    public class Generator
    {
        public   readonly    IReadOnlyDictionary<Type, TypeMapper>  typeMappers;
        public   readonly    string                                 folder;
        
        public   readonly    Dictionary<TypeMapper, EmitResult>     emitTypes   = new Dictionary<TypeMapper, EmitResult>();
        public   readonly    Dictionary<string, Package>            packages    = new Dictionary<string, Package>();
        public   readonly    Dictionary<string, string>             files       = new Dictionary<string, string>();

        public Generator (string folder, TypeStore typeStore) {
            this.folder     = folder;
            typeMappers     = typeStore.GetTypeMappers();
        }
        
        public static string Indent(int max, string str) {
            return new string(' ', Math.Max(max - str.Length, 0));
        }
        
        public bool IsUnionType (Type type) {
            var instanceFactory = typeMappers[type].instanceFactory;
            return instanceFactory != null;
        }

        public void AddEmitType(EmitResult emit) {
            emitTypes.Add(emit.mapper, emit);
        }
        
        public void GroupTypesByNamespace() {
            foreach (var pair in emitTypes) {
                EmitResult  emit    = pair.Value;
                var         ns      = emit.mapper.type.Namespace;
                if (!packages.TryGetValue(ns, out var package)) {
                    packages.Add(ns, package = new Package());
                }
                package.emitResults.Add(emit);
                package.imports.UnionWith(emit.imports);
            }
        }
        
        public void CreateFiles(StringBuilder sb, Func<string, string> toFilename) {
            foreach (var pair in packages) {
                string      ns      = pair.Key;
                Package     package = pair.Value;
                sb.Clear();
                sb.AppendLine(package.header);
                foreach (var result in package.emitResults) {
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
        
        public TypeMapper GetPolymorphBaseMapper(Type type) {
            var baseType = type.BaseType;
            if (baseType == null)
                throw new InvalidOperationException("");
            TypeMapper mapper;
            
            // When searching for polymorph base class there may be are classes in this hierarchy. E.g. BinaryBoolOp. 
            // If these classes may have a protected constructor they need to be skipped. These classes have no TypeMapper. 
            while (!typeMappers.TryGetValue(baseType, out mapper)) {
                baseType = baseType.BaseType;
                if (baseType == null)
                    throw new InvalidOperationException("");
            }
            return mapper;
        }
        
    }
}