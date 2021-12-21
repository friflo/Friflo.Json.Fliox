// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Fliox.Transform;
using NUnit.Framework;
using static NUnit.Framework.Assert;


namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public class TestScalar
    {
        [Test]
        public void TestScalarCompareTo() {
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
            
            CompareTo( 0, lng2,     lng2);
            CompareTo( 0, dbl2,     dbl2);
            CompareTo( 0, dbl2,     lng2);
            CompareTo( 0, lng2,     dbl2);
            
            var @true  = new Scalar(true);
            var @false = new Scalar(false);
            CompareTo( 0, @true,    @true);
            CompareTo( 1, @true,    @false);
            CompareTo(-1, @false,   @true);
        }
        
        private static void CompareTo(int expect, Scalar left, Scalar right) {
            var compare = left.CompareTo(right, null, null, out Scalar result);
            AreEqual(ScalarType.Undefined, result.type);
            AreEqual(expect, compare);
        }
        
        [Test]
        public void TestScalarEqualsTo() {
            var str  = new Scalar("hello");
            var str2 = new Scalar("hello2");
            
            AssertIsTrue (str.EqualsTo(str));
            AssertIsFalse(str2.EqualsTo(str));

            var dbl1 = new Scalar(1.0);
            var lng1 = new Scalar(1);
            var dbl2 = new Scalar(2.0);
            var lng2 = new Scalar(2);
            
            AssertIsFalse (lng1.EqualsTo(lng2));
            AssertIsFalse (dbl1.EqualsTo(lng2));
            AssertIsFalse (lng1.EqualsTo(dbl2));
            AssertIsFalse (dbl1.EqualsTo(dbl2));

            AssertIsTrue (lng2.EqualsTo(lng2));
            AssertIsTrue (dbl2.EqualsTo(dbl2));
            AssertIsTrue (dbl2.EqualsTo(lng2));
            AssertIsTrue (lng2.EqualsTo(dbl2));
            
            var t   = new Scalar(true);
            var f   = new Scalar(false);
            AssertIsTrue (t.EqualsTo(t));
            AssertIsFalse(t.EqualsTo(f));
            AssertIsFalse(f.EqualsTo(t));
            
            AreEqual("Cannot compare 1 with 'hello'",       lng1.EqualsTo(str).ErrorMessage);
            AreEqual("Cannot compare 1 with true",          lng1.EqualsTo(t).ErrorMessage);
            
            AreEqual("Cannot compare 1 with 'hello'",       dbl1.EqualsTo(str).ErrorMessage);
            AreEqual("Cannot compare 1 with true",          dbl1.EqualsTo(t).ErrorMessage);
            
            AreEqual("Cannot compare true with 1",          t.EqualsTo(lng1).ErrorMessage);
            AreEqual("Cannot compare true with 'hello'",    t.EqualsTo(str).ErrorMessage);

            AreEqual("Cannot compare 'hello' with true",    str.EqualsTo(t).ErrorMessage);
            AreEqual("Cannot compare 'hello' with 1",       str.EqualsTo(lng1).ErrorMessage);
        }
        
        private static void AssertIsTrue(Scalar value) {
            IsTrue(value.IsBool);
            IsTrue(value.IsTrue);
        }
        
        private static void AssertIsFalse(Scalar value) {
            IsTrue(value.IsBool);
            IsFalse(value.IsTrue);
        }

    }
}