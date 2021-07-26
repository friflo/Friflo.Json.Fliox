// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
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
            EnsureSymbol(nameof(Generator.emitFiles));
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
    }
}