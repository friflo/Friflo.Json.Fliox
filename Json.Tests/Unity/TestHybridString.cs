#if UNITY_5_3_OR_NEWER

using System;
using NUnit.Framework;
using Unity.Collections;

namespace Friflo.Json.Tests.Unity
{

    public struct HybridString
    {
        public String value;

        public HybridString(String str) {
            value = str;
        }
    }
    
    public struct HybridNativeString
    {
        public FixedString128 value;

        public HybridNativeString(FixedString128 str) {
            value = str;
        }
    }


    public class TestHybridString
    {
        [Test]
        public void testHybridString() {
            HybridString hybrid = new HybridString("ABC");
            
            HybridNativeString hybridNative = new HybridNativeString("ABC");
        }
    }
        
}

#endif // UNITY_5_3_OR_NEWER
