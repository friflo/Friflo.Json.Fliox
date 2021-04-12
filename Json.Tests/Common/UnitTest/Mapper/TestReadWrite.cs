using System;
using System.Collections.Generic;
using System.Numerics;
using Friflo.Json.Burst;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

using static NUnit.Framework.Assert;
// using static Friflo.Json.Tests.Common.Utils.SimpleAssert;


namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public struct TestStruct {
        public int key;
    }

    public class TestChild {
    }
    
    public class TestMapperClass {
        public TestMapperClass    selfReference; // test cyclic references
        public TestChild    testChild;
        public int          key;
        public string       str;
        public BigInteger   bigInt;
        public int[]        intArray;
        //
        public double       dblFld;
        public float        fltFld;
        public long         lngFld;
        public int          intFld;
        public short        srtFld;
        public byte         bytFld;
        public bool         blnFld;
        //
        // ReSharper disable InconsistentNaming
        public double       dblPrp { get; set; }
        public float        fltPrp { get; set; }
        public long         lngPrp { get; set; }
        public int          intPrp { get; set; }
        public short        srtPrp { get; set; }
        public byte         bytPrp { get; set; }
        public bool         blnPrp { get; set; }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TestMapperClass) obj);
        }

        // ReSharper disable method CompareOfFloatsByEqualityOperator
        private bool Equals(TestMapperClass o) {
            return key == o.key &&
                   dblPrp == o.dblPrp && fltPrp == o.fltPrp && lngPrp == o.lngPrp && intPrp == o.intPrp &&
                   srtPrp == o.srtPrp && bytPrp == o.bytPrp && blnPrp == o.blnPrp &&
                   dblFld == o.dblFld && fltFld == o.fltFld && lngFld == o.lngFld && intFld == o.intFld &&
                   srtFld == o.srtFld && bytFld == o.bytFld && blnFld == o.blnFld &&
                   bigInt.Equals(o.bigInt);
        }

        // ReSharper disable NonReadonlyMemberInGetHashCode
        public override int GetHashCode() {
            return key + bigInt.GetHashCode();
        }
    }

    public class TestReadWrite : LeakTestsFixture
    {
        const string BigInt = "1234567890123456789012345678901234567890";

        [Test]
        public void Run() {
            string testClassJson = $@"
{{
    ""intArray"":null,
    ""testChild"":null,
    ""key"":42,
    ""bigInt"":""{BigInt}"",

    ""dblPrp"": 100,    ""fltPrp"": 101,    ""lngPrp"": 102,    ""intPrp"": 103,
    ""srtPrp"": 104,    ""bytPrp"": 105,    ""blnPrp"": true,

    ""dblFld"": 200,    ""fltFld"": 201,    ""lngFld"": 202,    ""intFld"": 203,
    ""srtFld"": 204,    ""bytFld"": 205,    ""blnFld"": true,

    ""unknownObject"": {{
        ""anotherUnknown"": 42
    }},
    ""unknownArray"": [42],
    ""unknownStr"": ""str"", 
    ""unknownNumber"":999,
    ""unknownBool"":true,
    ""unknownNull"":null
}}";
            var hello =      "\"hello\"";
            var @double =    "12.5";
            var @long =      "42";
            var @true =      "true";
            var @null =      "null";
            var bigInt =     BigInt;
            var dblOverflow= "1.7976931348623157E+999";
            var bigIntStr =  $"\"{BigInt}\"";
            var bigIntStrN = $"\"{BigInt}n\"";
            var dateTime =   "2021-01-14T09:59:40.101Z";
            var dateTimeStr= "\"2021-01-14T09:59:40.101Z\"";
             // --- arrays
            var arrNum =     "[1,2,3]";
            var arrBigInt=   "[\"1\",\"2\",\"1234567890123456789012345678901234567890\"]";
            var arrStr =     "[\"A\",\"B\",\"C\"]";
            var arrBln =     "[true, false]";
            var arrObj =     "[{\"key\":42}]";
            var arrNull =    "[null]";
            var arrArrNum =  "[[1,2,3]]";
            var arrArrObj =  "[[{\"key\":42}]]";
             // --- class/map
            var testClass =  testClassJson;
            var mapNull =    "{\"key\":null}";
            var mapNum =     "{\"key\":42}";
            var mapBool =    "{\"key\":true}";
            var mapStr =     "{\"key\":\"value\"}";
            var mapMapNum =  "{\"key\":{\"key\":42}}";
            var mapNum2 =    "{\"str\":44}";
            var invalid =    "invalid";
                
            using (TypeStore typeStore = new TypeStore())
            using (JsonReader enc = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter write = new JsonWriter(typeStore))
            //
            using (JsonReader read2 = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter write2 = new JsonWriter(typeStore))
            {
                reader = enc;
                cmpRead = read2; 
                cmpWrite = write2; 
                
                int iterations = 2; // dont use < 2
                for (int n = 0; n < iterations; n++) {
                    AreEqual("hello",   Read<string>(hello));
                    AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    
                    AreEqual(12.5,                      Read<double>        (@double));
                    AreEqual(12.5,                      Read<double?>       (@double));
                    AreEqual(null,                      Read<double?>       (@null));

                    enc.Read<double>(@null);
                    StringAssert.Contains("JsonReader/error: Cannot assign null to double. Expect: System.Double, got: null path: '(root)'", enc.Error.msg.ToString());

                    enc.Read<double>(@null);
                    StringAssert.Contains("Cannot assign null to double. Expect: System.Double, got: null path: '(root)'", enc.Error.msg.ToString());

                    // error cases
                    enc.Read<double>(@true);
                    StringAssert.Contains("Cannot assign bool to double. Expect: System.Double, got: true path: '(root)'", enc.Error.msg.ToString());
                    enc.Read<double>(hello);
                    StringAssert.Contains("Cannot assign string to double. Expect: System.Double, got: 'hello' path: '(root)'", enc.Error.msg.ToString());
                    enc.Read<double>(arrNum);
                    StringAssert.Contains("Cannot assign array to double. Expect: System.Double, got: [...] path: '[]'", enc.Error.msg.ToString());
                    enc.Read<double>(mapNum);
                    StringAssert.Contains("Cannot assign object to double. Expect: System.Double, got: {...} path: '(root)'", enc.Error.msg.ToString());
                    enc.Read<long>(bigInt);
                    StringAssert.Contains("Value out of range when parsing long:", enc.Error.msg.ToString());
                    enc.Read<float>(bigInt);
                    AreEqual(true, enc.Error.ErrSet);
                    // StringAssert.Contains("float value out of range. val:", enc.Error.msg.ToString()); // Unity has different error message
                    enc.Read<double>   (dblOverflow);
                    AreEqual(true, enc.Error.ErrSet);
                    // StringAssert.Contains("double value out of range. val:", enc.Error.msg.ToString());// Unity has different error message


                    AreEqual(12.5,                      Read<float>    (@double));
                    AreEqual(12.5,                      Read<float?>   (@double));
                    AreEqual(null,                      Read<float?>   (@null));
                    enc.Read<float>(@null);
                    StringAssert.Contains("Cannot assign null to float. Expect: System.Single, got: null path: '(root)'", enc.Error.msg.ToString());

                    AreEqual(42,        Read<long>     (@long));
                    AreEqual(42,        Read<long>     (@long));
                    AreEqual(42,        Read<long?>    (@long));
                    AreEqual(null,      Read<long?>    (@null));
                    enc.Read<long>(@null);
                    StringAssert.Contains("Cannot assign null to long. Expect: System.Int64, got: null path: '(root)'", enc.Error.msg.ToString());

                    AreEqual(42,        Read<int>      (@long));
                    AreEqual(42,        Read<int>      (@long));
                    AreEqual(42,        Read<int?>     (@long));
                    AreEqual(null,      Read<int?>     (@null));
                    enc.Read<int>(@null);
                    StringAssert.Contains("Cannot assign null to int. Expect: System.Int32, got: null path: '(root)'", enc.Error.msg.ToString());

                    AreEqual(42,        Read<short>    (@long));
                    AreEqual(42,        Read<short>    (@long));
                    AreEqual(42,        Read<short?>   (@long));
                    AreEqual(null,      Read<short?>   (@null));
                    enc.Read<short>(@null);
                    StringAssert.Contains("Cannot assign null to short. Expect: System.Int16, got: null path: '(root)'", enc.Error.msg.ToString());

                    AreEqual(42,        Read<byte>     (@long));
                    AreEqual(42,        Read<byte>     (@long));
                    AreEqual(42,        Read<byte?>    (@long));
                    AreEqual(null,      Read<byte?>    (@null));
                    enc.Read<byte>(@null);
                    StringAssert.Contains("Cannot assign null to byte. Expect: System.Byte, got: null path: '(root)'", enc.Error.msg.ToString());

                    AreEqual(true,      Read<bool>     (@true));
                    AreEqual(true,      Read<bool>     (@true));
                    AreEqual(true,      Read<bool?>    (@true));
                    AreEqual(null,      Read<bool?>    (@null));
                    enc.Read<bool>(@null);
                    StringAssert.Contains("Cannot assign null to bool. Expect: System.Boolean, got: null path: '(root)'", enc.Error.msg.ToString());

                    
                    AreEqual(null,      Read<object>(@null));
                    AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    
                    AreEqual(null,      enc.Read<object>(invalid));
                    AreEqual("JsonParser/JSON error: unexpected character while reading value. Found: i path: '(root)' at position: 1",     enc.Error.msg.ToString());
                    
                    // ------------------------------------- class -------------------------------------
                    {
                        BigInteger bigIntVal = BigInteger.Parse(bigInt);
                        var expect = new TestMapperClass {
                            key = 42, bigInt = bigIntVal,
                            dblPrp = 100, fltPrp = 101, lngPrp = 102, intPrp = 103, srtPrp = 104, bytPrp = 105, blnPrp = true,
                            dblFld = 200, fltFld = 201, lngFld = 202, intFld = 203, srtFld = 204, bytFld = 205, blnFld = true
                        };
                        var value = Read<TestMapperClass>(testClass);
                        if (JsonEvent.EOF != enc.JsonEvent)
                            Fail(enc.Error.msg.ToString());
                        AreEqual(new SkipInfo { arrays=1, booleans= 1, floats= 0, integers= 3, nulls= 1, objects= 1, strings= 1 }, enc.SkipInfo);
                        AreEqual(expect, value);
                    }

                    TestMapperClass root = new TestMapperClass();
                    TestMapperClass curTestClass = root;
                    for (int i = 0; i < 30; i++) {
                        curTestClass.selfReference = new TestMapperClass();
                        curTestClass = curTestClass.selfReference;
                    }
                    var jsonResult = write.Write(root);
                    {
                        var result = Read<TestMapperClass>(jsonResult);
                        if (JsonEvent.Error == enc.JsonEvent)
                            Fail(enc.Error.msg.ToString());
                        AreEqual(root, result);
                    }
                    enc.Read<TestMapperClass>(mapNull);
                    StringAssert.Contains("Cannot assign null to int. Expect: System.Int32, got: null path: 'key'", enc.Error.msg.ToString());
                    
                    enc.Read<TestMapperClass>(mapStr);
                    StringAssert.Contains("Cannot assign string to int. Expect: System.Int32, got: 'value' path: 'key'", enc.Error.msg.ToString());
                    
                    enc.Read<TestMapperClass>(mapNum2);
                    StringAssert.Contains("Cannot assign number to string. Expect: System.String, got: 44 path: 'str'", enc.Error.msg.ToString());

                    enc.Read<TestMapperClass>(mapBool);
                    StringAssert.Contains("Cannot assign bool to int. Expect: System.Int32, got: true path: 'key'", enc.Error.msg.ToString());
                    

                    // ------------------------------------- Array -------------------------------------
                    AreEqual(null,                Read<string[]>    (@null));           AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(null,                Read<double[]>    (@null));           AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(null,                Read<float[]>     (@null));           AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(null,                Read<long[]>      (@null));           AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(null,                Read<int[]>       (@null));           AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(null,                Read<short[]>     (@null));           AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(null,                Read<byte[]>      (@null));           AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(null,                Read<bool[]>      (@null));           AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    
                    enc.Read<TestStruct[]>(arrNull);
                    StringAssert.Contains("Cannot assign null to class. Expect: Friflo.Json.Tests.Common.UnitTest.Mapper.TestStruct, got: null", enc.Error.msg.ToString());
                    enc.Read<int[]>(arrNull);
                    StringAssert.Contains("Cannot assign null to int. Expect: System.Int32, got: null path: '[0]'", enc.Error.msg.ToString());
                    enc.Read<int[]>(arrStr);
                    StringAssert.Contains("Cannot assign string to int. Expect: System.Int32, got: 'A' path: '[0]'", enc.Error.msg.ToString());

                    AreEqual(new [] {"A","B","C"},    Read<string[]>    (arrStr));          AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    {
                        var reused = new string[1];
                        AreEqual(new[] {"A","B","C"}, ReadTo(arrStr, reused));
                        AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    }
                    // --- array non nullable
                    AreEqual(new [] {1,2,3},      Read   <long[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(new [] {1,2,3},      Read    <int[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(new [] {1,2,3},      Read  <short[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(new [] {1,2,3},      Read   <byte[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.JsonEvent);
                            
                    AreEqual(new [] {1,2,3},      Read <double[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(new [] {1,2,3},      Read  <float[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.JsonEvent);
                            
                    AreEqual(new [] {true, false},Read   <bool[]>    (arrBln));          AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    // --- array nullable
                    AreEqual(new [] {1,2,3},      Read  <long?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(new [] {1,2,3},      Read   <int?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(new [] {1,2,3},      Read <short?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(new [] {1,2,3},      Read  <byte?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.JsonEvent);
                            
                    AreEqual(new [] {1,2,3},      Read<double?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(new [] {1,2,3},      Read <float?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.JsonEvent);
                            
                    AreEqual(new [] {true, false},Read <bool?[]>     (arrBln));          AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    // array nullable - with null
                    AreEqual(new   long?[] {null},  Read   <long?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(new    int?[] {null},  Read    <int?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(new  short?[] {null},  Read  <short?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(new   byte?[] {null},  Read   <byte?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.JsonEvent);

                    AreEqual(new double?[] {null},  Read <double?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    AreEqual(new  float?[] {null},  Read   <byte?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.JsonEvent);

                    AreEqual(new   bool?[] {null},  Read   <bool?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.JsonEvent);

                    {
                        Dictionary<string, int>[] expect = {new Dictionary<string, int> {{"key", 42}}};
                        AreEqual(expect, Read<Dictionary<string, int>[]>(arrObj));
                        AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    }

                    // --- array of array
                    {
                        int[][] expect = {new []{1, 2, 3}};
                        AreEqual(expect, Read<int[][]>(arrArrNum));
                        AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    }
                    {
                        Dictionary<string, int>[][] expect = {new []{ new Dictionary<string, int> {{"key", 42}}}};
                        AreEqual(expect, Read<Dictionary<string, int>[][]>(arrArrObj));
                        AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    }
                    // --- multi dimensional arrays
                    {
                        // int[,] expect = {{1, 2, 3}};
                        var e = Assert.Throws<NotSupportedException>(() => enc.Read<int[,]>(arrArrNum));
                        AreEqual("Type not supported. Found no TypeMapper in TypeStore Type: System.Int32[,]", e.Message);              
                    }
                    
                    // ------------------------------------- List<T> -------------------------------------
                    AreEqual(null, Read<List<int>>    (@null));           AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    {
                        List<int> expect = new List<int> {1, 2, 3};
                        AreEqual(expect, Read<List<int>> (arrNum));
                        AreEqual(expect, Read<List<int?>>(arrNum));
                        AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    } {
                        List<bool> expect = new List<bool> {true, false};
                        AreEqual(expect, Read<List<bool>>(arrBln));
                        AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    } {
                        var expect = new List<int?> { null };
                        AreEqual(expect, Read<List<int?>>(arrNull));
                        AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    }
                    // --- list of list
                    {
                        List<List<int>> expect = new List<List<int>> {new List<int> {1, 2, 3}};
                        AreEqual(expect, Read<List<List<int>>>(arrArrNum));
                    } {
                        List<int> expect = new List<int> {1, 2, 3};
                        List<int> reused = new List<int> (expect);
                        AreEqual(expect, ReadTo (arrNum, reused));
                        AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    } {
                        List<bool> expect = new List<bool> {true, false};
                        List<bool> reused = new List<bool> (expect);
                        AreEqual(expect, ReadTo (arrBln, reused));
                        AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    } {
                        List<string> expect = new List<string> {"A", "B", "C"};
                        List<string> reused = new List<string> {"1", "2", "3", "4", "5"};
                        AreEqual(expect, ReadTo (arrStr, reused));
                        AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    } {
                        var big = BigInteger.Parse("1234567890123456789012345678901234567890");
                        List<BigInteger> expect = new List<BigInteger> {new BigInteger(1), new BigInteger(2), big};
                        List<BigInteger> reused = new List<BigInteger> {new BigInteger(10), new BigInteger(11), new BigInteger(12), new BigInteger(13)};
                        AreEqual(expect, ReadTo (arrBigInt, reused));
                        AreEqual(JsonEvent.EOF, enc.JsonEvent);
                    }
                    
                    enc.Read<List<int>>(arrNull);
                    StringAssert.Contains("Cannot assign null to int. Expect: System.Int32, got: null path: '[0]'", enc.Error.msg.ToString());
                    
                    enc.Read<List<int>>(arrStr);
                    StringAssert.Contains("Cannot assign string to int. Expect: System.Int32, got: 'A' path: '[0]'", enc.Error.msg.ToString());
                    
                    enc.Read<List<string>>(arrNum);
                    StringAssert.Contains("Cannot assign number to string. Expect: System.String, got: 1 path: '[0]'", enc.Error.msg.ToString());

                    enc.Read<List<string>>(arrBln);
                    StringAssert.Contains("Cannot assign bool to string. Expect: System.String, got: true path: '[0]'", enc.Error.msg.ToString());
                    
                    enc.Read<List<string>>(mapStr);
                    StringAssert.Contains("Cannot assign object to List. Expect:", enc.Error.msg.ToString());

                    // --------------------------------- Dictionary<K,V> ---------------------------------
                    enc.Read<Dictionary<string, long>>(arrNull);
                    StringAssert.Contains("Cannot assign array to Dictionary. Expect: System.Collections.Generic.Dictionary`2[System.String,System.Int64], got: [...]", enc.Error.msg.ToString());
                        
                    enc.Read<Dictionary<string, long>>(arrStr);
                    StringAssert.Contains("Cannot assign array to Dictionary. Expect: System.Collections.Generic.Dictionary`2[System.String,System.Int64], got: [...]", enc.Error.msg.ToString());
                        
                    enc.Read<Dictionary<string, string>>(arrNum);
                    StringAssert.Contains("Cannot assign array to Dictionary. Expect: System.Collections.Generic.Dictionary`2[System.String,System.String], got: [...]", enc.Error.msg.ToString());
                        
                    enc.Read<Dictionary<string, string>>(arrBln);
                    StringAssert.Contains("Cannot assign array to Dictionary. Expect:", enc.Error.msg.ToString());
                    {
                        var e = Assert.Throws<NotSupportedException>(() => enc.Read<Dictionary<int, string>>(mapStr));
                        AreEqual("Type not supported. Dictionary only support string as key type Type: System.Collections.Generic.Dictionary`2[System.Int32,System.String]", e.Message);              
                    }
                    
                    // --- maps - value type: integral 
                    {
                        var expect = new Dictionary<string, long> {{"key", 42}};
                        AreEqual(expect, Read<Dictionary<string, long>>(mapNum));
                    } {
                        var expect = new Dictionary<string, int> {{"key", 42}};
                        AreEqual(expect, Read<Dictionary<string, int>>(mapNum));
                    } {
                        var expect = new Dictionary<string, short> {{"key", 42}};
                        AreEqual(expect, Read<Dictionary<string, short>>(mapNum));
                    } {
                        var expect = new Dictionary<string, byte> {{"key", 42}};
                        AreEqual(expect, Read<Dictionary<string, byte>>(mapNum));
                    }
                    // --- maps - value type: floating point
                    {
                        var expect = new Dictionary<string, double> {{"key", 42}};
                        AreEqual(expect, Read<Dictionary<string, double>>(mapNum));
                    } {
                        var expect = new Dictionary<string, float> {{"key", 42}};
                        AreEqual(expect, Read<Dictionary<string, float>>(mapNum));
                    } {
                        var expect = new Dictionary<string, float?> {{"key", 42}};
                        AreEqual(expect, Read<Dictionary<string, float?>>(mapNum));
                    } {
                        var expect = new Dictionary<string, float?> {{"key", null}};
                        AreEqual(expect, Read<Dictionary<string, float?>>(mapNull));
                    }
                    
                    // --- map - value type: string
                    {
                        var expect = new Dictionary<string, string> {{"key", "value" }};
                        AreEqual(expect, Read<Dictionary<string, string>>(mapStr));
                    } {
                        var expect = new Dictionary<string, string> {{"key", null }};
                        AreEqual(expect, Read<Dictionary<string, string>>(mapNull));
                    }
                    // --- map - value type: bool
                    {
                        var expect = new Dictionary<string, bool> {{"key", true }};
                        AreEqual(expect, Read<Dictionary<string, bool>>(mapBool));
                    }
                    // --- map - value type: map
                    {
                        var expect = new Dictionary<string, Dictionary<string, int>>() {{"key", new Dictionary<string, int> {{"key", 42 }} }};
                        AreEqual(expect, Read<Dictionary<string, Dictionary<string, int>>>(mapMapNum));
                    }
                    
                    // ---- BigInteger ---
                    AreEqual(new TestStruct{ key = 42 },        Read<TestStruct>    (mapNum));
                    AreEqual(default(TestStruct),               enc.Read<TestStruct>(@null));
                    StringAssert.Contains("Cannot assign null to class. Expect: Friflo.Json.Tests.Common.UnitTest.Mapper.TestStruct, got: null path: '(root)'", enc.Error.msg.ToString());
                    {
                        BigInteger expect = BigInteger.Parse(bigInt.ToString());
                        AreEqual(expect, Read<BigInteger>(bigIntStr));
                        AreEqual(JsonEvent.EOF, enc.JsonEvent);
                        
                        AreEqual(expect, Read<BigInteger>(bigIntStrN));
                        AreEqual(JsonEvent.EOF, enc.JsonEvent);
                        
                        AreEqual(expect, Read<BigInteger>(bigInt));
                        AreEqual(JsonEvent.EOF, enc.JsonEvent);
                        
                        var result = write.Write(expect);
                        AreEqual(bigIntStr.ToString(), result);
                    }
                    enc.Read<BigInteger>(hello);
                    StringAssert.Contains("Failed parsing BigInt. value:", enc.Error.msg.ToString());
                    
                    // --- DateTime ---
                    {
                        DateTime expect = DateTime.Parse(dateTime.ToString());
                        DateTime value = Read<DateTime>(dateTimeStr);
                        AreEqual(expect, value);
                        
                        var result = write.Write(expect);
                        AreEqual(dateTimeStr.ToString(), result);   
                    }
                    enc.Read<DateTime>(hello);
                    StringAssert.Contains("Failed parsing DateTime. value:", enc.Error.msg.ToString());

                    // Ensure minimum required type lookups
                    if (n > 0) {
                        AreEqual(128, enc.TypeCache.LookupCount);
                        AreEqual( 0,  enc.TypeCache.StoreLookupCount);
                        AreEqual( 0,  enc.TypeCache.TypeCreationCount);
                    }
                    enc.TypeCache.ClearCounts();
                }
            }
        }

        private JsonReader reader;
        
        private JsonReader cmpRead;
        private JsonWriter cmpWrite;

        private T Read<T>(string json) {
            // return reader.Read<T>(bytes);

            var result = reader.Read<T>(json);
            if (!reader.Success)
                return result;

            T writeResult;
            using (var dst = new TestBytes()) {
                cmpWrite.Write(result, ref dst.bytes);
                writeResult = cmpRead.Read<T>(dst.bytes);
            }
            
            AreEqual(result, writeResult);
            return result;
        }
        
        private T ReadTo<T>(string json, T value) where T : class {
            // return reader.ReadTo<T>(bytes, value);

            T result = reader.ReadTo(json, value);
            if (!reader.Success)
                return default;

            T writeResult;
            using (var dst = new TestBytes()) {
                cmpWrite.Write(value, ref dst.bytes);
                writeResult = cmpRead.Read<T>(dst.bytes);
            }
            AreEqual(value, writeResult);
            return result;
        }


    }
}