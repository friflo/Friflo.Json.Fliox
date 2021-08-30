// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Linq;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;
#pragma warning disable 649 // Field 'field' is never assigned

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    public class TestMemberNull : LeakTestsFixture
    {
        enum EnumNull {
        }
        
        struct StructNull {
        }
        
        class Child {
        }
        
        class TestNull
        {
            public int?         int32;
            public Child        child;
            public StructNull?  nullableStruct;
            public EnumNull?    nullableEnum;
        }
        
        [Test] public void WriteNullReflect()   { WriteNull(TypeAccess.Reflection); }
        [Test] public void WriteNullIL()        { WriteNull(TypeAccess.IL); }
        
        private void WriteNull(TypeAccess typeAccess) {
            string json = @"
            {
                ""int32"":          null,
                ""child"":          null,
                ""nullableStruct"": null,
                ""nullableEnum"":   null
            }";
            using (var typeStore = new TypeStore(new StoreConfig(typeAccess)))
            using (var m = new ObjectMapper(typeStore)) {
                var naming = m.Read<TestNull>(json);
                var result = m.Write(naming);
                string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));

                AreEqual(expect, result);
            }
        }
        
        [Test] public void OmitNullReflect()    { OmitNull(TypeAccess.Reflection); }
        [Test] public void OmitNullIL()         { OmitNull(TypeAccess.IL); }

        private void OmitNull(TypeAccess typeAccess) {
            string json = "{}";
            using (var typeStore = new TypeStore(new StoreConfig(typeAccess)))
            using (var m = new ObjectMapper(typeStore)) {
                m.WriteNullMembers = false;
                var naming = m.Read<TestNull>(json);
                var result = m.Write(naming);
                string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));

                AreEqual(expect, result);
            }
        }
    }
}