// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Burst
{
    public class TestBytes32 : LeakTestsFixture
    {
        [Test]
        public void TestBytes32Assignment() {
            var bytes32 = new Bytes32();
            
            var str0 = new Bytes("", Untracked.Bytes);
            bytes32.FromBytes(str0);
            str0.Dispose(Untracked.Bytes);
            
            var str1 = new Bytes("1", Untracked.Bytes);
            bytes32.FromBytes(str1);
            str1.Dispose(Untracked.Bytes);
            
            var str8 = new Bytes("12345678", Untracked.Bytes);
            bytes32.FromBytes(str8);
            str8.Dispose(Untracked.Bytes);
                
            var src = new Bytes("", Untracked.Bytes);
            var dst = new Bytes("", Untracked.Bytes);

            /* var builder = new StringBuilder();
            for (int n = 0; n <= 32; n++) {
                var refStr = builder.ToString();
                src.Clear();
                src.FromString(refStr);
                bytes32.FromBytes(ref src);
                bytes32.ToBytes(ref dst);
                
                AreEqual(refStr, dst.AsString());
                
                builder.Append((char)('@' + n));
            } */
            
            src.Clear();
            // for (int n = 0; n <= 50_000_000; n++)
            //     bytes32.FromBytes(ref src);
            
            // for (int n = 0; n <= 300_000_000; n++)
            //     bytes32.IsEqual(ref bytes32);
            
            
            src.Dispose(Untracked.Bytes);
            dst.Dispose(Untracked.Bytes);
        }
    }

}
