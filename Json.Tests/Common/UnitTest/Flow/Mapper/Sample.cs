// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Numerics;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Mapper
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

    struct ChildStructIL
    {
        public int val2;
    }
    
    struct StructIL
    {
        public int              structInt;
        public ChildStructIL?   child1;
        public ChildStructIL?   child2;

        public ChildIL          childClass1;
        public ChildIL          childClass2;

        public void Init() {
            structInt = 200;
            child1 = new ChildStructIL {val2 = 201};
            child2 = null;
            childClass1 = null;
            childClass2 = new ChildIL();
            childClass2.val = 202;
        }
    }
    
    class SampleIL
    {
        public EnumIL   enumIL1;
        public EnumIL?  enumIL2;
            
        public ChildStructIL?childStructNull1;
        public ChildStructIL?childStructNull2;

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
        
        public ChildStructIL childStruct1;
        public ChildStructIL childStruct2;
        
        public ChildIL  child;
        public ChildIL  childNull;
        public StructIL structIL;  // after child & childNull (to have class type before)

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
                
            childStructNull1 = new ChildStructIL {val2 = 68};
            childStructNull2 = new ChildStructIL {val2 = 69};
            
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
            child =     null;
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
            childStructNull2 = new ChildStructIL {val2 = 19};

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
            
            childStruct1.val2 = 111;
            childStruct2.val2 = 112;
            
            child = new ChildIL { val = 42 };
            childNull = null;
            structIL.Init();
            
            dbl   = 22.5d;
            flt   = 33.5f;
            
            int64 = 10;
            int32 = 11;
            int16 = 12;
            int8  = 13;
            bln   = true;
        }
    }
}
