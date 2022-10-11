// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public class TestDiffer : LeakTestsFixture
    {
        /* class DiffChild
        {
            public int          childIntVal;
        } */

        class DiffBase
        {
            public int          intVal1 { get; set; }
            public int          intVal2 { get; set; }
            public int          intVal3 { get; set; }
            public int          intVal4 { get; set; }
            public int          intVal5 { get; set; }
            public int          intVal6 { get; set; }
            public int          intVal7 { get; set; }
            public int          intVal8 { get; set; }
        }
        
        [Test]
        public void TestDiff() {
            using (var typeStore        = new TypeStore()) 
            using (var mapper           = new ObjectMapper(typeStore))
            using (var differ           = new ObjectDiffer())
            {
                var writer = mapper.writer;
                mapper.Pretty = true;

                var left  = new DiffBase {
                    intVal1 = 1, intVal2 = 1, intVal3 = 1, intVal4 = 1,
                    intVal5 = 1, intVal6 = 1, intVal7 = 1, intVal8 = 1,
                };
                var right = new DiffBase ();
               
                var diff = differ.GetDiff(left, right, writer);
                
                AreEqual(8, diff.Children.Count);
                var childrenDiff = diff.AsString(20);
                var expect =
@"/intVal1            1 != 0
/intVal2            1 != 0
/intVal3            1 != 0
/intVal4            1 != 0
/intVal5            1 != 0
/intVal6            1 != 0
/intVal7            1 != 0
/intVal8            1 != 0
"; 
                AreEqual(expect, childrenDiff);
                
                var start = GC.GetAllocatedBytesForCurrentThread();
                for (int n = 0; n < 10; n++) {
                    differ.GetDiff(left, right, writer);
                }
                var diffAlloc =  GC.GetAllocatedBytesForCurrentThread() - start;
                // AreEqual(0, diffAlloc);
                Console.WriteLine($"Diff allocations: {diffAlloc}");
            }
        }
        
        [Test]
        public void TestMapperPerf() {
            using (var typeStore        = new TypeStore(new StoreConfig())) 
            using (var mapper           = new ObjectMapper(typeStore)) {
                var value  = new DiffBase {
                    intVal1 = 100, intVal2 = 101, intVal3 = 102, intVal4 = 103,
                    intVal5 = 104, intVal6 = 105, intVal7 = 106, intVal8 = 107,
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