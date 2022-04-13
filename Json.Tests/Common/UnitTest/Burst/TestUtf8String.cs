// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Burst
{
    public class TestUtf8String : LeakTestsFixture
    {
        [Test]
        public void Utf8String() {
            var count   = 100;
            var strings = new List<Utf8String>(count);
            var buffer  = new Utf8Buffer();
            for (int n = 0; n < count; n++) {
                var str = buffer.Add("xxx"); 
                strings.Add(str);
            }
            var fooUtf8 = buffer.Add("foo"); 
            strings.Add(fooUtf8);
            
            var foo = new Bytes("foo");
            for (int n = 0; n <= count; n++) {
                var str = strings[n];    
                if (str.IsEqual(ref foo)) {
                    AreEqual(count, n);
                    foo.Dispose();
                    return;
                }
            }
            Fail("unexpected");
        }
    }
}