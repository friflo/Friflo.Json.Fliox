// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Utils;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    public class TestMemberClass {
        public DateTime     DateTime    { get; set; }
        public DateTime     dateTime;
        public Guid         Guid        { get; set; }
        public Guid         guid;
        public long         Lng         { get; set; }
        public long         lng;
        public int          Int32       { get; set; }
        public int          int32;
        public MemberStruct struct16;
    }
    
    public struct MemberStruct {
        public Guid     guid1;
        public Guid     guid2;
    }

    
    public static class TestVarMember
    {
        private const long Count = 1; // 1_000_000_000L;
        
        [Test]
        public static void TestVarMember_PerfGetVar()
        {
            var mapper      = GetMapper<TestMemberClass>();
            var instance    = new TestMemberClass { int32 = 123 };
            var member      = mapper.GetMember("int32");
            var start       = Mem.GetAllocatedBytes();
            for (long n = 0; n < Count; n++) {
                _ = member.GetVar(instance).Int32;
                // _ = instance.int32;
            }
            var diff = Mem.GetAllocationDiff(start);
            AreEqual(0, diff);
        }
        
        [Test]
        public static void TestVarMember_PerfDelegate()
        {
            var mi          = typeof(TestMemberClass).GetField(nameof(TestMemberClass.int32));
            var getter      = DelegateUtils.CreateMemberGetter<TestMemberClass, int>(mi);
            var instance    = new TestMemberClass { int32 = 123 };

            for (long n = 0; n < Count; n++) {
                var _ = getter(instance);
                // var _ = new Var(getter(instance));
            }
        }
        
        // [Test]
        public static void TestVarMember_PerfCopy()
        {
            var mapper      = GetMapper<Post>();
            var fields      = mapper.PropFields.fields;
            var source      = new Post();
            var target      = new Post();

            for (long n = 0; n < 10_000_000L; n++) {
                foreach (var field in fields) {
                    field.member.Copy(source, target);
                }
            }
        }
        
        // [Test]
        public static void TestVarMember_PerfCopyRef()
        {
            var mi          = typeof(TestMemberClass).GetField(nameof(TestMemberClass.int32));
            var getter      = DelegateUtils.CreateMemberGetter<TestMemberClass, int>(mi);
            var setter      = DelegateUtils.CreateMemberSetter<TestMemberClass, int>(mi);
            var source      = new TestMemberClass { int32 = 123 };
            var target      = new TestMemberClass();

            for (long n = 0; n < 1_000_000_000L; n++) {
                setter(target, getter(source));
                // target.int32 = source.int32;
            }
        }
        
        private static TypeMapper<T> GetMapper<T>() {
            var typeStore   = new TypeStore();
            return typeStore.GetTypeMapper<T>();
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