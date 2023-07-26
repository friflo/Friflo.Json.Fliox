// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Collections;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Collection
{
    public static class TestListOne
    {
        [Test]
        public static void TestListOne_Add() {
            var list = new ListOne<int>();
            AreEqual(0, list.Count);
            AreEqual(1, list.Capacity);

            list.Add(20);
            AreEqual(1, list.Count);
            AreEqual(1, list.Capacity);
            AreEqual(20, list[0]);
            
            list.Add(21);
            AreEqual(2, list.Count);
            AreEqual(4, list.Capacity);
            AreEqual(20, list[0]);
            AreEqual(21, list[1]);
        }
    }
}