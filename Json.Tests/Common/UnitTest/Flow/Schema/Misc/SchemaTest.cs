// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.Definition;
using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Flow.Schema.Utils;
using NUnit.Framework;


namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema.Misc
{
    public static class SchemaTest
    {
        [Test]
        public static void TestTopologicalSort() {
            var a = new Item("A");
            var c = new Item("C");
            var f = new Item("F");
            var h = new Item("H");
            var d = new Item("D", a);
            var g = new Item("G", f, h);
            var e = new Item("E", d, g);
            var b = new Item("B", c, e);

            var unsorted = new[] { a, b, c, d, e, f, g, h };

            var sorted = TopologicalSort.Sort(unsorted, x => x.dependencies);
            
            var expect = new[] { a, c, d, f, h, g, e, b};
            Assert.AreEqual (expect, sorted);
        }

        public class Item {
            private readonly    string  name;
            public  readonly    Item[]  dependencies;

            public override string  ToString() => name;

            public Item(string name, params Item[] dependencies) {
                this.name = name;
                this.dependencies = dependencies;
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private static void EnsureSymbol(string _) {}
        
        // ReSharper disable once UnusedMember.Local
        private static void EnsureApiAccess() {
            EnsureSymbol(nameof(Generator.files));
            EnsureSymbol(nameof(Generator.fileEmits));
            EnsureSymbol(nameof(Generator.types));
            EnsureSymbol(nameof(Generator.fileExt));
            
            EnsureSymbol(nameof(EmitType.type));

            EnsureSymbol(nameof(EmitFile.imports));
            EnsureSymbol(nameof(EmitFile.header));
            EnsureSymbol(nameof(EmitFile.footer));
            EnsureSymbol(nameof(EmitFile.emitTypes));
            
            EnsureSymbol(nameof(TypeContext.generator));
            EnsureSymbol(nameof(TypeContext.imports));
            EnsureSymbol(nameof(TypeContext.type));
        }
        
        // ------------------------------------ utilities ------------------------------------ 
        public static List<string> JsonTypesFromTypes(ICollection<Type> types, string @namespace) {
            var list = new List<string>();
            foreach (var type in types) {
                list.Add($"./{@namespace}{type.Name}.json#/definitions/{type.Name}");
            }
            return list;
        }
        
        public static string JsonTypeFromType(Type type, string @namespace) {
            return $"./{@namespace}{type.Name}.json#/definitions/{type.Name}";
        }
        
        public static TypeDef TypeDefFromType(Type type, JsonTypeSchema jsonSchema, string @namespace) {
            var path = JsonTypeFromType(type, @namespace);
            return jsonSchema.TypeAsTypeDef(path);
        }
        
        public static void AssertFoldersAreEqual(string expectFolder, string otherFolder) {
            var expectFiles = Directory.GetFiles(expectFolder, "*.json", SearchOption.TopDirectoryOnly);
            var otherFiles  = Directory.GetFiles(otherFolder,  "*.json", SearchOption.TopDirectoryOnly);
            
            var expectNames = expectFiles.Select(name => name.Substring(expectFolder.Length));
            var otherNames  = otherFiles. Select(name => name.Substring(otherFolder.Length));

            Assert.AreEqual (expectNames, otherNames);
            foreach (var expectName in expectNames) {
                var expectContent = File.ReadAllText(expectFolder + expectName, Encoding.UTF8);
                var otherContent  = File.ReadAllText(otherFolder  + expectName, Encoding.UTF8);
                Assert.AreEqual (expectContent, otherContent);
            }
        }
    }
}