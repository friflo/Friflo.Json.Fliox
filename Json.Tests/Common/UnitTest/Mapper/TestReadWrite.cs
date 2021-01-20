using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Utils;
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
        public double       dblProp { get; set; }
        public float        fltProp { get; set; }
        public long         lngProp { get; set; }
        public int          intProp { get; set; }
        public short        srtProp { get; set; }
        public byte         bytProp { get; set; }
        public bool         blnProp{ get; set; }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TestMapperClass) obj);
        }

        private bool Equals(TestMapperClass other) {
            return key == other.key && bigInt.Equals(other.bigInt);
        }

        // ReSharper disable NonReadonlyMemberInGetHashCode
        public override int GetHashCode() {
            return key + bigInt.GetHashCode();
        }
    }

    public class TestReadWrite
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

    ""dblProp"": 100,
    ""fltProp"": 101,
    ""lngProp"": 102,
    ""intProp"": 103,
    ""srtProp"": 104,
    ""bytProp"": 105,
    ""blnProp"": true,

    ""unknownObject"": {{
        ""anotherUnknown"": 42
    }},
    ""unknownArray"": [42],
    ""unknownStr"": ""str"", 
    ""unknownNumber"":999,
    ""unknownBool"":true,
    ""unknownNull"":null
}}";
            using (TypeStore typeStore = new TypeStore(new DebugTypeResolver()))
            using (JsonReader enc = new JsonReader(typeStore))
            using (JsonWriter write = new JsonWriter(typeStore))
            //
            using (JsonReader read2 = new JsonReader(typeStore))
            using (JsonWriter write2 = new JsonWriter(typeStore))
                
            using (var hello =      new Bytes ("\"hello\""))
            using (var @double =    new Bytes ("12.5"))
            using (var @long =      new Bytes ("42"))
            using (var @true =      new Bytes ("true"))
            using (var @null =      new Bytes ("null"))
            using (var bigInt =     new Bytes (BigInt))
            using (var dblOverflow= new Bytes ("1.7976931348623157E+999"))
            using (var bigIntStr =  new Bytes ($"\"{BigInt}\""))
            using (var bigIntStrN = new Bytes ($"\"{BigInt}n\""))
            using (var dateTime =   new Bytes ("2021-01-14T09:59:40.101Z"))
            using (var dateTimeStr= new Bytes ("\"2021-01-14T09:59:40.101Z\""))
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
                this.reader = enc;
                this.read2 = read2; 
                this.write2 = write2; 
                
                int iterations = 2; // dont use < 2
                for (int n = 0; n < iterations; n++) {
                    AreEqual("hello",   Read<string>(hello));
                    AreEqual(JsonEvent.EOF, enc.parser.Event);
                    
                    AreEqual(12.5,                      Read<double>        (@double));
                    AreEqual(12.5,                      Read<double?>       (@double));
                    AreEqual(null,                      Read<double?>       (@null));

                    enc.Read<double>(@null);
                    StringAssert.Contains("JsonReader/error: Cannot assign null to double. Expect: System.Double, got: null path: '(root)'", enc.Error.msg.ToString());
#if !UNITY_EDITOR
                    {
                        enc.ThrowException = true;
                        var e = Throws<FrifloException>(() => enc.Read<double>(@null));
                        StringAssert.Contains("Cannot assign null to double. Expect: System.Double, got: null path: '(root)'", e.Message);
                        enc.ThrowException = false;
                    }
#endif
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
                    AreEqual(JsonEvent.EOF, enc.parser.Event);
                    
                    AreEqual(null,      enc.Read<object>(invalid));
                    AreEqual("JsonParser/JSON error: unexpected character while reading value. Found: i path: '(root)' at position: 1",     enc.Error.msg.ToString());
                    
                    // ------------------------------------- class -------------------------------------
                    {
                        BigInteger bigIntVal = BigInteger.Parse(bigInt.ToString());
                        var expect = new TestMapperClass {
                            key = 42, bigInt = bigIntVal, dblProp = 100, fltProp = 101,
                            lngProp = 102, intProp = 103, srtProp = 104, bytProp = 105, blnProp = true
                        };
                        var value = Read<TestMapperClass>(testClass);
                        if (JsonEvent.EOF != enc.parser.Event)
                            Fail(enc.Error.msg.ToString());
                        AreEqual(new SkipInfo { arrays=1, booleans= 1, floats= 0, integers= 3, nulls= 1, objects= 1, strings= 1 }, enc.parser.skipInfo);
                        AreEqual(expect, value);
                    }

                    TestMapperClass root = new TestMapperClass();
                    TestMapperClass curTestClass = root;
                    for (int i = 0; i < 30; i++) {
                        curTestClass.selfReference = new TestMapperClass();
                        curTestClass = curTestClass.selfReference;
                    }
                    write.Write(root);
                    Read<TestMapperClass>(write.Output);
                    AreEqual(JsonEvent.EOF, enc.parser.Event);
                    
                    enc.Read<TestMapperClass>(mapNull);
                    StringAssert.Contains("Cannot assign null to class field: key. Expect: System.Int32, got: null path: 'key'", enc.Error.msg.ToString());
                    
                    enc.Read<TestMapperClass>(mapStr);
                    StringAssert.Contains("Cannot assign string to class field: key. Expect: System.Int32, got: 'value' path: 'key'", enc.Error.msg.ToString());
                    
                     enc.Read<TestMapperClass>(mapNum2);
                    StringAssert.Contains("Cannot assign number to class field: str. Expect: System.String, got: 44 path: 'str'", enc.Error.msg.ToString());

                    enc.Read<TestMapperClass>(mapBool);
                    StringAssert.Contains("Cannot assign bool to class field: key. Expect: System.Int32, got: true path: 'key'", enc.Error.msg.ToString());
                    

                    // ------------------------------------- Array -------------------------------------
                    AreEqual(null,                Read<string[]>    (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(null,                Read<double[]>    (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(null,                Read<float[]>     (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(null,                Read<long[]>      (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(null,                Read<int[]>       (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(null,                Read<short[]>     (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(null,                Read<byte[]>      (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(null,                Read<bool[]>      (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    
                    enc.Read<TestStruct[]>(arrNull);
                    StringAssert.Contains("Cannot assign null to array element. Expect: Friflo.Json.Tests.Common.UnitTest.Mapper.TestStruct, got: null", enc.Error.msg.ToString());
                    enc.Read<int[]>(arrNull);
                    StringAssert.Contains("Cannot assign null to array element. Expect: System.Int32, got: null path: '[0]'", enc.Error.msg.ToString());
                    enc.Read<int[]>(arrStr);
                    StringAssert.Contains("Cannot assign string to array element. Expect: System.Int32, got: 'hello' path: '[0]'", enc.Error.msg.ToString());

                    AreEqual(new [] {"hello"},    Read<string[]>    (arrStr));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    {
                        var inOut = new string[1];
                        IsTrue(ReadTo(arrStr, inOut));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                        AreEqual(new[] {"hello"}, inOut);
                    }
                    // --- array non nullable
                    AreEqual(new [] {1,2,3},      Read   <long[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new [] {1,2,3},      Read    <int[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new [] {1,2,3},      Read  <short[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new [] {1,2,3},      Read   <byte[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                            
                    AreEqual(new [] {1,2,3},      Read <double[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new [] {1,2,3},      Read  <float[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                            
                    AreEqual(new [] {true, false},Read   <bool[]>    (arrBln));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    // --- array nullable
                    AreEqual(new [] {1,2,3},      Read  <long?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new [] {1,2,3},      Read   <int?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new [] {1,2,3},      Read <short?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new [] {1,2,3},      Read  <byte?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                            
                    AreEqual(new [] {1,2,3},      Read<double?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new [] {1,2,3},      Read <float?[]>    (arrNum));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                            
                    AreEqual(new [] {true, false},Read <bool?[]>     (arrBln));          AreEqual(JsonEvent.EOF, enc.parser.Event);
                    // array nullable - with null
                    AreEqual(new   long?[] {null},  Read   <long?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new    int?[] {null},  Read    <int?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new  short?[] {null},  Read  <short?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new   byte?[] {null},  Read   <byte?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.parser.Event);

                    AreEqual(new double?[] {null},  Read <double?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.parser.Event);
                    AreEqual(new  float?[] {null},  Read   <byte?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.parser.Event);

                    AreEqual(new   bool?[] {null},  Read   <bool?[]>   (arrNull));         AreEqual(JsonEvent.EOF, enc.parser.Event);

                    {
                        Dictionary<string, int>[] expect = {new Dictionary<string, int> {{"key", 42}}};
                        AreEqual(expect, Read<Dictionary<string, int>[]>(arrObj));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                    }

                    // --- array of array
                    {
                        int[][] expect = {new []{1, 2, 3}};
                        AreEqual(expect, Read<int[][]>(arrArrNum));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                    }
                    {
                        Dictionary<string, int>[][] expect = {new []{ new Dictionary<string, int> {{"key", 42}}}};
                        AreEqual(expect, Read<Dictionary<string, int>[][]>(arrArrObj));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                    }
                    // --- multi dimensional arrays
                    {
                        // int[,] expect = {{1, 2, 3}};
                        var e = Assert.Throws<NotSupportedException>(() => enc.Read<int[,]>(arrArrNum));
                        AreEqual("Type not supported. Type: System.Int32[,]", e.Message);              
                    }
                    
                    // ------------------------------------- List<T> -------------------------------------
                    AreEqual(null, Read<List<int>>    (@null));           AreEqual(JsonEvent.EOF, enc.parser.Event);
                    {
                        List<int> expect = new List<int> {1, 2, 3};
                        AreEqual(expect, Read<List<int>> (arrNum));
                        AreEqual(expect, Read<List<int?>>(arrNum));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                    } {
                        List<bool> expect = new List<bool> {true, false};
                        AreEqual(expect, Read<List<bool>>(arrBln));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                    } {
                        var expect = new List<int?> { null };
                        AreEqual(expect, Read<List<int?>>(arrNull));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                    }
                    // --- list of list
                    {
                        List<List<int>> expect = new List<List<int>> {new List<int> {1, 2, 3}};
                        AreEqual(expect, Read<List<List<int>>>(arrArrNum));
                    }
                    
                    enc.Read<List<int>>(arrNull);
                    StringAssert.Contains("Cannot assign null to List element. Expect: System.Int32, got: null path: '[0]'", enc.Error.msg.ToString());
                    
                    enc.Read<List<int>>(arrStr);
                    StringAssert.Contains("Cannot assign string to List element. Expect: System.Int32, got: 'hello' path: '[0]'", enc.Error.msg.ToString());
                    
                    enc.Read<List<string>>(arrNum);
                    StringAssert.Contains("Cannot assign number to List element. Expect: System.String, got: 1 path: '[0]'", enc.Error.msg.ToString());

                    enc.Read<List<string>>(arrBln);
                    StringAssert.Contains("Cannot assign bool to List element. Expect: System.String, got: true path: '[0]'", enc.Error.msg.ToString());
                    
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
                        AreEqual("Dictionary only support string as key type. Type: System.Collections.Generic.Dictionary`2[System.Int32,System.String]", e.Message);              
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
                    // --- map derivations                
                    {
                        var expect = new Dictionary<string, long> {{"key", 42}};
                        AreEqual(expect, Read<ConcurrentDictionary<string, long>>(mapNum));
                        // AreEqual(expect, enc.Read<ReadOnlyDictionary<string, long>>(mapNum));
                    }
                    
                    // ---- BigInteger ---
                    AreEqual(new TestStruct{ key = 42 },        Read<TestStruct>    (mapNum));
                    AreEqual(default(TestStruct),               enc.Read<TestStruct>(@null));
                    StringAssert.Contains("Cannot assign null to class. Expect: Friflo.Json.Tests.Common.UnitTest.Mapper.TestStruct, got: null path: '(root)'", enc.Error.msg.ToString());
                    {
                        BigInteger expect = BigInteger.Parse(bigInt.ToString());
                        AreEqual(expect, Read<BigInteger>(bigIntStr));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                        
                        AreEqual(expect, Read<BigInteger>(bigIntStrN));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                        
                        AreEqual(expect, Read<BigInteger>(bigInt));
                        AreEqual(JsonEvent.EOF, enc.parser.Event);
                        
                        write.Write(expect);
                        AreEqual(bigIntStr.ToString(), write.Output.ToString());
                    }
                    enc.Read<BigInteger>(hello);
                    StringAssert.Contains("Failed parsing BigInt. value:", enc.Error.msg.ToString());
                    
                    // --- DateTime ---
                    {
                        DateTime expect = DateTime.Parse(dateTime.ToString());
                        DateTime value = Read<DateTime>(dateTimeStr);
                        AreEqual(expect, value);
                        
                        write.Write(expect);
                        AreEqual(dateTimeStr.ToString(), write.Output.ToString());   
                    }
                    enc.Read<DateTime>(hello);
                    StringAssert.Contains("Failed parsing DateTime. value:", enc.Error.msg.ToString());

                    // Ensure minimum required type lookups
                    if (n > 0) {
#if !UNITY_EDITOR
                        AreEqual(125, enc.typeCache.LookupCount);
#endif
                        AreEqual( 0, enc.typeCache.StoreLookupCount);
                        AreEqual( 0, enc.typeCache.TypeCreationCount);
                    }
                    enc.typeCache.ClearCounts();
                }
            }
        }

        private JsonReader reader;
        
        private JsonReader read2;
        private JsonWriter write2;

        private T Read<T>(Bytes bytes) {
            // return reader.Read<T>(bytes);

            if (!reader.Read(bytes, out T result))
                return result;
            
            write2.Write(result);
            T writeResult = read2.Read<T>(write2.bytes);

            AreEqual(result, writeResult);
            return result;
        }
        
        private bool ReadTo<T>(Bytes bytes, T value) where T : class {
            // return reader.ReadTo<T>(bytes, value);

            bool success = reader.ReadTo(bytes, value);
            if (!success)
                return false;
            
            write2.Write(value);
            T writeResult = read2.Read<T>(write2.bytes);

            AreEqual(value, writeResult);
            return true;
        }


    }
}