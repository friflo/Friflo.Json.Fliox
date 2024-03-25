// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
// using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.Utils.SimpleAssert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{

    public class TestILClassMapper : LeakTestsFixture
    {
        static readonly string boxedStr = $@"
{{
    ""bigInt""      : ""123"",
    ""dateTime""    : ""2021-01-14T09:59:40.101Z"",
    ""enumIL""      : ""two""
}}";
        [Test]
        public static void ReadWriteBoxed() {
            string payloadTrimmed = string.Concat(boxedStr.Where(c => !char.IsWhiteSpace(c)));
            
            using (var typeStore   = new TypeStore(new StoreConfig()))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer      = new ObjectWriter(typeStore))
            using (var json        = new Bytes(payloadTrimmed))
            {
                var result = reader.Read<BoxedIL>(json);
                if (reader.Error.ErrSet)
                    Fail(reader.Error.msg.ToString());
                var jsonResult = writer.Write(result);
                AreEqual(payloadTrimmed, jsonResult);
            }
        }

        private static readonly string StructJson = $@"
{{
    ""structInt"": 200,
    ""child1"" : {{
        ""val2"": 201
    }},
    ""child2"" : null,
    ""childClass1"": null,
    ""childClass2"": {{
        ""val"": 202
    }}
}}
";


        [Test]
        public static void        WriteStruct() {
            string payloadTrimmed = string.Concat(StructJson.Where(c => !char.IsWhiteSpace(c)));
            
            using (var typeStore   = new TypeStore(new StoreConfig()))
            using (var writer      = new ObjectWriter(typeStore))
            {
                var sample = new StructIL();
                sample.Init();
                var jsonResult = writer.Write(sample);
                AreEqual(payloadTrimmed, jsonResult);
            }
        }
        
        private static void AssertStructIL(ref StructIL structIL) {
            AreEqual(200,   structIL.structInt);
            
            AreEqual(201,   structIL.child1.Value.val2);
            AreEqual(false, structIL.child2.HasValue);
            
            AreEqual(null,  structIL.childClass1);
            AreEqual(202,   structIL.childClass2.val);
        }
        
        [Test]
        public static void        ReadStruct() {
            using (var typeStore   = new TypeStore(new StoreConfig()))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            {
                var result = reader.Read<StructIL>(StructJson);
                if (reader.Error.ErrSet)
                    Fail(reader.Error.msg.ToString());

                AssertStructIL(ref result);
            }
        }
        
        [Test]
        public static void        ReadStruct_Error() {
            using (var typeStore   = new TypeStore(new StoreConfig()))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            {
                var json = "{\"structInt\": true}";
                reader.Read<StructIL>(json);
                IsTrue (reader.Error.ErrSet);
                AreEqual("JsonReader/error: Cannot assign bool to int. got: true path: 'structInt' at position: 18", reader.Error.msg.ToString());
            }
        }

        private static readonly string PayloadStr = $@"
{{
    ""enumIL1""          : ""three"",
    ""enumIL2""          : null,
    ""childStructNull1"" : null,
    ""childStructNull2"" : {{
        ""val2"": 19
    }},
    ""nulDouble""       : 20.5,
    ""nulDoubleNull""   : null,
    ""nulFloat""        : 21.5,
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
    ""structIL"": {StructJson},
    ""dbl"":   22.5,
    ""flt"":   33.5,

    ""int64"": 10,
    ""int32"": 11,
    ""int16"": 12,
    ""int8"":  13,

    ""bln"":   true
}}
";
        private static void AssertSampleIL(SampleIL sample) {
            // ReSharper disable PossibleInvalidOperationException
            AreEqual(null,  sample.enumIL2);
            AreEqual(false, sample.childStructNull1.HasValue);
            AreEqual(19,    sample.childStructNull2.Value.val2);
            

            AreEqual(20.5d, sample.nulDouble.Value);
            AreEqual(null,  sample.nulDoubleNull);
            AreEqual(21.5f, sample.nulFloat.Value);
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
            AssertStructIL(ref sample.structIL);
                
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

        [Test]
        public static  void        WriteJson() {
            string payloadTrimmed = string.Concat(PayloadStr.Where(c => !char.IsWhiteSpace(c)));
            
            using (var typeStore   = new TypeStore(new StoreConfig()))
            using (var writer      = new ObjectWriter(typeStore))
            {
                var sample = new SampleIL();
                sample.Init();
                var jsonResult = writer.Write(sample);
                AreEqual(payloadTrimmed, jsonResult);
            }
        }
        
        
        [Test]
        public static  void        ReadClass() {
            using (var typeStore   = new TypeStore(new StoreConfig()))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            {
                var result = reader.Read<SampleIL>(PayloadStr);
                if (reader.Error.ErrSet)
                    Fail(reader.Error.msg.ToString());

                AssertSampleIL(result);
            }
        }
        
        [Test]
        public static void        NoAllocWriteClass () {

            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (var typeStore   = new TypeStore(new StoreConfig()))
            using (var writer      = new ObjectWriter(typeStore))
            using (var dst         = new TestBytes())
            {
                var obj = new SampleIL();
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(obj, ref dst.bytes);
                }
            }
            // memLog.AssertNoAllocations();
        }
        
        
        [Test]
        public static  void        NoAllocReadClass () {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (var typeStore   = new TypeStore(new StoreConfig()))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var json        = new Bytes(PayloadStr))
            {
                var obj = new SampleIL();
                obj.Init();
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    obj = reader.ReadTo(json, obj, false);
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                    AssertSampleIL(obj);
                }
            }

            // memLog.AssertNoAllocations();
        }
        
        [Test]
        public static void        ReadWriteStruct () {
            using (var typeStore   = new TypeStore(new StoreConfig()))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer      = new ObjectWriter(typeStore))
            using (var dst         = new TestBytes())
            {
                var obj = new ChildStructIL();
                writer.Write(obj, ref dst.bytes);
                var result = reader.Read<ChildStructIL>(dst.bytes);
                if (reader.Error.ErrSet)
                    Fail(reader.Error.msg.ToString());
                AreEqual(obj, result);
            }
        }
        
        
        [Test]
        public static void        NoAllocListClass () {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (var typeStore   = new TypeStore(new StoreConfig()))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer      = new ObjectWriter(typeStore))
            using (var dst         = new TestBytes())
            {
                var list = new List<SampleIL>() { new SampleIL() };
                list[0].Init();
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(list, ref dst.bytes);
                    list = reader.ReadTo(dst.bytes, list, false);
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                    AssertSampleIL(list[0]);
                }
            }

            // memLog.AssertNoAllocations();
        }
        
        [Test]
        public static void        NoAllocListStruct () {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (var typeStore   = new TypeStore(new StoreConfig()))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer      = new ObjectWriter(typeStore))
            using (var dst         = new TestBytes())
            {
                var list = new List<ChildStructIL>() { new ChildStructIL{val2 = 42} };
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(list, ref dst.bytes);
                    list[0] = new ChildStructIL { val2 = 999 };
                    list = reader.ReadTo(dst.bytes, list, false);
                    AreEqual(42, list[0].val2);   // ensure List element being a struct is updated
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                }
            }
            // memLog.AssertNoAllocations();
        }

        [Test]
        public static void        NoAllocArrayClass () {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (var typeStore   = new TypeStore(new StoreConfig()))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer      = new ObjectWriter(typeStore))
            using (var dst         = new TestBytes())
            {
                var arr = new [] { new SampleIL() };
                arr[0].Init();
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(arr, ref dst.bytes);
                    arr = reader.ReadTo(dst.bytes, arr, false);
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                    AssertSampleIL(arr[0]);
                }
            }
            // memLog.AssertNoAllocations();
        }
        
        [Test]
        public static void        NoAllocArrayStruct () {
            var memLog      = new MemoryLogger(100, 100, MemoryLog.Enabled);

            using (var typeStore   = new TypeStore(new StoreConfig()))
            using (var reader      = new ObjectReader(typeStore) { ErrorHandler =  ObjectReader.NoThrow} )
            using (var writer      = new ObjectWriter(typeStore))
            using (var dst         = new TestBytes())
            {
                var arr = new [] { new ChildStructIL{val2 = 42} };
                int iterations = 1000;
                for (int n = 0; n < iterations; n++) {
                    memLog.Snapshot();
                    writer.Write(arr, ref dst.bytes);
                    arr[0] = new ChildStructIL { val2 = 999 };
                    arr = reader.ReadTo(dst.bytes, arr, false);
                    AreEqual(42, arr[0].val2);   // ensure array element being a struct is updated
                    if (reader.Error.ErrSet)
                        Fail(reader.Error.msg.ToString());
                }
            }

            // memLog.AssertNoAllocations();
        }

    }
}
