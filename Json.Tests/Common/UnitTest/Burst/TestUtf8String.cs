// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
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
            // Typical use case:
            //
            // 1. add strings once
            var foo = new Bytes("foo");
            var count   = 10;
            var strings = new List<Utf8String>(count);
            var buffer  = new Utf8Buffer();
            for (int n = 0; n < count; n++) {
                var str = buffer.Add("abc"); 
                strings.Add(str);
            }
            var fooUtf8 = buffer.Add("foo"); 
            strings.Add(fooUtf8);
            
            // 2. search a value in strings
            for (long i = 0; i < 1000; i++) {
                Search(strings, ref foo);
            }
            foo.Dispose();
        }
        
        private static void Search (List<Utf8String> strings, ref Bytes value) {
            var count   = strings.Count - 1;
            for (int n = 0; n <= count; n++) {
                var str = strings[n];    
                if (str.IsEqual(value)) {
                    if (n != count)
                        throw new InvalidOperationException("unexpected");
                    return;
                }
            }
            Fail("unexpected");
        }
    }
}