// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;

// ReSharper disable UnusedMember.Local
// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    public class TestMemberClass2 {
        public DateTime DateTime { get; set; }
    }
    
    public class TestMemberClass {
        public DateTime DateTime    { get; set; }
        public DateTime dateTime;
        public Guid     Guid        { get; set; }
        public Guid     guid;
        public long     Lng         { get; set; }
        public long     lng;
        public int      Int32       { get; set; }
        public int      int32;
        
        public MemberStruct      struct16;
    }
    
    public struct MemberStruct {
        public Guid     guid1;
        public Guid     guid2;
    }
    

    
    public static class TestVarMember
    {
        private const long Count = 1; // 1_000_000_000L;
        
        [Test]
        public static void TestVarMember_Get() {
            
            var typeStore = new TypeStore();
            var mapper = typeStore.GetTypeMapper<TestMemberClass2>();
            var instance = new TestMemberClass2 { DateTime = DateTime.Now };
            
            
            var member = mapper.GetMember("DateTime");
            var result = 0;
            for (long n = 0; n < Count; n++) {
                // result ^= instance.val;
                var _ = member.GetVar(instance).DateTime;
                // var _ = instance.val;
            }
            Console.WriteLine(result);
        }
        
        // -- getter / setter used to explore generate IL code
        private static DateTime IL_DateTime     (TestMemberClass instance)              => instance.DateTime;
        private static Guid     IL_Guid         (TestMemberClass instance)              => instance.Guid;
        private static void     IL_guidSet      (TestMemberClass instance, Guid value)  => instance.guid = value;
        private static void     IL_struct16Set  (TestMemberClass instance, MemberStruct value)  => instance.struct16 = value;
        private static long     IL_Lng          (TestMemberClass instance)              => instance.Lng;
        private static long     IL_lngGet       (TestMemberClass instance)              => instance.lng;
        private static void     IL_lngSet       (TestMemberClass instance, long value)  => instance.lng = value;
        private static int      IL_Int32        (TestMemberClass instance)              => instance.Int32;
        private static int      IL_int32Get     (TestMemberClass instance)              => instance.int32;
        private static void     IL_int32Set     (TestMemberClass instance, int value)   => instance.int32 = value;
    }
}