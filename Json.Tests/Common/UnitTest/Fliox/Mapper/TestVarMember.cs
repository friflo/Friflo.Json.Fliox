// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox;
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
    [NamingPolicy(NamingPolicyType.Default)]
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
        
        [Test]
        public static void TestVarMember_PerfCopyRef()
        {
            var mi          = typeof(TestMemberClass).GetField(nameof(TestMemberClass.int32));
            var getter      = DelegateUtils.CreateMemberGetter<TestMemberClass, int>(mi);
            var setter      = DelegateUtils.CreateMemberSetter<TestMemberClass, int>(mi);
            var source      = new TestMemberClass { int32 = 123 };
            var target      = new TestMemberClass();

            for (long n = 0; n < Count; n++) {
                setter(target, getter(source));
                // target.int32 = source.int32;
            }
        }
        
        private const long PostCount = 1; // 100_000_000L;
        
        [Test]
        public static void TestVarMember_PerfPostCopy()
        {
            var mapper      = GetMapper<Post>();
            var fields      = mapper.PropFields.fields;
            var source      = new Post();
            var target      = new Post();

            for (long n = 0; n < PostCount; n++) {
                foreach (var field in fields) {
                    field.member.Copy(source, target);
                }
            }
        }
        
        private static readonly Var[] PostVars =  new Var[] {
            new Var(1),
            new Var("abc"),
            new Var(DateTime.Now),
            new Var(DateTime.Now),
            new Var((int?)null),
            new Var((int?)null),
            new Var((int?)null),
            new Var((int?)null),
            new Var((int?)null),
            new Var((int?)null),
            new Var((int?)null),
            new Var((int?)null),
            new Var((int?)null),
        };

        [Test]
        public static void TestVarMember_PerfPostSetVar()
        {
            var mapper      = GetMapper<Post>();
            var fields      = mapper.PropFields.fields;
            var target      = new Post();
            for (long n = 0; n < PostCount; n++) {
                for (int i = 0; i < fields.Length; i++) {
                    fields[i].member.SetVar(target, PostVars[i]);
                }
            }
        }
        
        [Test]
        public static void TestVarMember_PerfPostReadVars()
        {
            var target      = new Post();
            for (long n = 0; n < PostCount; n++) {
                ReadVars(target, PostVars);
            }
        }
        
        private static void ReadVars(Post obj, Var[] vars)
        {
            obj.Id                  = vars[0].Int32;
            obj.Text                = vars[1].String;
            obj.CreationDate        = vars[2].DateTime;
            obj.LastChangeDate      = vars[3].DateTime;
            obj.Counter1            = vars[4].Int32Null;
            obj.Counter2            = vars[5].Int32Null;
            obj.Counter3            = vars[6].Int32Null;
            obj.Counter4            = vars[7].Int32Null;
            obj.Counter5            = vars[8].Int32Null;
            obj.Counter6            = vars[9].Int32Null;
            obj.Counter7            = vars[10].Int32Null;
            obj.Counter8            = vars[11].Int32Null;
            obj.Counter9            = vars[12].Int32Null;
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