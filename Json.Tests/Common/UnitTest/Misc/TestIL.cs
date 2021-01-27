using System;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Misc
{
    public class TestIL
    {
        [Test]
        public void DoubleToLong() {
            double dbl64 = 123.456;
            long   lng          = BitConverter.DoubleToInt64Bits(dbl64);
            double dblResult    = BitConverter.Int64BitsToDouble(lng);
            AreEqual(dbl64, dblResult);
            
            float  flt32 = 123.456f;
            int integer        = BitConverter.SingleToInt32Bits(flt32);
            float result32     = BitConverter.Int32BitsToSingle(integer);
            AreEqual(flt32, result32);

        }
        
        [Test]
        public void CodeGenPatternStoreLoad() {
            // --- store
            long[]  src = { 0, 1, 2, 3, 1, 5, 6 };
            
            long    field0 =            src[0];
            int     field1 = (int)      src[1];
            short   field2 = (short)    src[2];
            byte    field3 = (byte)     src[3];
            
            bool    field4 =            src[4] != 0;
            
            double  field5 = BitConverter.Int64BitsToDouble (     src[5]);
            float   field6 = BitConverter.Int32BitsToSingle ((int)src[6]);

            // --- load
            long[] dst = new long[7];

            dst[0] = field0;
            dst[1] = field1;
            dst[2] = field2;
            dst[3] = field3;
            
            dst[4] = field4 ? 1 : 0;

            dst[5] = BitConverter.DoubleToInt64Bits(field5);
            dst[6] = BitConverter.SingleToInt32Bits(field6);

            AreEqual(src, dst);
        }
    }
}