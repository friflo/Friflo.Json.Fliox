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
    }
}