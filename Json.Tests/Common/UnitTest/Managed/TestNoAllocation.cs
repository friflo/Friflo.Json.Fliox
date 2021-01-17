using Friflo.Json.Burst;
using Friflo.Json.Managed;
using Friflo.Json.Managed.Codecs;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

// using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.Utils.NoCheck;

namespace Friflo.Json.Tests.Common.UnitTest.Managed
{
    public class NoAllocClass {
        public NoAllocClass    selfReference; // test cyclic references
        public NoAllocClass    testChild;
        public int              key;
        public int[]            intArray;

    }

    public class TestNoAllocation
    {
        string testClassJson = $@"
{{
    ""intArray"":null,
    ""testChild"":null,
    ""key"":42,
    ""unknownObject"": {{
        ""anotherUnknown"": 42
    }},
    ""unknownArray"": [42],
    ""unknownStr"": ""str"", 
    ""unknownNumber"":999,
    ""unknownBool"":true,
    ""unknownNull"":null
}}";
        
        [Test]
        public void TestNoAlloc() {
            var memLog = new MemoryLogger(100, 100, MemoryLog.Enabled);
            
            using (TypeStore typeStore = new TypeStore(new DebugTypeResolver()))
            using (JsonReader enc = new JsonReader(typeStore))
            using (JsonWriter write = new JsonWriter(typeStore))
            using (var hello =      new Bytes ("\"hello\""))
            using (var @double =    new Bytes ("12.5"))
            using (var @long =      new Bytes ("42"))
            using (var @true =      new Bytes ("true"))
            using (var @null =      new Bytes ("null"))
            using (var dblOverflow= new Bytes ("1.7976931348623157E+999"))
                // --- arrays
            using (var arrNum =     new Bytes ("[1,2,3]"))
            using (var arrStr =     new Bytes ("[\"hello\"]"))
            using (var arrBln =     new Bytes ("[true, false]"))
            using (var arrObj =     new Bytes ("[{\"key\":42}]"))
            using (var arrNull =    new Bytes ("[null]"))
            using (var arrArrNum =  new Bytes ("[[1,2,3]]"))
            using (var arrArrObj =  new Bytes ("[[{\"key\":42}]]"))
                // --- class/map
            using (var testClass =  new Bytes (testClassJson)) 
            using (var mapNull =    new Bytes ("{\"key\":null}"))
            using (var mapNum =     new Bytes ("{\"key\":42}"))
            using (var mapBool =    new Bytes ("{\"key\":true}"))
            using (var mapStr =     new Bytes ("{\"key\":\"value\"}"))
            using (var mapMapNum =  new Bytes ("{\"key\":{\"key\":42}}"))
            using (var mapNum2 =    new Bytes ("{\"str\":44}"))
            using (var invalid =    new Bytes ("invalid")) {
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    Var result = new Var();
                    
                    //IsTrue(enc.Read<double> (@double, ref result));     AreEqual(12.5d, result.Dbl);
                    //IsTrue(enc.Read<float>  (@double, ref result));     AreEqual(12.5,  result.Flt);
                    //
                    IsTrue(enc.Read<long>       (@long, ref result));     AreEqual(42, result.Lng);
                    IsTrue(enc.Read<int>        (@long, ref result));     AreEqual(42, result.Int);
                    IsTrue(enc.Read<short>      (@long, ref result));     AreEqual(42, result.Short);
                    IsTrue(enc.Read<byte>       (@long, ref result));     AreEqual(42, result.Byte);
                    
                    IsTrue(enc.Read<bool>       (@true, ref result));     AreEqual(true, result.Bool);
                    
                    IsTrue(enc.Read<object>     (@null, ref result));     AreEqual(null, result.Obj);
                    
                    IsTrue(enc.Read<string[]>   (@null, ref result));     AreEqual(null, result.Obj); // no alloc only, if not containing string
                    
                    IsTrue(enc.Read<double[]>   (@null, ref result));     AreEqual(null, result.Obj);
                    IsTrue(enc.Read<float[]>    (@null, ref result));     AreEqual(null, result.Obj);
                    IsTrue(enc.Read<long[]>     (@null, ref result));     AreEqual(null, result.Obj);
                    IsTrue(enc.Read<int[]>      (@null, ref result));     AreEqual(null, result.Obj);
                    IsTrue(enc.Read<short[]>    (@null, ref result));     AreEqual(null, result.Obj);
                    IsTrue(enc.Read<byte[]>     (@null, ref result));     AreEqual(null, result.Obj);
                    IsTrue(enc.Read<bool[]>     (@null, ref result));     AreEqual(null, result.Obj);

                    
                    
   
                    // Ensure minimum required type lookups
                    if (n > 1) {
                        AreEqual(14, enc.typeCache.LookupCount);
                        AreEqual( 0, enc.typeCache.StoreLookupCount);
                        AreEqual( 0, enc.typeCache.TypeCreationCount);
                    }
                    enc.typeCache.ClearCounts();
                }
            }
            memLog.AssertNoAllocations();
        }
    }
    
}