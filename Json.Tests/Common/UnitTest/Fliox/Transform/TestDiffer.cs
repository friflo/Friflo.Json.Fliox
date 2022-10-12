// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public class TestDiffer : LeakTestsFixture
    {
        class DiffChild
        {
            public int          intVal1 { get; set; }
            public int          intVal2 { get; set; }
        }

        class DiffBase
        {
            public DiffChild    child1   { get; set; }
            public DiffChild    child2   { get; set; }
            public DiffChild    child3   { get; set; }
            public DiffChild    child4   { get; set; }
            public DiffChild    child5   { get; set; }
            public DiffChild    child6   { get; set; }
        }
        
        [Test]
        public void TestDiff() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore))
            using (var differ           = new ObjectDiffer(typeStore))
            using (var jsonDiff         = new JsonDiff(typeStore))
            {
                mapper.Pretty = true;

                var left  = new DiffBase {
                    child1 = new DiffChild(),
                    child2 = new DiffChild(),
                    child3 = new DiffChild(),   // equal object
                    child4 = null,              // equal null
                    child5 = new DiffChild(),   // only left
                    child6 = null,              // only right
                };
                var right = new DiffBase {
                    child1 = new DiffChild { intVal1 = 1 },
                    child2 = new DiffChild { intVal2 = 2 },
                    child3 = new DiffChild(),
                    child4 = null,
                    child5 = null,
                    child6 = new DiffChild()
                };
               
                var diff    = differ.GetDiff(left, right);
                
                AreEqual(4, diff.Children.Count);
                var childrenDiff = diff.AsString(20);
                var expectedDiff =
@"/child1             {DiffChild} != {DiffChild}
/child1/intVal1     0 != 1
/child2             {DiffChild} != {DiffChild}
/child2/intVal2     0 != 2
/child5             {DiffChild} != null
/child6             null != {DiffChild}
"; 
                AreEqual(expectedDiff, childrenDiff);
                
                var json    = jsonDiff.CreateJsonDiff(diff);
                var expectedJson =
                "{'child1':{'intVal1':1},'child2':{'intVal2':2},'child5':null,'child6':{'intVal1':0,'intVal2':0}}".Replace('\'', '\"'); 
                AreEqual(expectedJson, json.AsString());
                
                var start = GC.GetAllocatedBytesForCurrentThread();
                for (int n = 0; n < 10; n++) {
                    differ.GetDiff(left, right);
                }
                var diffAlloc =  GC.GetAllocatedBytesForCurrentThread() - start;
                AreEqual(0, diffAlloc);
                Console.WriteLine($"Diff allocations: {diffAlloc}");
            }
        }
        
        [Test]
        public void TestMapperPerf() {
            using (var typeStore        = new TypeStore(new StoreConfig())) 
            using (var mapper           = new ObjectMapper(typeStore)) {
                var value  = new DiffBase {
                    child1 = new DiffChild {
                        intVal1 = 111,
                        intVal2 = 222,
                    },
                    child2 = new DiffChild {
                        intVal1 = 333,
                        intVal2 = 444,
                    }
                };
                var array = mapper.WriteAsArray(value);
                var json = new JsonValue(array);
                
                var result  = new DiffBase();
                
                mapper.ReadTo(json, result);
                var start = GC.GetAllocatedBytesForCurrentThread();
                for (int n = 0; n < 10; n++) {
                    mapper.ReadTo<DiffBase>(json, result);
                }
                var diffAlloc =  GC.GetAllocatedBytesForCurrentThread() - start;
                // AreEqual(0, diffAlloc);
                Console.WriteLine($"Diff allocations: {diffAlloc}");
            }
        }
    }
}