// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Diff;
using Friflo.Json.Fliox.Transform.Merge;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public class TestJsonMerge
    {
        class MergeClass {
            public  int     int1;
            public  int     int2;
        }
        
        // [Test]
        public void TestCreateJsonTree() {
            using (var typeStore        = new TypeStore()) 
            using (var differ           = new ObjectDiffer(typeStore))
            using (var jsonDiff         = new JsonDiff(typeStore))
            using (var writer           = new ObjectWriter(typeStore))
            using (var merger           = new JsonMerger())
                
            {
                var left    = new MergeClass { int1 = 1, int2 = 2 };
                var right   = new MergeClass { int1 = 3, int2 = 4 };

                var diff        = differ.GetDiff(left, right, DiffKind.DiffArrays);
                var patch       = jsonDiff.CreateJsonDiff(diff);

                writer.Pretty   = true;
                var jsonArray   = writer.WriteAsArray(left);
                var json        = new JsonValue(jsonArray);
                
                merger.Merge(json, patch);
            }
        }
    }
}