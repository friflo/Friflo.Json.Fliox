using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

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
        // public EnumIL?  enumIL2;
            
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
        
        public bool     nulBool;
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
            // enumIL2 = EnumIL.two;
                
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
            // enumIL2          = null;
                
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
            var resolver = new DefaultTypeResolver(new ResolverConfig(true));
            
            using (TypeStore    typeStore   = new TypeStore(resolver))
            using (JsonReader   reader      = new JsonReader(typeStore))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            using (Bytes        json        = new Bytes(payloadTrimmed))
            {
                var result = reader.Read<BoxedIL>(json);
                if (reader.Error.ErrSet)
                    Fail(reader.Error.msg.ToString());
                writer.Write(result);
                AreEqual(payloadTrimmed, writer.Output.ToString());
            }
        }
        
        
        readonly string payloadStr = $@"
{{
    ""enumIL1""          : ""three"",
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
        
        [Test]
        public void WriteJson() {


            string payloadTrimmed = string.Concat(payloadStr.Where(c => !char.IsWhiteSpace(c)));
            var resolver = new DefaultTypeResolver(new ResolverConfig(true));
            
            using (TypeStore    typeStore   = new TypeStore(resolver))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            {
                var sample = new SampleIL();
                sample.Init();
                writer.Write(sample);
                AreEqual(payloadTrimmed, writer.Output.ToString());
            }
        }
        
        [Test]
        public void ReadJson() {
            var resolver = new DefaultTypeResolver(new ResolverConfig(true));
            
            using (TypeStore    typeStore   = new TypeStore(resolver))
            using (JsonReader   reader      = new JsonReader(typeStore))
            using (Bytes        json        = new Bytes(payloadStr))
            {
                var result = reader.Read<SampleIL>(json);
                if (reader.Error.ErrSet)
                    Fail(reader.Error.msg.ToString());
                
                AreEqual(20,    result.nulDouble);
                AreEqual(null,  result.nulDoubleNull);
                
                AreEqual(22.5,  result.dbl);
                AreEqual(33.5,  result.flt);
                
                AreEqual(10,    result.int64);
                AreEqual(11,    result.int32);
                AreEqual(12,    result.int16);
                AreEqual(13,    result.int8);
                AreEqual(true,  result.bln);
                AreEqual(42,    result.child.val);
                AreEqual(null,  result.childNull);
                AreEqual(111,   result.childStruct1.val2);
                AreEqual(112,   result.childStruct2.val2);
            }
        }
        
        [Test]
        public void NoAllocWriteClass () {

            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);
            var resolver    = new DefaultTypeResolver(new ResolverConfig(true));

            using (TypeStore    typeStore   = new TypeStore(resolver))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            {
                var obj = new SampleIL();
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(obj);
                }
            }
            memLog.AssertNoAllocations();
        }
        
        [Test]
        public void NoAllocReadClass () {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);
            var resolver    = new DefaultTypeResolver(new ResolverConfig(true));
            
            using (TypeStore    typeStore   = new TypeStore(resolver))
            using (JsonReader   reader      = new JsonReader(typeStore))
            using (Bytes        json        = new Bytes(payloadStr))
            {
                var obj = new SampleIL();
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    reader.ReadTo(json, obj, out bool _);
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                }
            }
            memLog.AssertNoAllocations();
        }
        
        [Test]
        public void ReadWriteStruct () {
            var resolver    = new DefaultTypeResolver(new ResolverConfig(true));
            
            using (TypeStore    typeStore   = new TypeStore(resolver))
            using (JsonReader   reader      = new JsonReader(typeStore))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            {
                var obj = new StructIL();
                writer.Write(obj);
                var result = reader.Read<StructIL>(writer.Output);
                if (reader.Error.ErrSet)
                    Fail(reader.Error.msg.ToString());
                AreEqual(obj, result);
            }
        }
        
        [Test]
        public void NoAllocListClass () {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);
            var resolver    = new DefaultTypeResolver(new ResolverConfig(true));
            
            using (TypeStore    typeStore   = new TypeStore(resolver))
            using (JsonReader   reader      = new JsonReader(typeStore))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            {
                var list = new List<SampleIL>() { new SampleIL() };
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(list);
                    reader.ReadTo(writer.Output, list, out bool _);
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                }
            }
            memLog.AssertNoAllocations();
        }
        
        [Test]
        public void NoAllocListStruct () {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);
            var resolver    = new DefaultTypeResolver(new ResolverConfig(true));
            
            using (TypeStore    typeStore   = new TypeStore(resolver))
            using (JsonReader   reader      = new JsonReader(typeStore))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            {
                var list = new List<StructIL>() { new StructIL() };
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(list);
                    reader.ReadTo(writer.Output, list, out bool _);
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                }
            }
            memLog.AssertNoAllocations();
        }

        [Test]
        public void NoAllocArrayClass () {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);
            var resolver    = new DefaultTypeResolver(new ResolverConfig(true));
            
            using (TypeStore    typeStore   = new TypeStore(resolver))
            using (JsonReader   reader      = new JsonReader(typeStore))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            {
                var arr = new [] { new SampleIL() };
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(arr);
                    reader.ReadTo(writer.Output, arr, out bool _);
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                }
            }
            memLog.AssertNoAllocations();
        }
        
        [Test]
        public void NoAllocArrayStruct () {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);
            var resolver    = new DefaultTypeResolver(new ResolverConfig(true));
            
            using (TypeStore    typeStore   = new TypeStore(resolver))
            using (JsonReader   reader      = new JsonReader(typeStore))
            using (JsonWriter   writer      = new JsonWriter(typeStore))
            {
                var arr = new [] { new StructIL() };
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(arr);
                    reader.ReadTo(writer.Output, arr, out bool _);
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                }
            }
            memLog.AssertNoAllocations();
        }

    }
}

#endif

