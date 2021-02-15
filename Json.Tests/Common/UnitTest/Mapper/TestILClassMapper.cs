using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
// using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.Utils.SimpleAssert;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    enum EnumIL {
        one,
        two,
        three,
    }
    
    class BoxedIL {
#pragma warning disable 649
        public BigInteger   bigInt;
        public DateTime     dateTime;
        public EnumIL       enumIL;
#pragma warning restore 649
    }
    
    class ChildIL
    {
        public int val;
    }

    struct StructIL
    {
        public int val2;
    }
    
    class SampleIL
    {
        public EnumIL   enumIL1;
        public EnumIL?  enumIL2;
            
        public StructIL?childStructNull1;
        public StructIL?childStructNull2;
        
        public double?  nulDouble;
        public double?  nulDoubleNull;

        public float?   nulFloat;
        public float?   nulFloatNull;

        public long?    nulLong;
        public long?    nulLongNull;
        
        public int?     nulInt;
        public int?     nulIntNull;
        
        public short?   nulShort;
        public short?   nulShortNull;
        
        public byte?    nulByte;
        public byte?    nulByteNull;
        
        public bool?    nulBool;
        public bool?    nulBoolNull;
        
        public StructIL childStruct1;
        public StructIL childStruct2;
        
        public ChildIL  child;
        public ChildIL  childNull;
        
        public double   dbl;
        public float    flt;
              
        public long     int64;
        public int      int32;
        public short    int16;
        public byte     int8;
              
        public bool     bln;

        public SampleIL() {
            enumIL1 = EnumIL.one;
            enumIL2 = EnumIL.two;
                
            childStructNull1 = new StructIL {val2 = 68};
            childStructNull2 = new StructIL {val2 = 69};
            
            nulDouble       = 70;
            nulDoubleNull   = 71;
            nulFloat        = 72;
            nulFloatNull    = 73;
            nulLong         = 74;
            nulLongNull     = 75;
            nulInt          = 76;
            nulIntNull      = 77;
            nulShort        = 78;
            nulShortNull    = 79;
            nulByte         = 80;
            nulByteNull     = 81;
            nulBool         = false;
            nulBoolNull     = true;

            //
            childStruct1.val2 = 90;
            childStruct2.val2 = 91;
            child =     new ChildIL { val = 92 };
            childNull = new ChildIL { val = 93 };
            dbl   = 94;
            flt   = 95;
            
            int64 = 96;
            int32 = 97;
            int16 = 98;
            int8  = 99;
            bln   = true;
        }

        public void Init() {
            enumIL1          = EnumIL.three;
            enumIL2          = null;
                
            childStructNull1 = null;
            childStructNull2 = new StructIL {val2 = 19};
            
            nulDouble       = 20;
            nulDoubleNull   = null;
            
            nulFloat        = 21;
            nulFloatNull    = null;

            nulLong         = 22;
            nulLongNull     = null;
            nulInt          = 23;
            nulIntNull      = null;
            nulShort        = 24;
            nulShortNull    = null;
            nulByte         = 25;
            nulByteNull     = null;
            
            nulBool         = true;
            nulBoolNull     = null;
            
            child = new ChildIL { val = 42 };
            childStruct1.val2 = 111;
            childStruct2.val2 = 112;
            childNull = null;
            dbl   = 22.5d;
            flt   = 33.5f;
            
            int64 = 10;
            int32 = 11;
            int16 = 12;
            int8  = 13;
            bln   = true;
        }
    }
    
    public class TestILClassMapper
    {
        readonly string boxedStr = $@"
{{
    ""bigInt""      : ""123"",
    ""dateTime""    : ""2021-01-14T09:59:40.101Z"",
    ""enumIL""      : ""two""
}}";
        [Test]
        public void ReadWriteBoxed() {
            string payloadTrimmed = string.Concat(boxedStr.Where(c => !char.IsWhiteSpace(c)));
            
            using (TypeStore    typeStore   = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
            using (JsonReader   reader      = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            using (Bytes        json        = new Bytes(payloadTrimmed))
            {
                var result = reader.Read<BoxedIL>(json);
                if (reader.Error.ErrSet)
                    Fail(reader.Error.msg.ToString());
                var jsonResult = writer.Write(result);
                AreEqual(payloadTrimmed, jsonResult);
            }
        }
        
        
        readonly string payloadStr = $@"
{{
    ""enumIL1""          : ""three"",
    ""enumIL2""          : null,
    ""childStructNull1"" : null,
    ""childStructNull2"" : {{
        ""val2"": 19
    }},
    ""nulDouble""       : 20.0,
    ""nulDoubleNull""   : null,
    ""nulFloat""        : 21.0,
    ""nulFloatNull""    : null,
    ""nulLong""         : 22,
    ""nulLongNull""     : null,
    ""nulInt""          : 23,
    ""nulIntNull""      : null,
    ""nulShort""        : 24,
    ""nulShortNull""    : null,
    ""nulByte""         : 25,
    ""nulByteNull""     : null,
    ""nulBool""         : true,
    ""nulBoolNull""     : null,

    ""childStruct1"": {{
        ""val2"": 111
    }},
    ""childStruct2"": {{
        ""val2"": 112
    }},
    ""child"": {{
        ""val"": 42
    }},
    ""childNull"": null,
    ""dbl"":   22.5,
    ""flt"":   33.5,

    ""int64"": 10,
    ""int32"": 11,
    ""int16"": 12,
    ""int8"":  13,

    ""bln"":   true
}}
";
        private void AssertSampleIL(SampleIL sample) {
            // ReSharper disable PossibleInvalidOperationException
            AreEqual(null,  sample.enumIL2);
            // AreEqual(false, sample.childStructNull1.HasValue);
            // AreEqual(19,    sample.childStructNull2.Value.val2);

            AreEqual(20d,   sample.nulDouble.Value);
            AreEqual(null,  sample.nulDoubleNull);
            AreEqual(21f,   sample.nulFloat.Value);
            AreEqual(null,  sample.nulFloatNull);
            AreEqual(22L,   sample.nulLong.Value);
            AreEqual(null,  sample.nulLongNull);
            AreEqual(23,    sample.nulInt.Value);
            AreEqual(null,  sample.nulIntNull);
            AreEqual(24,    sample.nulShort.Value);
            AreEqual(null,  sample.nulShortNull);
            AreEqual(25,    sample.nulByte.Value);
            AreEqual(null,  sample.nulByteNull);
            AreEqual(true,  sample.nulBool.Value);
            AreEqual(null,  sample.nulBoolNull);
            
            AreEqual(111,   sample.childStruct1.val2);
            AreEqual(112,   sample.childStruct2.val2);
            AreEqual(42,    sample.child.val);
            AreEqual(null,  sample.childNull);
                
            AreEqual(22.5,  sample.dbl);
            AreEqual(33.5,  sample.flt);
                
            AreEqual(10,    sample.int64);
            AreEqual(11,    sample.int32);
            AreEqual(12,    sample.int16);
            AreEqual(13,    sample.int8);
            AreEqual(true,  sample.bln);

            AreEqual(42,    sample.child.val);
            AreEqual(null,  sample.childNull);
            AreEqual(111,   sample.childStruct1.val2);
            AreEqual(112,   sample.childStruct2.val2);
        }

        [Test] public void  WriteJsonReflect()   { WriteJson(TypeAccess.Reflection); }
        [Test] public void  WriteJsonIL()        { WriteJson(TypeAccess.IL); }

        private void        WriteJson(TypeAccess typeAccess) {
            string payloadTrimmed = string.Concat(payloadStr.Where(c => !char.IsWhiteSpace(c)));
            
            using (TypeStore    typeStore   = new TypeStore(null, new StoreConfig(typeAccess)))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            {
                var sample = new SampleIL();
                sample.Init();
                var jsonResult = writer.Write(sample);
                AreEqual(payloadTrimmed, jsonResult);
            }
        }
        
        [Test] public void  ReadJsonReflect()   { ReadJson(TypeAccess.Reflection); }
        [Test] public void  ReadJsonIL()        { ReadJson(TypeAccess.IL); }
        
        private void        ReadJson(TypeAccess typeAccess) {
            using (TypeStore    typeStore   = new TypeStore(null, new StoreConfig(typeAccess)))
            using (JsonReader   reader      = new JsonReader(typeStore, JsonReader.NoThrow))
            {
                var result = reader.Read<SampleIL>(payloadStr);
                if (reader.Error.ErrSet)
                    Fail(reader.Error.msg.ToString());

                AssertSampleIL(result);
            }
        }
        
        [Test] public void  NoAllocWriteClassReflect()   { NoAllocWriteClass(TypeAccess.Reflection); }
        [Test] public void  NoAllocWriteClassIL()        { NoAllocWriteClass(TypeAccess.IL); }
        
        private void        NoAllocWriteClass (TypeAccess typeAccess) {

            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (TypeStore    typeStore   = new TypeStore(null, new StoreConfig(typeAccess)))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            using (var          dst         = new TestBytes())
            {
                var obj = new SampleIL();
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(obj, ref dst.bytes);
                }
            }
            if (typeAccess == TypeAccess.IL)
                memLog.AssertNoAllocations();
        }
        
        [Test] public void  NoAllocReadClassReflect()   { NoAllocReadClass(TypeAccess.Reflection); }
        [Test] public void  NoAllocReadClassIL()        { NoAllocReadClass(TypeAccess.IL); }
        
        private void        NoAllocReadClass (TypeAccess typeAccess) {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (TypeStore    typeStore   = new TypeStore(null, new StoreConfig(typeAccess)))
            using (JsonReader   reader      = new JsonReader(typeStore, JsonReader.NoThrow))
            using (Bytes        json        = new Bytes(payloadStr))
            {
                var obj = new SampleIL();
                obj.Init();
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    obj = reader.ReadTo(json, obj);
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                    AssertSampleIL(obj);
                }
            }
            if (typeAccess == TypeAccess.IL)
                memLog.AssertNoAllocations();
        }
        
        [Test] public void  ReadWriteStructReflect()   { ReadWriteStruct(TypeAccess.Reflection); }
        [Test] public void  ReadWriteStructIL()        { ReadWriteStruct(TypeAccess.IL); }
        
        private void        ReadWriteStruct (TypeAccess typeAccess) {
            using (TypeStore    typeStore   = new TypeStore(null, new StoreConfig(typeAccess)))
            using (JsonReader   reader      = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            using (var          dst         = new TestBytes())
            {
                var obj = new StructIL();
                writer.Write(obj, ref dst.bytes);
                var result = reader.Read<StructIL>(dst.bytes);
                if (reader.Error.ErrSet)
                    Fail(reader.Error.msg.ToString());
                AreEqual(obj, result);
            }
        }
        
        [Test] public void NoAllocListClassReflect()   { NoAllocListClass(TypeAccess.Reflection); }
        [Test] public void NoAllocListClassIL()        { NoAllocListClass(TypeAccess.IL); }
        
        private void        NoAllocListClass (TypeAccess typeAccess) {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (TypeStore    typeStore   = new TypeStore(null, new StoreConfig(typeAccess)))
            using (JsonReader   reader      = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            using (var          dst         = new TestBytes())
            {
                var list = new List<SampleIL>() { new SampleIL() };
                list[0].Init();
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(list, ref dst.bytes);
                    list = reader.ReadTo(dst.bytes, list);
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                    AssertSampleIL(list[0]);
                }
            }
            if (typeAccess == TypeAccess.IL)
                memLog.AssertNoAllocations();
        }
        
        [Test]  public void NoAllocListStructReflect()   { NoAllocListStruct(TypeAccess.Reflection); }
        [Test]  public void NoAllocListStructIL()        { NoAllocListStruct(TypeAccess.IL); } 
        
        private void        NoAllocListStruct (TypeAccess typeAccess) {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (TypeStore    typeStore   = new TypeStore(null, new StoreConfig(typeAccess)))
            using (JsonReader   reader      = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            using (var          dst         = new TestBytes())
            {
                var list = new List<StructIL>() { new StructIL{val2 = 42} };
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(list, ref dst.bytes);
                    list[0] = new StructIL { val2 = 999 };
                    list = reader.ReadTo(dst.bytes, list);
                    AreEqual(42, list[0].val2);   // ensure List element being a struct is updated
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                }
            }
            if (typeAccess == TypeAccess.IL)
                memLog.AssertNoAllocations();
        }

        [Test]  public void NoAllocArrayClassReflect()   { NoAllocArrayClass(TypeAccess.Reflection); }
        [Test]  public void NoAllocArrayClassIL()        { NoAllocArrayClass(TypeAccess.IL); } 
        
        private void        NoAllocArrayClass (TypeAccess typeAccess) {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (TypeStore    typeStore   = new TypeStore(null, new StoreConfig(typeAccess)))
            using (JsonReader   reader      = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            using (var          dst         = new TestBytes())
            {
                var arr = new [] { new SampleIL() };
                arr[0].Init();
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(arr, ref dst.bytes);
                    arr = reader.ReadTo(dst.bytes, arr);
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                    AssertSampleIL(arr[0]);
                }
            }
            if (typeAccess == TypeAccess.IL)
                memLog.AssertNoAllocations();
        }
        
        [Test]  public void NoAllocArrayStructReflect()   { NoAllocArrayStruct(TypeAccess.Reflection); }
        [Test]  public void NoAllocArrayStructIL()        { NoAllocArrayStruct(TypeAccess.IL); }
        
        private void        NoAllocArrayStruct (TypeAccess typeAccess) {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (TypeStore    typeStore   = new TypeStore(null, new StoreConfig(typeAccess)))
            using (JsonReader   reader      = new JsonReader(typeStore, JsonReader.NoThrow))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            using (var          dst         = new TestBytes())
            {
                var arr = new [] { new StructIL{val2 = 42} };
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(arr, ref dst.bytes);
                    arr[0] = new StructIL { val2 = 999 };
                    arr = reader.ReadTo(dst.bytes, arr);
                    AreEqual(42, arr[0].val2);   // ensure array element being a struct is updated
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                }
            }
            if (typeAccess == TypeAccess.IL)
                memLog.AssertNoAllocations();
        }

    }
}

#endif

