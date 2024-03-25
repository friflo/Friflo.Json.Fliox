// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper.Reference;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    enum Reference {
        Same,
        NotSame
    }
    public class TestObjectTools : LeakTestsFixture
    {
        class ToolsChild
        {
            public  int         inVal { get; set; }
        }
        
        [Discriminator("type")]
        [PolymorphType(typeof(Dog),    "dog")]
        [PolymorphType(typeof(Cat),    "cat")]
        abstract class Animal { }
        
        class Dog : Animal { public string name  {get; set;} }
        class Cat : Animal { public string color {get; set;} }

        class ToolsClass
        {
            public  ToolsChild  child   { get; set; }
            public  Animal      pet     { get; set; }
        }
        
        [Test]
        public void TestToolsCopyClass() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore)) {
                var  tools = new ObjectTools(typeStore);
                {
                    var src = new ToolsClass { child = new ToolsChild(),    pet = new Dog()};
                    var dst = new ToolsClass { child = null };
                    DeepCopy(src , ref dst, Same, mapper, tools);
                    AssertDeepCopyAllocation(src, ref dst, tools);
                } {
                    var src = new ToolsClass { child = null,                pet = new Dog() };
                    var dst = new ToolsClass { child = new ToolsChild(),    pet = new Cat()};
                    DeepCopy(src , ref dst, Same, mapper, tools);
                    AssertDeepCopyAllocation(src, ref dst, tools);
                } {
                    var src = new ToolsClass { child = null };
                    var dst = new ToolsClass { child = null };
                    DeepCopy(src , ref dst, Same, mapper, tools);
                    AssertDeepCopyAllocation(src, ref dst, tools);
                } {
                    var src = new ToolsClass { child = new ToolsChild { inVal = 1 } };
                    var dst = new ToolsClass { child = new ToolsChild { inVal = 2 } };
                    DeepCopy(src , ref dst, Same, mapper, tools);
                    AssertDeepCopyAllocation(src, ref dst, tools);
                } {
                    var dst = new ToolsClass { child = null };
                    DeepCopy (null, ref dst, NotSame, mapper, tools);
                }
                // --- Performance
                {
                    var src = new ToolsClass { child = new ToolsChild { inVal = 1 } };
                    var dst = new ToolsClass { child = new ToolsChild { inVal = 2 } };
                    var start = Mem.GetAllocatedBytes();
                    for (int n = 0; n < 1; n++) {
                        tools.DeepCopy(src, ref dst);
                    }
                    var diff = Mem.GetAllocationDiff(start);
                    Mem.NoAlloc(diff);
                }
            }
        }
        
        [Test]
        public void TestToolsCopyArray() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore)) {
                var  tools = new ObjectTools(typeStore);
                {
                    var src = new int [] { 1 };
                    var dst = new int [] {};
                    DeepCopy(src , ref dst, NotSame, mapper, tools);
                    AssertDeepCopyAllocation(src, ref dst, tools);
                } {
                    var src = new [] { 2 };
                    var dst = new [] { 3 };
                    DeepCopy(src , ref dst, Same, mapper, tools);
                    AssertDeepCopyAllocation(src, ref dst, tools);
                } {
                    var src = new ToolsClass [] { new ToolsClass() };
                    var dst = new ToolsClass [] { };
                    DeepCopy(src , ref dst, NotSame, mapper, tools);
                    AssertDeepCopyAllocation(src, ref dst, tools);
                }
                // --- List<>
                {
                    var src = new List<int> { 2 };
                    var dst = new List<int> ();
                    DeepCopy(src , ref dst, Same, mapper, tools);
                    AssertDeepCopyAllocation(src, ref dst, tools);
                } {
                    var src = new List<int> { 2 };
                    var dst = new List<int> { 10, 11 };
                    DeepCopy(src , ref dst, Same, mapper, tools);
                    AssertDeepCopyAllocation(src, ref dst, tools);
                } {
                    var src = new List<ToolsClass> { new ToolsClass() };
                    var dst = new List<ToolsClass> ();
                    DeepCopy(src , ref dst, Same, mapper, tools);
                    AssertDeepCopyAllocation(src, ref dst, tools);
                } {
                    var src = new List<ToolsClass> { new ToolsClass() };
                    var dst = new List<ToolsClass> { new ToolsClass(), new ToolsClass() };
                    DeepCopy(src , ref dst, Same, mapper, tools);
                    AssertDeepCopyAllocation(src, ref dst, tools);
                }
                // --- ICollection<>
                {
                    ICollection<int> src    = new List<int> { 2 };
                    ICollection<int> dst    = new List<int> ();
                    DeepCopy(src , ref dst, Same, mapper, tools);
                } {
                    ICollection<int> src    = new List<int> { 2 };
                    ICollection<int> dst    = new List<int> { 10, 11 };
                    DeepCopy(src , ref dst, Same, mapper, tools);
                } {
                    ICollection<int> src    = new List<int> { 2 };
                    ICollection<int> dst    = null;
                    DeepCopy(src , ref dst, NotSame, mapper, tools);
                } {
                    ICollection<ToolsClass> src = new List<ToolsClass> { new ToolsClass() };
                    ICollection<ToolsClass> dst = null;
                    DeepCopy(src , ref dst, NotSame, mapper, tools);
                } {
                    ICollection<ToolsClass> src = new List<ToolsClass> { new ToolsClass(), new ToolsClass() };
                    ICollection<ToolsClass> dst = null;
                    DeepCopy(src , ref dst, NotSame, mapper, tools);
                }
                // --- IList<>
                {
                    IList<int> src  = new List<int> { 2 };
                    IList<int> dst  = new List<int> ();
                    DeepCopy(src , ref dst, Same, mapper, tools);
                } {
                    IList<int> src  = new List<int> { 2 };
                    IList<int> dst  = new List<int> { 10, 11 };
                    DeepCopy(src , ref dst, Same, mapper, tools);
                } {
                    IList<int> src  = new List<int> { 2 };
                    IList<int> dst  = null;
                    DeepCopy(src , ref dst, NotSame, mapper, tools);
                } {
                    IList<ToolsClass> src   = new List<ToolsClass> { new ToolsClass() };
                    IList<ToolsClass> dst   = null;
                    DeepCopy(src , ref dst, NotSame, mapper, tools);
                } {
                    IList<ToolsClass> src   = new List<ToolsClass> { new ToolsClass() };
                    IList<ToolsClass> dst   = new List<ToolsClass> { new ToolsClass(), new ToolsClass() };
                    DeepCopy(src , ref dst, Same, mapper, tools);
                }
            }
        }
        
        [Test]
        public void TestToolsCopyDictionary() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore))
            {
                var  tools = new ObjectTools(typeStore);
                {
                    var src = new Dictionary<string, string>();
                    var dst = new Dictionary<string, string>();
                    DeepCopy(src , ref dst, Same, mapper, tools);
                    AssertDeepCopyAllocation(src, ref dst, tools);
                } {
                    var src = new Dictionary<string, string> {{ "A", "B" }};
                    var dst = new Dictionary<string, string>();
                    DeepCopy(src , ref dst, Same, mapper, tools);
                    // addition call required to prevent allocation in subsequent assertion - no clue why
                    DeepCopy(src , ref dst, Same, mapper, tools);
                    AssertDeepCopyAllocation(src, ref dst, tools);
                } {
                    var src = new Dictionary<string, string>();
                    var dst = new Dictionary<string, string>{{ "C", "D" }};
                    DeepCopy(src , ref dst, Same, mapper, tools);
                    AssertDeepCopyAllocation(src, ref dst, tools);
                }
            }
        }
        
        [Test]
        public void TestToolsCopyPrimitives() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore))
            {
                var  tools = new ObjectTools(typeStore);
                {
                    int dst    = 0;
                    DeepCopy(1, ref dst, NotSame, mapper, tools);
                }
            }
        }
        
        private static void DeepCopy<T>(T src , ref T dst, Reference reference, ObjectMapper mapper, ObjectTools tools)
        {
            var passedDst = dst; 
            tools.DeepCopy(src , ref dst);
            
            var srcJson = mapper.Write(src);
            var dstJson = mapper.Write(dst);

            AreEqual(srcJson, dstJson);
            if (reference == Same) {
                AreSame(passedDst, dst);
            } else {
                AreNotEqual(passedDst, dst);
            }
        }
        
        private static void AssertDeepCopyAllocation<T>(T src, ref T dst, ObjectTools tools) {
            var start = Mem.GetAllocatedBytes();
            tools.DeepCopy(src , ref dst);
            var diff = Mem.GetAllocationDiff(start);
            Mem.NoAlloc(diff);
        }
    }
}