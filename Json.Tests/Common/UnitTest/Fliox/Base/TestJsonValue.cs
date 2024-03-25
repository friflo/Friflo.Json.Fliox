// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ClassNeverInstantiated.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Base
{
    public static class TestJsonValue
    {
        class JsonValueClass
        {
            public JsonValue value = default; // assign default to omit compiler warning
        }

        /// <summary>
        /// Test reusing <see cref="JsonValue"/> array for ReadTo() which utilize <see cref="JsonValue.Copy"/> 
        /// </summary>
        [Test]
        public static void TestJsonValueReadReuse() {
            var typeStore = new TypeStore();
            var mapper = new ObjectReader(typeStore);

            var test = mapper.Read<JsonValueClass>(@"{""value"":111}");
            AreEqual("111", test.value.AsString());
            var val1 = test.value;
            
            // value array is sufficient
            test = mapper.ReadTo(@"{""value"":222}", test, false);
            AreEqual("222", test.value.AsString());
            IsTrue(val1.IsEqualReference(test.value));
            
            // value array is not sufficient
            test = mapper.ReadTo(@"{""value"":3333}", test, false);
            AreEqual("3333", test.value.AsString());
            IsFalse(val1.IsEqualReference(test.value));
        }
        
        [Test]
        public static void TestJsonValueInit() {
            var value = new JsonValue(Encoding.UTF8.GetBytes("abc1234"), 3, 2);
            AreEqual("12", value.ToString());
        }
    }
}