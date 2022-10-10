// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public class TestDiffer : LeakTestsFixture
    {
        class DiffChild
        {
            public int          childIntVal;
        }

        class DiffBase
        {
            public int          intVal { get; set; }
            // public DiffChild    child;
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
                    intVal = 1
                };
                var right = new DiffBase {
                    intVal = 2
                };
                var diff = differ.GetDiff(left, right, writer);
                
                AreEqual(1, diff.Children.Count);
                var childrenDiff = diff.AsString(20);
                var expect =
@"/intVal             1 != 2
"; 
                AreEqual(expect, childrenDiff);
                
                var start = GC.GetAllocatedBytesForCurrentThread();
                for (int n = 0; n < 100; n++) {
                    differ.GetDiff(left, right, writer);
                }
                var diffAlloc =  GC.GetAllocatedBytesForCurrentThread() - start;
                AreEqual(4800, diffAlloc);
                Console.WriteLine($"Diff allocations: {diffAlloc}");
            }
        }
    }
}