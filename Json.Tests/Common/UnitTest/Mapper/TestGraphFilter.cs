using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.EntityGraph.Filter;
using Friflo.Json.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable CollectionNeverQueried.Global
namespace Friflo.Json.Tests.Common.UnitTest.Mapper
{
    public class Person
    {
        public          string       name;
        public readonly List<Person> children = new List<Person>();
    }
    
    public static class TestGraphFilter
    {
        [Test]
        public static void Test () {
            using (var filter       = new JsonFilter())
            using (var jsonMapper   = new JsonMapper())
            {
                var peter = new Person { name = "Peter" };
                peter.children.Add(new Person { name = "Paul" });
                peter.children.Add(new Person { name = "Marry" });
                var peterJson = jsonMapper.Write(peter);
                
                var john = new Person { name = "John" };
                john.children.Add(new Person { name = "Simon" });
                var johnJson = jsonMapper.Write(john);

                // ---
                var isPeter = new Equals {
                    left    = new StringLiteral   { value = "Peter"  },
                    right   = new Field           { field = ".name"  }
                };
                bool IsPeter(Person p) => p.name == "Peter";
                IsTrue (IsPeter(peter));
                IsTrue (filter.Filter(peterJson, isPeter));
                IsFalse(filter.Filter(johnJson,  isPeter));
                
                // ---
                var hasChildPaul = new Any {
                    lambda = new Equals {
                        left    = new StringLiteral   { value = "Paul"  },    
                        right   = new Field           { field = ".children[*].name"  },
                    }
                };
                bool HasChildPaul(Person p) => p.children.Any((child) => child.name == "Paul");
                IsTrue (HasChildPaul(peter));
                IsTrue (filter.Filter(peterJson, hasChildPaul));
                IsFalse(filter.Filter(johnJson,  hasChildPaul));
            }
        }
    }
}