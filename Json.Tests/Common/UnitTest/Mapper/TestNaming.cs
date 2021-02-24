using System.Linq;
using Friflo.Json.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestNaming
    {
        class Naming {
            public int      lower;
            public int      Upper;
            
            [FloProperty(Name = "field")]
            public int      namedField;
            
            [FloProperty(Name = "property")]
            public int      namedProperty { get; set; }
        }
        
        [Test]
        public void CamelCaseTest() {
            string json = @"
            {
                ""property"":   10,
                ""lower"":      11,
                ""upper"":      12,
                ""field"":      13
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
                ""property"":   10,
                ""Lower"":      11,
                ""Upper"":      12,
                ""field"":      13
            }";
            var m = new JsonMapper(new TypeStore(null, new StoreConfig(jsonNaming: new PascalCaseNaming())));
            var naming = m.Read<Naming>(json);
            var result = m.Write(naming);
            string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));
            
            AreEqual(expect, result);
        }
        

    }
}