using System.Linq;
using Friflo.Json.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestNaming
    {
        class Naming {
            public int lower;
            public int Upper;
        }
        
        [Test]
        public void CamelCaseTest() {
            string json = @"
            {
                ""lower"":   10,
                ""upper"":   11
            }";
            var m = new JsonMapper(new TypeStore(null, new StoreConfig(jsonNaming: new CamelCaseNaming())));
            var naming = m.Read<Naming>(json);
            var result = m.Write(naming);
            string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));
            
            AreEqual(expect, result);
        }
        
        [Test]
        public void PascalCaseTest() {
            string json = @"
            {
                ""Lower"":   10,
                ""Upper"":   11
            }";
            var m = new JsonMapper(new TypeStore(null, new StoreConfig(jsonNaming: new PascalCaseNaming())));
            var naming = m.Read<Naming>(json);
            var result = m.Write(naming);
            string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));
            
            AreEqual(expect, result);
        }
        

    }
}