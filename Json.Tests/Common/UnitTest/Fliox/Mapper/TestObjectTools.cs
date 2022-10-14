// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    public class TestObjectTools : LeakTestsFixture
    {
        class Child
        {
            public int          intVal1 { get; set; }
            public int          intVal2 { get; set; }
        }

        class BaseClass
        {
            public Child    child1   { get; set; }
            public Child    child2   { get; set; }
            public Child    child3   { get; set; }
            public Child    child4   { get; set; }
            public Child    child5   { get; set; }
            public Child    child6   { get; set; }
        }
        
        [Test]
        public void TestToolsCopyClass() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore))
            {
                var  tools = new ObjectTools(typeStore);
                var from  = new BaseClass {
                    child1 = new Child(),   // not equal
                    child2 = new Child(),   // not equal
                    child3 = new Child(),   // equal object
                    child4 = null,          // equal null
                    child5 = new Child(),   // only left
                    child6 = null,          // only right
                };
                var target = new BaseClass();
                DeepCopy(from, target, mapper, tools);
            }
        }
        
        class BaseArray
        {
            public int[]        array1   { get; set; }
            public int[]        array2   { get; set; }
            public int[]        array3   { get; set; }
            public int[]        array4   { get; set; }
            public int[]        array5   { get; set; }
        }
        
        // [Test]
        public void TestToolsCopyArray() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore))
            {
                var  tools = new ObjectTools(typeStore);
                var from  = new BaseArray {
                    array1 = new [] {1,   2},   // not equal
                    array2 = new [] {11, 12},   // equal
                    array3 = null,              // equal - both null
                    array4 = new [] {22},       // left only
                    array5 = null               // right only
                };
                var target = new BaseArray();
                DeepCopy(from, target, mapper, tools);
            }
        }
        
        class DiffDictionary
        {
            public Dictionary<string, string>   dict1   { get; set; }
            public Dictionary<string, string>   dict2   { get; set; }
            public Dictionary<string, string>   dict3   { get; set; }
            public Dictionary<string, string>   dict4   { get; set; }
            public Dictionary<string, string>   dict5   { get; set; }
        }
        
        [Test]
        public void TestToolsCopyDictionary() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore))
            {
                var  tools = new ObjectTools(typeStore);
                var from  = new DiffDictionary {
                    dict1 = new Dictionary<string, string>{{"key1", "A"}},  // not equal
                    dict2 = new Dictionary<string, string>{{"key2", "C"}},  // equal
                    dict3 = null,                                           // equal - both null
                    dict4 = new Dictionary<string, string>{{"key4", "D"}},  // only left
                    dict5 = null                                            // only right
                };
                var copy = new DiffDictionary();
                DeepCopy(from, copy, mapper, tools);
            }
        }
        
        private static void DeepCopy<T>(T from, T target, ObjectMapper mapper, ObjectTools tools) {
            
            tools.DeepCopy(from, ref target);
            
            var leftJson        = mapper.Write(from);
            var toJson          = mapper.Write(target);

            AreEqual(leftJson, toJson);
        }
    }
}