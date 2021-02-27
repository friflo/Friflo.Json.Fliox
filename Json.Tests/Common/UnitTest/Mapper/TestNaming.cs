using System.Linq;
using Friflo.Json.Mapper;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;
#pragma warning disable 649 // Field 'field' is never assigned

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class TestNaming : LeakTestsFixture
    {
        class Naming {
            public int      lower;
            public int      Upper;

            // ignored members
            [FloIgnore]
            public int      ignoredField;
            
            [FloIgnore]
            public int      ignoredProperty { get; set; }

            // custom member names
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
            using (var typeStore =  new TypeStore(new StoreConfig(jsonNaming: new CamelCaseNaming())))
            using (var m = new JsonMapper(typeStore)) {
                var naming = m.Read<Naming>(json);
                var result = m.Write(naming);
                string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));
                
                AreEqual(expect, result);
            }
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
            using (var typeStore = new TypeStore(new StoreConfig(jsonNaming: new PascalCaseNaming())))
            using (var m = new JsonMapper(typeStore)) {
                var naming = m.Read<Naming>(json);
                var result = m.Write(naming);
                string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));

                AreEqual(expect, result);
            }
        }
        

    }
}