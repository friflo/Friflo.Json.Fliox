using Friflo.Json.Flow.Transform;
using NUnit.Framework;
using static NUnit.Framework.Assert;


namespace Friflo.Json.Tests.Common.UnitTest.Flow.Transform
{
    public class TestScalar
    {
        [Test]
        public void TestScalars() {
            var str = new Scalar("hello");
            AreEqual("hello", str.AsString());
            AreEqual(ScalarType.String, str.type);
            
            var dbl = new Scalar(1.5);
            AreEqual(1.5, dbl.AsDouble());

            var lng = new Scalar(2);
            AreEqual(2, lng.AsLong());
            
            var bln = new Scalar(true);
            AreEqual(true, bln.AsBool());

            var undef = new Scalar();
            AreEqual(ScalarType.Undefined, undef.type);

            var dbl2 = new Scalar(2.0);
            
            var lng2 = new Scalar(2);
            
            AreEqual(0, lng2.CompareTo(lng2));
            AreEqual(0, dbl2.CompareTo(dbl2));
            AreEqual(0, dbl2.CompareTo(lng2));
            AreEqual(0, lng2.CompareTo(dbl2));
            
            var @true  = new Scalar(true);
            var @false = new Scalar(false);
            AreEqual( 0, @true.CompareTo(@true));
            AreEqual( 1, @true.CompareTo(@false));
            AreEqual(-1, @false.CompareTo(@true));
        }
    }
}