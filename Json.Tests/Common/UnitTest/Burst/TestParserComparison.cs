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


namespace Friflo.Json.Tests.Common.UnitTest.Burst
{
    public struct Int3
    {
        public int x, y, z;
        /*
        unsafe public int this[int index] {
            get { fixed (Int3* array = &this)   { return ((int*)array)[index]; } }
            set { fixed (int* array = &x)       { array[index] = value; } }
        } */
        
        public int this[int index] {
            get  {
                switch (index) {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                }
                throw new IndexOutOfRangeException("");
            }
            set {
                switch (index) {
                    case 0: x = value; return;
                    case 1: y = value; return;
                    case 2: z = value; return;
                }
                throw new IndexOutOfRangeException("");
            }
        }
        
        
        public void ReadManual (ref JsonParser p) {
            int index = 0;
            while (TestParserComparison.NextArrayElement(ref p)) {
                if      (p.Event == JsonEvent.ValueNumber)  { this[index++] = p.ValueAsInt(out _); }
                else                                        { p.SkipEvent(); }
            }
        }
        
        public void ReadAuto (ref JsonParser p, ref JArr i) {
            int index = 0;
            while (i.NextArrayElement(ref p)) {
                if (i.UseElementNum(ref p))                      { this[index++] = p.ValueAsInt(out _); }
            }
        }
    }
    
    /// <summary>
    /// Compare the usage of the <see cref="JsonParser"/> in two similar ways<br/>
    /// 1. using <see cref="NextObjectMember"/> and <see cref="NextArrayElement"/>
    /// 2. using <see cref="JsonParser.NextObjectMember"/> and <see cref="JsonParser.NextArrayElement"/> 
    /// </summary>
    public class TestParserComparison : LeakTestsFixture
    {
        public enum SkipMode {
            Manual,
            Auto
        }

        [Test]
        public void ManualSkip() {
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                RunParser(bytes, SkipMode.Manual, 1, MemoryLog.Disabled);
            }
        }
        
