using System.Linq;
using Friflo.Json.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;
#pragma warning disable 649 // Field 'field' is never assigned

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestMemberNull
    {
        class Child {
        }
        
        class TestNull
        {
            public Child    child;
            public int?     int32;
        }
        
        [Test]  public void WriteNullReflect()   { WriteNull(TypeAccess.Reflection); }
        [Test]  public void WriteNullClassIL()   { WriteNull(TypeAccess.IL); }
        
        private void WriteNull(TypeAccess typeAccess) {
            string json = @"
            {
                ""child"":   null,
                ""int32"":   null
            }";
            using (TypeStore typeStore = new TypeStore(new StoreConfig(typeAccess)))
            using (var m = new JsonMapper(typeStore)) {
                var naming = m.Read<TestNull>(json);
                var result = m.Write(naming);
                string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));

                AreEqual(expect, result);
            }
        }
        
        [Test]                  public void OmitNullReflect()   { OmitNull(TypeAccess.Reflection); }
        [Test]  [Ignore("")]    public void OmitNullClassIL()   { OmitNull(TypeAccess.IL); }

        private void OmitNull(TypeAccess typeAccess) {
            string json = "{}";
            using (TypeStore typeStore = new TypeStore(new StoreConfig(typeAccess)))
            using (var m = new JsonMapper(typeStore)) {
                m.WriteNullMembers = false;
                var naming = m.Read<TestNull>(json);
                var result = m.Write(naming);
                string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));

                AreEqual(expect, result);
            }
        }
    }
}