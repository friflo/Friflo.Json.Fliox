using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Burst
{
    public class TestBytes32 : LeakTestsFixture
    {
        [Test]
        public void TestBytes32Assignment() {
            var bytes32 = new Bytes32();
            
            var str0 = new Bytes("");
            bytes32.FromBytes(ref str0);
            str0.Dispose();
            
            var str1 = new Bytes("1");
            bytes32.FromBytes(ref str1);
            str1.Dispose();
            
            var str8 = new Bytes("12345678");
            bytes32.FromBytes(ref str8);
            str8.Dispose();
                
            var src = new Bytes("");
            var dst = new Bytes("");

            var builder = new StringBuilder();
            for (int n = 0; n <= 32; n++) {
                var refStr = builder.ToString();
                src.Clear();
                src.FromString(refStr);
                bytes32.FromBytes(ref src);
                bytes32.ToBytes(ref dst);
                
                AreEqual(refStr, dst.ToString());
                
                builder.Append((char)('@' + n));
            }
            
            src.Dispose();
            dst.Dispose();
        }
    }

}