        [Test]
        public void ManualSkipCheckAllocations() {
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                RunParser(bytes, SkipMode.Manual, 1000, MemoryLog.Enabled);
            }
        }
        
        [Test]
        public void AutoSkip() {
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                RunParser(bytes, SkipMode.Auto, 1, MemoryLog.Disabled);
            }
        }
        
        [Test]
        public void AutoSkipCheckAllocations() {
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                RunParser(bytes, SkipMode.Auto, 1000, MemoryLog.Enabled);
            }
        }

        private void RunParser(Bytes bytes, SkipMode skipMode, int iterations, MemoryLog memoryLog) {
            var memLog = new MemoryLogger(100, 100, memoryLog);
            using (var parser = new Local<JsonParser>())
            {
                ref var p = ref parser.value;
                if (skipMode == SkipMode.Manual) {
                    using (ParseManual manual = new ParseManual(Default.Constructor)) {
                        memLog.Reset();
                        for (int i = 0; i < iterations; i++) {
                            p.InitParser(bytes);
                            p.NextEvent(); // ObjectStart
                            manual.RootManualSkip(ref p);
                            memLog.Snapshot();
                        }
                        manual.AssertParseResult(ref p);
                        memLog.AssertNoAllocations();
                    }
                } else {
                    using (ParseManual manual = new ParseManual(Default.Constructor)) {
                        memLog.Reset();
                        for (int i = 0; i < iterations; i++) {
                            p.InitParser(bytes);
                            p.NextEvent(); // ObjectStart
                            p.ReadRootObject(out JObj obj);
                            manual.RootAutoSkip(ref p, ref obj);
                            memLog.Snapshot();
                        }
                        manual.AssertParseResult(ref p);
                        memLog.AssertNoAllocations();
                    }
                }
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

            public void RootManualSkip(ref JsonParser p) {
                ref var key = ref p.key;
                while (NextObjectMember(ref p)) {
                    if      (key.IsEqual32(in nm.map)      && p.Event == JsonEvent.ObjectStart)   { p.SkipTree(); }
                    else if (key.IsEqual32(in nm.map2)     && p.Event == JsonEvent.ObjectStart)   { p.SkipTree(); }
                    else if (key.IsEqual32(in nm.listStr)  && p.Event == JsonEvent.ArrayStart)    { ReadListStrManual(ref p); }
                    else if (key.IsEqual32(in nm.arr)      && p.Event == JsonEvent.ArrayStart)    { ReadArrManual(ref p); }
                    else if (key.IsEqual32(in nm.boolArr)  && p.Event == JsonEvent.ArrayStart)    { ReadBoolArrManual(ref p); }
                    else if (key.IsEqual32(in nm.i64Arr)   && p.Event == JsonEvent.ArrayStart)    { int3.ReadManual(ref p); }
                    else if (key.IsEqual32(in nm.i64)      && p.Event == JsonEvent.ValueNumber)   { i64 = p.ValueAsLong(out _); }
                    else if (key.IsEqual32(in nm.i64Neg)   && p.Event == JsonEvent.ValueNumber)   { i64Neg = p.ValueAsLong(out _); }
                    else if (key.IsEqual32(in nm.str)      && p.Event == JsonEvent.ValueString)   { str.Set(ref p.value); }
                    else if (key.IsEqual32(in nm.t)        && p.Event == JsonEvent.ValueBool)     { t = p.boolValue; }
                    else if (key.IsEqual32(in nm.n)        && p.Event == JsonEvent.ValueNull)     { foundNull = true; }
                    else if (key.IsEqual32(in nm.dbl)      && p.Event == JsonEvent.ValueNumber)   { dbl = p.ValueAsDouble(out _); }
                    else if (key.IsEqual32(in nm.flt)      && p.Event == JsonEvent.ValueNumber)   { flt = p.ValueAsFloat(out _); }
                    else                                                                           { p.SkipEvent(); }
                }
            }
            
            public void RootAutoSkip(ref JsonParser p, ref JObj i) {
                while (i.NextObjectMember(ref p)) {
                    if      (i.UseMemberObj(ref p, in nm.map,     out JObj _))      { p.SkipTree(); }
                    else if (i.UseMemberObj(ref p, in nm.map2,    out JObj _))      { p.SkipTree(); }
                    else if (i.UseMemberArr(ref p, in nm.listStr, out JArr arr1))   { ReadListStrAuto(ref p, ref arr1); }
                    else if (i.UseMemberArr(ref p, in nm.arr,     out JArr arr2))   { ReadArrAuto(ref p, ref arr2); }
                    else if (i.UseMemberArr(ref p, in nm.boolArr, out JArr arr3))   { ReadBoolArrAuto(ref p, ref arr3); }
                    else if (i.UseMemberArr(ref p, in nm.i64Arr,  out JArr arr4))   { int3.ReadAuto(ref p, ref arr4); }
                    else if (i.UseMemberNum(ref p, in nm.i64))       { i64 = p.ValueAsLong(out _); }
                    else if (i.UseMemberNum(ref p, in nm.i64Neg))    { i64Neg = p.ValueAsLong(out _); }
                    else if (i.UseMemberStr(ref p, in nm.str))       { str.Set(ref p.value); }
                    else if (i.UseMemberBln(ref p, in nm.t))         { t = p.boolValue; }
                    else if (i.UseMemberNul(ref p, in nm.n))         { foundNull = true; }
                    else if (i.UseMemberNum(ref p, in nm.dbl))       { dbl = p.ValueAsDouble(out _); }
                    else if (i.UseMemberNum(ref p, in nm.flt))       { flt = p.ValueAsFloat(out _); }
                }
            }
            
            void ReadListStrManual(ref JsonParser p) {
                while (NextArrayElement(ref p)) {
                    if      (p.Event == JsonEvent.ValueString) { strElement.Set( ref p.value); }
                    else                                       { p.SkipEvent(); }
                }
            }
            
            void ReadListStrAuto(ref JsonParser p, ref JArr i) {
                while (i.NextArrayElement(ref p)) {
                    if      (i.UseElementStr(ref p))                { strElement.Set( ref p.value); }
                }
            }
            
            void ReadArrManual(ref JsonParser p) {
                while (NextArrayElement(ref p)) {
                    if      (p.Event == JsonEvent.ValueNull)   { foundNullElement = true; }
                    else                                       { p.SkipEvent(); }
                }
            }
            
            void ReadArrAuto(ref JsonParser p, ref JArr i) {
                while (i.NextArrayElement(ref p)) {
                    if      (i.UseElementNul(ref p))                { foundNullElement = true; }
                }
            }

            void ReadBoolArrManual(ref JsonParser p) {
                while (NextArrayElement(ref p)) {
                    if      (p.Event == JsonEvent.ValueBool)   { trueElement = p.boolValue; }
                    else                                       { p.SkipEvent(); }
                }
            }
            
            void ReadBoolArrAuto(ref JsonParser p, ref JArr i) {
                while (i.NextArrayElement(ref p)) {
                    if      (i.UseElementBln(ref p))                { trueElement = p.boolValue; }
                }
            }
            
            public void AssertParseResult(ref JsonParser p) {
                if (p.error.ErrSet)
                    Fail(p.error.msg.ToString());
                AreEqual(JsonEvent.EOF, p.NextEvent());   // Important to ensure absence of application errors
                AreEqual(JsonEvent.Error, p.NextEvent()); // check iteration after EOF

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
                AreEqual(8,     p.skipInfo.arrays);
                AreEqual(1,     p.skipInfo.booleans);
                AreEqual(1,     p.skipInfo.floats);
                AreEqual(36,    p.skipInfo.integers);
                AreEqual(14,    p.skipInfo.nulls);
                AreEqual(41,    p.skipInfo.objects);
                AreEqual(2,     p.skipInfo.strings);
                AreEqual(103,   p.skipInfo.Sum);
            }
        }
        
        public static bool NextObjectMember (ref JsonParser p) {
            JsonEvent ev = p.NextEvent();
            switch (ev) {
                case JsonEvent.ValueString:
                case JsonEvent.ValueNumber:
                case JsonEvent.ValueBool:
                case JsonEvent.ValueNull:
                case JsonEvent.ObjectStart:
                case JsonEvent.ArrayStart:
                    return true;
                case JsonEvent.ArrayEnd:
                    throw new InvalidOperationException("unexpected ArrayEnd in NoSkipNextObjectMember()");
                case JsonEvent.ObjectEnd:
                    break;
            }
            return false;
        }
        
        public static bool NextArrayElement (ref JsonParser p) {
            JsonEvent ev = p.NextEvent();
            switch (ev) {
                case JsonEvent.ValueString:
                case JsonEvent.ValueNumber:
                case JsonEvent.ValueBool:
                case JsonEvent.ValueNull:
                case JsonEvent.ObjectStart:
                case JsonEvent.ArrayStart:
                    return true;
                case JsonEvent.ArrayEnd:
                    break;
                case JsonEvent.ObjectEnd:
                    throw new InvalidOperationException("unexpected ObjectEnd in NoSkipNextArrayElement()");
            }
            return false;
        }
    }
}