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
        class ToolsChild
        {
            public  int         inVal { get; set; }
        }

        class ToolsClass
        {
            public  ToolsChild   child   { get; set; }
        }
        
        [Test]
        public void TestToolsCopyClass() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore)) {
                var  tools = new ObjectTools(typeStore);
                {
                    var from    = new ToolsClass { child = new ToolsChild() };
                    var target  = new ToolsClass { child = null };
                    DeepCopy(from, ref target, mapper, tools);
                } {
                    var from    = new ToolsClass { child = null };
                    var target  = new ToolsClass { child = new ToolsChild() };
                    DeepCopy(from, ref target, mapper, tools);
                } {
                    var from    = new ToolsClass { child = null };
                    var target  = new ToolsClass { child = null };
                    DeepCopy(from, ref target, mapper, tools);
                } {
                    var from    = new ToolsClass { child = new ToolsChild { inVal = 1 } };
                    var target  = new ToolsClass { child = new ToolsChild { inVal = 2 } };
                    DeepCopy(from, ref target, mapper, tools);
                } {
                    var target  = new ToolsClass { child = null };
                    DeepCopy (null, ref target, mapper, tools);
                }
            }
        }
        
        [Test]
        public void TestToolsCopyArray() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore)) {
                var  tools = new ObjectTools(typeStore);
                {
                    var from    = new int [] { 1 };
                    var target  = new int [] {};
                    DeepCopy(from, ref target, mapper, tools);
                } {
                    var from    = new [] { 2 };
                    var target  = new [] { 3 };
                    DeepCopy(from, ref target, mapper, tools);
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
                    var from    = new Dictionary<string, string>();
                    var target  = new Dictionary<string, string>();
                    DeepCopy(from, ref target, mapper, tools);
                } {
                    var from    = new Dictionary<string, string> {{ "A", "B" }};
                    var target  = new Dictionary<string, string>();
                    DeepCopy(from, ref target, mapper, tools);
                } {
                    var from    = new Dictionary<string, string>();
                    var target  = new Dictionary<string, string>{{ "C", "D" }};
                    DeepCopy(from, ref target, mapper, tools);
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
                    int target = 0;
                    DeepCopy(1, ref target, mapper, tools);
                }
            }
        }
        
        private static void DeepCopy<T>(T from, ref T target, ObjectMapper mapper, ObjectTools tools)
        {
            tools.DeepCopy(from, ref target);
            
            var leftJson        = mapper.Write(from);
            var toJson          = mapper.Write(target);

            AreEqual(leftJson, toJson);
        }
    }
}