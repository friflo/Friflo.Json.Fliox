using System;
using Friflo.Json.Burst;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif


namespace Friflo.Json.Tests.Common.UnitTest
{
    public struct Int3
    {
        public int x, y, z;
        
        unsafe public int this[int index] {
            get { fixed (Int3* array = &this)   { return ((int*)array)[index]; } }
            set { fixed (int* array = &x)       { array[index] = value; } }
        }
        
        public void Read (ref JsonParser p) {
            int index = 0;
            while (p.NextArrayElement()) {
                if      (p.Event == JsonEvent.ValueNumber)  { this[index++] = p.ValueAsInt(out _); }
                else                                        { p.SkipEvent(); }
            }
        }
        
        public void Read2 (ref JsonParser p) {
            int index = 0;
            var arr = new ReadArray();
            while (arr.NextElement(ref p)) {
                if (arr.UseNum(ref p))       { this[index++] = p.ValueAsInt(out _); }
            }
        }
    }
    
    public class TestParseJsonManual : LeakTestsFixture
    {


        [Test]
        public void ParseJsonManual() {
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                RunParser(bytes, 1, MemoryLog.Disabled);
            }
        }
        
        [Test]
        public void ParseCheckAllocations() {
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                RunParser(bytes, 1000, MemoryLog.Enabled);
            }
        }

        private void RunParser(Bytes bytes, int iterations, MemoryLog memoryLog) {
            var memLog = new MemoryLogger(100, 100, memoryLog);
            var parser = new JsonParser();
            try {
                using (ParseManual manual = new ParseManual(Default.Constructor)) {
                    memLog.Reset();
                    for (int i = 0; i < iterations; i++) {
                        parser.InitParser(bytes);
                        parser.NextEvent(); // ObjectStart
                        manual.Root1(ref parser);
                        memLog.Snapshot();
                    }
                    manual.AssertParseResult(ref parser);
                    memLog.AssertNoAllocations();
                }
                using (ParseManual manual = new ParseManual(Default.Constructor)) {
                    memLog.Reset();
                    for (int i = 0; i < iterations; i++) {
                        parser.InitParser(bytes);
                        parser.NextEvent(); // ObjectStart
                        manual.Root2(ref parser);
                        memLog.Snapshot();
                    }

                    manual.AssertParseResult(ref parser);
                    memLog.AssertNoAllocations();
                }
                using (ParseManual manual = new ParseManual(Default.Constructor)) {
                    memLog.Reset();
                    for (int i = 0; i < iterations; i++) {
                        parser.InitParser(bytes);
                        parser.NextEvent(); // ObjectStart
                        manual.Root3(ref parser);
                        memLog.Snapshot();
                    }

                    manual.AssertParseResult(ref parser);
                    memLog.AssertNoAllocations();
                }
            }
            finally {
                parser.Dispose();
            }
        }

        /** pre create json keys upfront, to avoid creating them on the stack, when passing them to the Is...() methods.
         * Also enables navigating to places referencing the same json key. */
        public struct Names
        {
            public Str32 map;
            public Str32 map2;
            public Str32 listStr;
            public Str32 arr;
            public Str32 boolArr;
            public Str32 i64Arr; 
            public Str32 i64;
            public Str32 i64Neg;
            public Str32 str;    
            public Str32 t;     
            public Str32 n;
            public Str32 dbl;
            public Str32 flt;

            public Names(Default _) {
                map =       "map";
                map2 =      "map2";
                listStr =   "listStr";
                arr =       "arr";
                boolArr =   "boolArr";
                i64Arr =    "i64Arr";
                i64 =       "i64";
                i64Neg =    "i64Neg";
                str =       "str";
                t =         "t";
                n =         "n";
                dbl =       "dbl";
                flt =       "flt";
            }
        }
        
        public struct ParseManual : IDisposable
        {
            public  Int3    int3;
            public  long    i64;
            public  long    i64Neg;
            public  Bytes   str;
            public  bool    t;
            public  bool    foundNull;
            public  Bytes   strElement;
            public  bool    foundNullElement;
            public  bool    trueElement;
            private Names   nm;
            private Bytes   temp;
            public  double  dbl;
            public  float   flt;

            public ParseManual(Default _) {
                int3 = new Int3();
                temp = new Bytes(32);
                i64 = 0;
                i64Neg = 0;
                t = false;
                foundNull = false;
                strElement = new Bytes(16);
                str = new Bytes(16);
                foundNullElement = false;
                trueElement = false;
                nm = new Names(Default.Constructor);
                dbl = 0;
                flt = 0;
            }
            
            public void Dispose() {
                temp.Dispose();
                strElement.Dispose();
                str.Dispose();
            }

            public void Root1(ref JsonParser p) {
                ref var key = ref p.key;
                while (p.NextObjectMember()) {
                    if      (key.IsEqual32(ref nm.map)      && p.Event == JsonEvent.ObjectStart)   { p.SkipTree(); }
                    else if (key.IsEqual32(ref nm.map2)     && p.Event == JsonEvent.ObjectStart)   { p.SkipTree(); }
                    else if (key.IsEqual32(ref nm.listStr)  && p.Event == JsonEvent.ArrayStart)    { ReadListStr(ref p); }
                    else if (key.IsEqual32(ref nm.arr)      && p.Event == JsonEvent.ArrayStart)    { ReadArr(ref p); }
                    else if (key.IsEqual32(ref nm.boolArr)  && p.Event == JsonEvent.ArrayStart)    { ReadBoolArr(ref p); }
                    else if (key.IsEqual32(ref nm.i64Arr)   && p.Event == JsonEvent.ArrayStart)    { int3.Read(ref p); }
                    else if (key.IsEqual32(ref nm.i64)      && p.Event == JsonEvent.ValueNumber)   { i64 = p.ValueAsLong(out _); }
                    else if (key.IsEqual32(ref nm.i64Neg)   && p.Event == JsonEvent.ValueNumber)   { i64Neg = p.ValueAsLong(out _); }
                    else if (key.IsEqual32(ref nm.str)      && p.Event == JsonEvent.ValueString)   { str.Set(ref p.value); }
                    else if (key.IsEqual32(ref nm.t)        && p.Event == JsonEvent.ValueBool)     { t = p.boolValue; }
                    else if (key.IsEqual32(ref nm.n)        && p.Event == JsonEvent.ValueNull)     { foundNull = true; }
                    else if (key.IsEqual32(ref nm.dbl)      && p.Event == JsonEvent.ValueNumber)   { dbl = p.ValueAsDouble(out _); }
                    else if (key.IsEqual32(ref nm.flt)      && p.Event == JsonEvent.ValueNumber)   { flt = p.ValueAsFloat(out _); }
                    else                                                                           { p.SkipEvent(); }
                }
            }
            
            public void Root2(ref JsonParser p) {
                while (p.NextObjectMember()) {
                    if      (p.IsMemberObj(ref nm.map))       { p.SkipTree(); }
                    else if (p.IsMemberObj(ref nm.map2))      { p.SkipTree(); }
                    else if (p.IsMemberArr(ref nm.listStr))   { ReadListStr(ref p); }
                    else if (p.IsMemberArr(ref nm.arr))       { ReadArr(ref p); }
                    else if (p.IsMemberArr(ref nm.boolArr))   { ReadBoolArr(ref p); }
                    else if (p.IsMemberArr(ref nm.i64Arr))    { int3.Read(ref p); }
                    else if (p.IsMemberNum(ref nm.i64))       { i64 = p.ValueAsLong(out _); }
                    else if (p.IsMemberNum(ref nm.i64Neg))    { i64Neg = p.ValueAsLong(out _); }
                    else if (p.IsMemberStr(ref nm.str))       { str.Set(ref p.value); }
                    else if (p.IsMemberBln(ref nm.t))         { t = p.boolValue; }
                    else if (p.IsMemberNul(ref nm.n))         { foundNull = true; }
                    else if (p.IsMemberNum(ref nm.dbl))       { dbl = p.ValueAsDouble(out _); }
                    else if (p.IsMemberNum(ref nm.flt))       { flt = p.ValueAsFloat(out _); }
                    else                                      { p.SkipEvent(); }
                }
            }
            
            public void Root3(ref JsonParser p) {
                var obj = new ReadObject();
                while (obj.NextMember(ref p)) {
                    if      (obj.UseObj(ref p, ref nm.map))         { p.SkipTree(); }
                    else if (obj.UseObj(ref p, ref nm.map2))        { p.SkipTree(); }
                    else if (obj.UseArr(ref p, ref nm.listStr))     { ReadListStr2(ref p); }
                    else if (obj.UseArr(ref p, ref nm.arr))         { ReadArr2(ref p); }
                    else if (obj.UseArr(ref p, ref nm.boolArr))     { ReadBoolArr2(ref p); }
                    else if (obj.UseArr(ref p, ref nm.i64Arr))      { int3.Read2(ref p); }
                    else if (obj.UseNum(ref p, ref nm.i64))         { i64 = p.ValueAsLong(out _); }
                    else if (obj.UseNum(ref p, ref nm.i64Neg))      { i64Neg = p.ValueAsLong(out _); }
                    else if (obj.UseStr(ref p, ref nm.str))         { str.Set(ref p.value); }
                    else if (obj.UseBln(ref p, ref nm.t))           { t = p.boolValue; }
                    else if (obj.UseNul(ref p, ref nm.n))           { foundNull = true; }
                    else if (obj.UseNum(ref p, ref nm.dbl))         { dbl = p.ValueAsDouble(out _); }
                    else if (obj.UseNum(ref p, ref nm.flt))         { flt = p.ValueAsFloat(out _); }
                }
            }
            
            void ReadListStr(ref JsonParser p) {
                while (p.NextArrayElement()) {
                    if      (p.Event == JsonEvent.ValueString)      { strElement.Set( ref p.value); }
                    else                                            { p.SkipEvent(); }
                }
            }
            
            void ReadArr(ref JsonParser p) {
                while (p.NextArrayElement()) {
                    if      (p.Event == JsonEvent.ValueNull)        { foundNullElement = true; }
                    else                                            { p.SkipEvent(); }
                }
            }
            
            void ReadArr2(ref JsonParser p) {
                var arr = new ReadArray();
                while (arr.NextElement(ref p)) {
                    if      (arr.UseNul(ref p))                 { foundNullElement = true; }
                }
            }
            
            void ReadListStr2(ref JsonParser p) {
                var arr = new ReadArray();
                while (arr.NextElement(ref p)) {
                    if      (arr.UseStr(ref p))                 { strElement.Set( ref p.value); }
                }
            }
            
            void ReadBoolArr(ref JsonParser p) {
                while (p.NextArrayElement()) {
                    if      (p.Event == JsonEvent.ValueBool)    { trueElement = p.boolValue; }
                    else                                        { p.SkipEvent(); }
                }
            }
            
            void ReadBoolArr2(ref JsonParser p) {
                var arr = new ReadArray();
                while (arr.NextElement(ref p)) {
                    if      (arr.UseBln(ref p))                 { trueElement = p.boolValue; }
                }
            }
            
            public void AssertParseResult(ref JsonParser p) {
                if (p.error.ErrSet) {
                    Fail(p.error.msg.ToString());
                }
                else {
                    AreEqual(JsonEvent.EOF, p.NextEvent());
                    AreEqual(JsonEvent.Error, p.NextEvent()); // check iteration after EOF
                }
                AreEqual(1,                     int3.x);
                AreEqual(2,                     int3.y);
                AreEqual(3,                     int3.z);
                AreEqual(6400000000000000000,   i64);
                AreEqual(-640,                  i64Neg);
                AreEqual("string-ý",            str.ToString());
                AreEqual(true,                  t);
                AreEqual(true,                  foundNull);
                AreEqual("str0",                strElement.ToString());
                AreEqual(true,                  foundNullElement);
                AreEqual(true,                  trueElement);
                AreEqual(22.5,                  dbl);
                AreEqual(11.5,                  flt);
                //
                AreEqual(7,     p.skipInfo.arrays);
                AreEqual(1,     p.skipInfo.booleans);
                AreEqual(1,     p.skipInfo.floats);
                AreEqual(33,    p.skipInfo.integers);
                AreEqual(14,    p.skipInfo.nulls);
                AreEqual(37,    p.skipInfo.objects);
                AreEqual(4,     p.skipInfo.strings);
                AreEqual(97,    p.skipInfo.Sum);
            }
        }
    }
}