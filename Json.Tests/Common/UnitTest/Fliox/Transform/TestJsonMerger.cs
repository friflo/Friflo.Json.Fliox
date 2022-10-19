// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Transform.Tree;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable NotAccessedField.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public class TestJsonMerger
    {
        private class MergeChild {
            public  int     childInt;
        }
        
        private  class MergeClass {
            public  int         int1;
            public  MergeChild  child1;
            public  MergeChild  child2;
            public  string      str1;
            public  int         int2;
            public  int         int3;
        }
        
        [Test]
        public void TestJsonMerge() {
            using (var typeStore        = new TypeStore()) 
            using (var differ           = new ObjectDiffer(typeStore))
            using (var jsonDiff         = new JsonDiff(typeStore))
            using (var writer           = new ObjectWriter(typeStore))
            using (var merger           = new JsonMerger())
            {
                var left    = new MergeClass {
                    int1    =  1,
                    int2    =  2,
                    child1  = new MergeChild { childInt =  3 },
                    child2  = null,
                    str1    = "Test",
                    int3    =  5
                };
                var right   = new MergeClass {
                    int1    = 11,
                    int2    = 12,
                    child1  = new MergeChild { childInt = 13 },
                    child2  = new MergeChild { childInt = 14 },
                    str1    = null,  
                    int3    =  5
                };

                PrepareMerge(left, right, differ, jsonDiff, writer, out var value, out var patch);
                var merge       = merger.Merge(value, patch);
                var expect      = "{'int1':11,'child1':{'childInt':13},'int2':12,'int3':5,'child2':{'childInt':14}}".Replace('\'', '"');
                AreEqual(expect, merge.AsString());
                
                AssertAlloc(value, patch, 1, merger);
            }
        }
        
        private static void PrepareMerge<T>(
                T               left,
                T               right,
                ObjectDiffer    differ,
                JsonDiff        jsonDiff,
                ObjectWriter    writer,
            out JsonValue       value,  // left as JSON
            out JsonValue       patch)  // the merge patch - when merging to left the result is right
        {
            var diff    = differ.GetDiff(left, right, DiffKind.DiffArrays);
            patch       = jsonDiff.CreateJsonDiff(diff);

            writer.Pretty           = true;
            writer.WriteNullMembers = false;
            value        = writer.WriteAsValue(left);
        }
        
        private static void AssertAlloc(JsonValue value, JsonValue patch, int count, JsonMerger merger) {
            merger.MergeBytes(value, patch);

            var start   = GC.GetAllocatedBytesForCurrentThread();
            for (int n = 0; n < count; n++) {
                merger.MergeBytes(value, patch);
            }
            var dif     = GC.GetAllocatedBytesForCurrentThread() - start;
            AreEqual(0, dif);
        }
    }
}