using System;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.Utils
{
    public class TestException : Exception {
        public TestException(string msg) : base (msg) { }
    }
    
    public static class SimpleAssert
    {
        
        public static void AreEqual(object expect, object value) {
            if (expect == null) {
                if (value == null)
                    return;
            } else {
                if (expect.Equals(value))
                    return;
            }
            throw new TestException("Expect: " + expect + "\nBut was: " + value);
        }
        
        public static void AreEqual(long expect, long value) {
            if (expect != value)
                throw new TestException("Expect: " + expect + "\nBut was: " + value);
        }
        
        public static void AreEqual(double expect, double value) {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (expect != value)
                throw new TestException("Expect: " + expect + "\nBut was: " + value);
        }
        
        public static void AreEqual(bool expect, bool value) {
            if (expect != value)
                throw new TestException("Expect: " + expect + "\nBut was: " + value);
        }


        public static void Fail(string msg) {
            throw new TestException("Test failed. " + msg);
        }

        public static void IsTrue(bool value) {
            if (!value)
                throw new TestException("Expect: true\nBut was: false");
        }
        
        public static void IsFalse(bool value) {
            if (value)
                throw new TestException("Expect: false\nBut was: true");
        }
        

        public static TActual Throws<TActual>(TestDelegate code) where TActual : Exception {
            try {
                code();
            } catch (Exception e) {
                return (TActual)e;
            }
            return null;
        }
    }
}