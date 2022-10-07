// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper.Map.Object.Reflect;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    internal class TestVarObject {
        public string name;
        public override string ToString() => name;
    }
    
    
    public class TestVar
    {
        [Test] public void  TestVarGetAndSet()
        {
            // --- object
            var testObj1 = new TestVarObject{ name = "testObj1"};
            var testObj2 = new TestVarObject{ name = "testObj2"};
            
            var obj1A   = new Var(testObj1);
            var obj1B   = new Var(testObj1);
            var obj2    = new Var(testObj2);
            var objNull = new Var((object)null);
            
            var str = objNull.ToString();
            
            
            IsFalse (obj2.IsNull);
            IsTrue  (objNull.IsNull);
            
            IsTrue  (obj1A == obj1B);
            IsTrue  (obj1A != obj2);
            
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
            
            IsFalse (long0.IsNull);
            IsTrue (longNull.IsNull);
            IsFalse (long1A.IsNull);
            
            IsTrue  (long1A == long1B);
            IsTrue  (long1A != long2);
            IsFalse (long1A == longNull);
        }
    }
}