// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema
{
    public class Package
    {
        /// contain all types and their generated piece of code for each type
        public  readonly    List<EmitType>  emitTypes   = new List<EmitType>();
        /// contain all imports used by all types in a package
        public  readonly    HashSet<Type>   imports     = new HashSet<Type>();
        /// the generated code used as package header. Typically all imports (using statements)
        public              string          header;
    }
    
    public class Generator
    {
        /// map of all <see cref="TypeMapper"/>'s required by the types provided for schema generation
        public   readonly    IReadOnlyDictionary<Type, TypeMapper>  typeMappers;
        /// map of all emitted types and their emitted code 
        public   readonly    Dictionary<TypeMapper, EmitType>       emitTypes   = new Dictionary<TypeMapper, EmitType>();
        /// map of all generated packages. key: namespace  
        public   readonly    Dictionary<string, Package>            packages    = new Dictionary<string, Package>();
        /// set of generated files and their source content. key: file name
        public   readonly    Dictionary<string, string>             files       = new Dictionary<string, string>();

        public Generator (TypeStore typeStore) {
            typeMappers     = typeStore.GetTypeMappers();
        }
        
        public static string Indent(int max, string str) {
            return new string(' ', Math.Max(max - str.Length, 0));
        }
        
        public bool IsUnionType (Type type) {
            var instanceFactory = typeMappers[type].instanceFactory;
            return instanceFactory != null;
        }

        public void AddEmitType(EmitType emit) {
            emitTypes.Add(emit.mapper, emit);
        }
        
        public void GroupTypesByNamespace() {
            foreach (var pair in emitTypes) {
                EmitType    emit    = pair.Value;
                var         ns      = emit.mapper.type.Namespace;
                if (!packages.TryGetValue(ns, out var package)) {
                    packages.Add(ns, package = new Package());
                }
                package.emitTypes.Add(emit);
                package.imports.UnionWith(emit.imports);
            }
        }
        
        public void CreateFiles(StringBuilder sb, Func<string, string> toFilename) {
            foreach (var pair in packages) {
                string      ns      = pair.Key;
                Package     package = pair.Value;
                sb.Clear();
                sb.AppendLine(package.header);
                foreach (var result in package.emitTypes) {
                    sb.AppendLine(result.content);
                }
                var filename = toFilename(ns);
                files.Add(filename, sb.ToString());
            }
        }
        
        public void WriteFiles(string folder) {
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
    
    public static class GeneratorExtension
    {
        public static int MaxLength<TSource>(this ICollection<TSource> source, Func<TSource, int> selector) {
            if (source.Count == 0)
                return 0;
            return source.Max(selector); 
        }
    }
}