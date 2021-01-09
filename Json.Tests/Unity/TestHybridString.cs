#if UNITY_5_3_OR_NEWER && JSON_BURST

using System;
using NUnit.Framework;
using Unity.Collections;

namespace Friflo.Json.Tests.Unity
{

    public struct HybridString
    {
        public String Value;

        public HybridString(String str) {
            Value = str;
        }
    }
    
    public struct HybridNativeString
    {
        public FixedString128 Value;

        public HybridNativeString(FixedString128 str) {
            Value = str;
        }
    }


    public class TestHybridString
    {
        [Test]
        public void TestInstantiation() {
            HybridString        hybrid =        new HybridString        ("ABC");
            HybridNativeString  hybridNative =  new HybridNativeString  ("ABC");
            Assert.AreEqual(hybrid.Value, hybridNative.Value.ToString());
        }
    }
        
}

#endif // UNITY_5_3_OR_NEWER
