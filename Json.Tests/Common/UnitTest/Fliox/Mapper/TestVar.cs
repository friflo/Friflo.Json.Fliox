// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    internal class TestVarObject {
        public string name;
        public override string ToString() => name;
    }
    
    
    public static class TestVar
    {
        [Test]
        public static void TestVarGetAndSet()
        {
            // --- object
            var testObj1 = new TestVarObject{ name = "testObj1"};
            var testObj2 = new TestVarObject{ name = "testObj2"};
            
            var obj1A   = new Var(testObj1);
            var obj1B   = new Var(testObj1);
            var obj2    = new Var(testObj2);
            var objNull = new Var((object)null);
            
            IsFalse (obj2.IsNull);
            IsTrue  (objNull.IsNull);
            
            IsTrue  (obj1A == obj1B);
            IsTrue  (obj1A != obj2);
            AreEqual(testObj1, obj1A.Object);
            
            // --- string
            var abc1 = new string("abc");
            var abc2 = new string("abc");
            IsFalse(ReferenceEquals(abc1, abc2));
            
            var str1A   = new Var(abc1);
            var str1B   = new Var(abc2);
            var str2    = new Var("xyz");
            var strNull = new Var((string)null); 
            
            IsFalse (str1A.IsNull);
            IsTrue  (strNull.IsNull);

            IsTrue  (str1A == str1B);
            IsTrue  (str1A != str2);
            IsTrue  (str1A != strNull);
            
            // --- long
            var long0       = new Var(0);
            var long1A      = new Var(1);
            var long1B      = new Var(1);
            var long2       = new Var(2);
            var longNull    = new Var((long?)null);
            var longNull2   = new Var((long?)null);
            
            IsFalse (long0.IsNull);
            IsTrue (longNull.IsNull);
            IsFalse (long1A.IsNull);
            
            IsTrue  (long1A == long1B);
            IsTrue  (long1A != long2);
            IsTrue  (longNull == longNull2);
            
            // --- bool
            var boolTrue    = new Var(true);
            var boolFalse   = new Var(false);
            var boolNull    = new Var((bool?)null);
            
            IsFalse (boolTrue.IsNull);
            IsTrue  (boolNull.IsNull);
            IsFalse (boolFalse.IsNull);
            
            IsTrue  (boolTrue  == new Var(true));
            IsTrue  (boolNull  == new Var((bool?)null));
            IsFalse (boolTrue  == boolFalse);
            
            // --- char
            var charA       = new Var('a');
            var charB       = new Var('b');
            var charNull    = new Var((char?)null);
            
            IsFalse (charA.IsNull);
            IsTrue  (charNull.IsNull);
            
            IsFalse (charA  == charB);
            IsTrue  (charNull  == new Var((char?)null));
        }
        
        [Test]
        public static void  TestVarFromType() {
            var testObj = new TestVarObject{ name = "testObj1"};
            
            AreEqual("long",    VarFromValue(1).Name);
            AreEqual("double",  VarFromValue(1.1).Name);
            AreEqual("char",    VarFromValue('a').Name);
            AreEqual("bool",    VarFromValue(true).Name);
            
            AreEqual("long?",   VarFromValue((int?)1).Name);
            AreEqual("double?", VarFromValue((float?)1.1).Name);
            AreEqual("char?",   VarFromValue((char?)'a').Name);
            AreEqual("bool?",   VarFromValue((bool?)true).Name);
            
            AreEqual("string",  VarFromValue("test").Name);
            AreEqual("object",  VarFromValue(testObj).Name);
        }
        
        private static VarType  VarFromValue<T>(T val) {
            var type    = VarType.FromType(typeof(T));
            return type;
        }
        
        [Test]
        public static void  TestVarGet() {
            var testObj = new TestVarObject{ name = "testObj1"};
            IsTrue(testObj          == new Var(testObj).Object);
            IsTrue("Test"           == new Var("Test").String);
            
            IsTrue('a'              == new Var('a').Char);
            IsTrue(true             == new Var(true).Bool);

            IsTrue(255              == new Var(255).Int8);
            IsTrue(short.MaxValue   == new Var(short.MaxValue).Int16);
            IsTrue(int.MaxValue     == new Var(int.MaxValue).Int32);
            IsTrue(long.MaxValue    == new Var(long.MaxValue).Int64);
            
            IsTrue(float.MaxValue   == new Var(float.MaxValue).Flt32);
            IsTrue(double.MaxValue  == new Var(double.MaxValue).Flt64);
            
            // --- nullable
            IsTrue('a'              == new Var((char?)'a').CharNull);
            IsTrue(true             == new Var((bool?)true).BoolNull);

            IsTrue(255              == new Var((byte?)255).Int8Null);
            IsTrue(short.MaxValue   == new Var((short?)short.MaxValue).Int16Null);
            IsTrue(int.MaxValue     == new Var((int?)int.MaxValue).Int32Null);
            IsTrue(long.MaxValue    == new Var((long?)long.MaxValue).Int64Null);
            
            IsTrue(float.MaxValue   == new Var((float?)float.MaxValue).Flt32Null);
            IsTrue(double.MaxValue  == new Var((double?)double.MaxValue).Flt64Null);
        }
    }
}