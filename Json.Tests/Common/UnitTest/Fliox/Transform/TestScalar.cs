// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
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
            // --- numeric
            CompareTo( 0,       ScalarType.Undefined,   lng2,     lng2);
            CompareTo( 0,       ScalarType.Undefined,   dbl2,     dbl2);
            CompareTo( 0,       ScalarType.Undefined,   dbl2,     lng2);
            CompareTo( 0,       ScalarType.Undefined,   lng2,     dbl2);
            
            CompareTo (0,       ScalarType.Null,        Scalar.Null,    lng);
            CompareTo (0,       ScalarType.Null,        lng,            Scalar.Null);
            CompareTo (0,       ScalarType.Null,        Scalar.Null,    dbl);
            CompareTo (0,       ScalarType.Null,        dbl,            Scalar.Null);

            // --- string
            CompareTo( 0,       ScalarType.Undefined,   str,            str);
            CompareTo( 0,       ScalarType.Null,        Scalar.Null,    str);
            CompareTo( 0,       ScalarType.Null,        str,            Scalar.Null);
            
            // --- bool
            var @true  = new Scalar(true);
            var @false = new Scalar(false);
            CompareTo( 0,       ScalarType.Undefined,   @true,          @true);
            CompareTo( 1,       ScalarType.Undefined,   @true,          @false);
            CompareTo(-1,       ScalarType.Undefined,   @false,         @true);
            CompareTo( 0,       ScalarType.Null,        Scalar.Null,    @true);
            CompareTo( 0,       ScalarType.Null,        @true,          Scalar.Null);
            
            // --- null
            CompareTo (0,       ScalarType.Undefined,   Scalar.Null,    Scalar.Null);
        }
        
        private static void CompareTo(object expectReturn, object expectResult, Scalar left, Scalar right) {
            var compare = left.CompareTo(right, null, out Scalar result);
            AreEqual(expectResult, result.type);
            AreEqual(expectReturn, compare);
        }
        
        [Test]
        public void TestScalarEqualsTo() {
            var str  = new Scalar("hello");
            var str2 = new Scalar("hello2");
            
            AssertIsTrue (str.EqualsTo(str, null));
            AssertIsFalse(str2.EqualsTo(str, null));

            var dbl1 = new Scalar(1.0);
            var lng1 = new Scalar(1);
            var dbl2 = new Scalar(2.0);
            var lng2 = new Scalar(2);
            
            AssertIsFalse (lng1.EqualsTo(lng2, null));
            AssertIsFalse (dbl1.EqualsTo(lng2, null));
            AssertIsFalse (lng1.EqualsTo(dbl2, null));
            AssertIsFalse (dbl1.EqualsTo(dbl2, null));

            AssertIsTrue (lng2.EqualsTo(lng2, null));
            AssertIsTrue (dbl2.EqualsTo(dbl2, null));
            AssertIsTrue (dbl2.EqualsTo(lng2, null));
            AssertIsTrue (lng2.EqualsTo(dbl2, null));
            
            var t   = new Scalar(true);
            var f   = new Scalar(false);
            AssertIsTrue (t.EqualsTo(t, null));
            AssertIsFalse(t.EqualsTo(f, null));
            AssertIsFalse(f.EqualsTo(t, null));
            
            AreEqual("incompatible operands: 1 with 'hello'",       lng1.EqualsTo(str,  null).ErrorMessage);
            AreEqual("incompatible operands: 1 with true",          lng1.EqualsTo(t,    null).ErrorMessage);
            
            AreEqual("incompatible operands: 1 with 'hello'",       dbl1.EqualsTo(str,  null).ErrorMessage);
            AreEqual("incompatible operands: 1 with true",          dbl1.EqualsTo(t,    null).ErrorMessage);
            
            AreEqual("incompatible operands: true with 1",          t.EqualsTo(lng1,    null).ErrorMessage);
            AreEqual("incompatible operands: true with 'hello'",    t.EqualsTo(str,     null).ErrorMessage);

            AreEqual("incompatible operands: 'hello' with true",    str.EqualsTo(t,     null).ErrorMessage);
            AreEqual("incompatible operands: 'hello' with 1",       str.EqualsTo(lng1,  null).ErrorMessage);
        }
        
        [Test]
        public void TestScalarString() {
            var str1 = new Scalar("abc");
            var str2 = new Scalar("xyz");
            AssertIsFalse(str1.         Contains(str2,           null));
            AssertIsTrue (str1.         Contains(str1,           null));
            AssertIsNull (str1.         Contains(Scalar.Null,    null));
            AssertIsNull (Scalar.Null.  Contains(str1,    null));
            
            AssertIsFalse(str1.         StartsWith(str2,         null));
            AssertIsFalse(str1.         EndsWith  (str2,         null));
        }
        
        [Test]
        public void TestScalarNumberUnary() {
            var num1    = new Scalar(1);
            var dbl1    = new Scalar(1.0);
            var e       = new Scalar(Math.E);

            AreEqual (1,  num1.         Abs(null)       .AsLong());
            AreEqual (1,  dbl1.         Abs(null)       .AsDouble());
            
            AreEqual (1,  num1.         Ceiling(null)   .AsLong());
            AreEqual (1,  dbl1.         Ceiling(null)   .AsDouble());
            
            AreEqual (1,  num1.         Floor(null)     .AsLong());
            AreEqual (1,  dbl1.         Floor(null)     .AsDouble());
            
            AreEqual (1,  num1.         Sqrt(null)      .AsDouble());
            AreEqual (1,  dbl1.         Sqrt(null)      .AsDouble());

            AreEqual (Math.E,  num1.    Exp(null)       .AsDouble());
            AreEqual (Math.E,  dbl1.    Exp(null)       .AsDouble());
            
            AreEqual (1,  e.            Log(null)       .AsDouble());
            AreEqual (0,  num1.         Log(null)       .AsDouble());


        }
        
        [Test]
        public void TestScalarNumberBinary() {
            var num1 = new Scalar(1);
            var num2 = new Scalar(2);
            var dbl1 = new Scalar(1.0);

            AreEqual (3,  num1.         Add(num2,           null).AsLong());
            AreEqual (2,  num1.         Add(num1,           null).AsLong());
            AssertIsNull (num1.         Add(Scalar.Null,    null));
            AssertIsNull (Scalar.Null.  Add(num1,           null));
            
            AreEqual (0,  dbl1.         Subtract(dbl1,      null).AsDouble());
            AreEqual (0,  num1.         Subtract(dbl1,      null).AsDouble());
            AreEqual (0,  dbl1.         Subtract(num1,      null).AsDouble());
            
            AreEqual (2,  dbl1.         Add(dbl1,           null).AsDouble());
            AreEqual (2,  num1.         Add(dbl1,           null).AsDouble());
            AreEqual (2,  dbl1.         Add(num1,           null).AsDouble());
            
            AreEqual (1,  dbl1.         Multiply(dbl1,      null).AsDouble());
            AreEqual (1,  num1.         Multiply(dbl1,      null).AsDouble());
            AreEqual (1,  dbl1.         Multiply(num1,      null).AsDouble());

            AreEqual (1,  dbl1.         Divide(dbl1,        null).AsDouble());
            AreEqual (1,  num1.         Divide(dbl1,        null).AsDouble());
            AreEqual (1,  dbl1.         Divide(num1,        null).AsDouble());
        }
        
        private static void AssertIsTrue(Scalar value) {
            IsTrue(value.IsTrue);
        }
        
        private static void AssertIsFalse(Scalar value) {
            IsFalse(value.IsTrue);
        }
        
        private static void AssertIsNull(Scalar value) {
            IsTrue(value.IsNull);
        }

    }
}