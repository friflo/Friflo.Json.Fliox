using System;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.Utils
{
    public static class NoCheck
    {
        public static bool checkStaticMemoryUsage = false;
        
        public static void AreEqual(object expect, object value) {
            if (!expect.Equals(value))
                throw new InvalidOperationException("Expect: " + expect + "\nBut was: " + value);
        }
        
        public static void AreEqual(long expect, long value) {
            if (expect != value)
                throw new InvalidOperationException("Expect: " + expect + "\nBut was: " + value);
        }

        public static void Fail(string msg) {
            throw new InvalidOperationException("Test failed. " + msg);
        }

        public static void IsTrue(bool value) {
            if (!value)
                throw new InvalidOperationException("Expect: true\nBut was: false");
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