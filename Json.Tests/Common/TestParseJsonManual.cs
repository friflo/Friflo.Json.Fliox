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


namespace Friflo.Json.Tests.Common
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
            JsonEvent ev;
            do {
                ev = p.NextEvent();
                if      (ev == JsonEvent.ValueNumber)   { this[index++] = p.ValueAsInt(out _); }
                else                                    { p.SkipEvent(ev); }
            } while (p.ContinueArray(ev));
        }
        
        public void Read2 (ref JsonParser p) {
            int index = 0;
            var arr = new ReadArray();
            while (arr.NextEvent(ref p)) {
                if (arr.IsNum(ref p))       { this[index++] = p.ValueAsInt(out _); }
            }
        }
    }
    
    public class TestParseJsonManual : ECSLeakTestsFixture
    {
        private static void AssertParseResult(ref ParseManual manual, ref JsonParser p) {
            if (p.error.ErrSet) {
                Fail(p.error.Msg.ToString());
            }
            else {
                AreEqual(JsonEvent.EOF, p.NextEvent());
                AreEqual(JsonEvent.Error, p.NextEvent()); // check iteration after EOF
            }
            AreEqual(1,             manual.int3.x);
            AreEqual(2,             manual.int3.y);
            AreEqual(3,             manual.int3.z);
            AreEqual(64,            manual.i64);
            AreEqual("string-ý",    manual.str.ToString());
            AreEqual(true,          manual.t);
            AreEqual(true,          manual.foundNull);
            AreEqual("str0",        manual.strElement.ToString());
            AreEqual(true,          manual.foundNullElement);
            AreEqual(true,          manual.trueElement);
            //
            AreEqual(8,     p.SkipInfo.arrays);
            AreEqual(1,     p.SkipInfo.booleans);
            AreEqual(1,     p.SkipInfo.floats);
            AreEqual(34,    p.SkipInfo.integers);
            AreEqual(15,    p.SkipInfo.nulls);
            AreEqual(37,    p.SkipInfo.objects);
            AreEqual(4,     p.SkipInfo.strings);
            AreEqual(100,   p.SkipInfo.Sum);
        }
        
        [Test]
        public void ParseJsonManual() {
            using (Bytes bytes = CommonUtils.FromFile("assets/codec/complex.json")) {
                var parser = new JsonParser();
                {
                    ParseManual manual = new ParseManual(Default.Constructor);
                    parser.InitParser(bytes);
                    parser.NextEvent();
                    manual.Root1(ref parser);
                    AssertParseResult(ref manual, ref parser);
                    manual.Dispose();
                } {
                    ParseManual manual = new ParseManual(Default.Constructor);
                    parser.InitParser(bytes);
                    parser.NextEvent();
                    manual.Root2(ref parser);
                    AssertParseResult(ref manual, ref parser);
                    manual.Dispose();
                } {
                    ParseManual manual = new ParseManual(Default.Constructor);
                    for (int i = 0; i < 1; i++) {
                        parser.InitParser(bytes);
                        parser.NextEvent();
                        manual.Root3(ref parser);
                    }
                    AssertParseResult(ref manual, ref parser);
                    manual.Dispose();
                }
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
            public Str32 boolArr;
            public Str32 i64Arr; 
            public Str32 i64;  
            public Str32 str;    
            public Str32 t;     
            public Str32 n;

            public Names(Default _) {
                map =       "map";
                map2 =      "map2";
                listStr =   "listStr";
                boolArr =   "boolArr";
                i64Arr =    "i64Arr";
                i64 =       "i64";
                str =       "str";
                t =         "t";
                n =         "n";
            }
        }
        
        public struct ParseManual : IDisposable
        {
            public  Int3    int3;
            public  long    i64;
            public  Bytes   str;
            public  bool    t;
            public  bool    foundNull;
            public  Bytes   strElement;
            public  bool    foundNullElement;
            public  bool    trueElement;
            private Names   nm;
            private Bytes   temp;

            public ParseManual(Default _) {
                int3 = new Int3();
                temp = new Bytes(32);
                i64 = 0;
                t = false;
                foundNull = false;
                strElement = new Bytes(16);
                str = new Bytes(16);
                foundNullElement = false;
                trueElement = false;
                nm = new Names(Default.Constructor);
            }
            
            public void Dispose() {
                temp.Dispose();
                strElement.Dispose();
                str.Dispose();
            }

            public void Root1(ref JsonParser p) {
                ref var key = ref p.key;
                JsonEvent ev;
                do {
                    ev = p.NextEvent();
                    if      (key.IsEqual32(ref nm.map)      && ev == JsonEvent.ObjectStart) { p.SkipTree(); }
                    else if (key.IsEqual32(ref nm.map2)     && ev == JsonEvent.ObjectStart) { p.SkipTree(); }
                    else if (key.IsEqual32(ref nm.listStr)  && ev == JsonEvent.ArrayStart)  { ReadListStr(ref p); }
                    else if (key.IsEqual32(ref nm.boolArr)  && ev == JsonEvent.ArrayStart)  { ReadBoolArr(ref p); }
                    else if (key.IsEqual32(ref nm.i64Arr)   && ev == JsonEvent.ArrayStart)  { int3.Read(ref p); }
                    else if (key.IsEqual32(ref nm.i64)      && ev == JsonEvent.ValueNumber) { i64 = p.ValueAsInt(out _); }
                    else if (key.IsEqual32(ref nm.str)      && ev == JsonEvent.ValueString) { str.Set(ref p.value); }
                    else if (key.IsEqual32(ref nm.t)        && ev == JsonEvent.ValueBool)   { t = p.boolValue; }
                    else if (key.IsEqual32(ref nm.n)        && ev == JsonEvent.ValueNull)   { foundNull = true; }
                    else                                                                    { p.SkipEvent(ev); }
                } while(p.ContinueObject(ev));
            }
            
            public void Root2(ref JsonParser p) {
                JsonEvent ev;
                do {
                    ev = p.NextEvent();
                    if      (p.IsObj(ev, ref nm.map))       { p.SkipTree(); }
                    else if (p.IsObj(ev, ref nm.map2))      { p.SkipTree(); }
                    else if (p.IsArr(ev, ref nm.listStr))   { ReadListStr(ref p); }
                    else if (p.IsArr(ev, ref nm.boolArr))   { ReadBoolArr(ref p); }
                    else if (p.IsArr(ev, ref nm.i64Arr))    { int3.Read(ref p); }
                    else if (p.IsNum(ev, ref nm.i64))       { i64 = p.ValueAsInt(out _); }
                    else if (p.IsStr(ev, ref nm.str))       { str.Set(ref p.value); }
                    else if (p.IsBln(ev, ref nm.t))         { t = p.boolValue; }
                    else if (p.IsNul(ev, ref nm.n))         { foundNull = true; }
                    else                                    { p.SkipEvent(ev); }
                } while(p.ContinueObject(ev));
            }
            
            public void Root3(ref JsonParser p) {
                var obj = new ReadObject();
                while (obj.NextEvent(ref p)) {
                    if      (obj.IsObj(ref p, ref nm.map))        { p.SkipTree(); }
                    else if (obj.IsObj(ref p, ref nm.map2))       { p.SkipTree(); }
                    else if (obj.IsArr(ref p, ref nm.listStr))    { ReadListStr2(ref p); }
                    else if (obj.IsArr(ref p, ref nm.boolArr))    { ReadBoolArr2(ref p); }
                    else if (obj.IsArr(ref p, ref nm.i64Arr))     { int3.Read2(ref p); }
                    else if (obj.IsNum(ref p, ref nm.i64))        { i64 = p.ValueAsInt(out _); }
                    else if (obj.IsStr(ref p, ref nm.str))        { str.Set(ref p.value); }
                    else if (obj.IsBln(ref p, ref nm.t))          { t = p.boolValue; }
                    else if (obj.IsNul(ref p, ref nm.n))          { foundNull = true; }
                }
            }
	        
            void ReadListStr(ref JsonParser p) {
                JsonEvent ev;
                do {
                    ev = p.NextEvent();
                    if      (ev == JsonEvent.ValueString)   { strElement.Set( ref p.value); }
                    else                                    { p.SkipEvent(ev); }
                } while (p.ContinueArray(ev));
            }
            
            void ReadListStr2(ref JsonParser p) {
                var arr = new ReadArray();
                while (arr.NextEvent(ref p)) {
                    if      (arr.IsStr(ref p))              { strElement.Set( ref p.value); }
                }
            }
            
            void ReadBoolArr(ref JsonParser p) {
                JsonEvent ev;
                do {
                    ev = p.NextEvent();
                    if      (ev == JsonEvent.ValueBool)     { trueElement = p.boolValue; }
                    else if (ev == JsonEvent.ValueNull)     { foundNullElement = true; }
                    else                                    { p.SkipEvent(ev); }
                } while (p.ContinueArray(ev));
            }
            
            void ReadBoolArr2(ref JsonParser p) {
                var arr = new ReadArray();
                while (arr.NextEvent(ref p)) {
                    if      (arr.IsBln(ref p))              { trueElement = p.boolValue; }
                    else if (arr.IsNul(ref p))              { foundNullElement = true; }
                }
            }
        }
    }
}