using Friflo.Json.EntityGraph.Filter;
using Friflo.Json.Mapper;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class FilterRoot
    {
        public string name;
    }
    
    public static class TestGraphFilter
    {
        [Test]
        public static void Test () {

            var equals = new Equals {
                left  = new Field           { field = ".name"  },
                right = new StringLiteral   { value = "Smith"  }
            };

            var root = new FilterRoot {
                name = "Smith"
            };

            using (var filter       = new JsonFilter())
            using (var jsonMapper   = new JsonMapper())
            {
                var json = jsonMapper.Write(root);
                
                filter.Filter(json, equals);
            }
        }
    }
}