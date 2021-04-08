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
                var person = new Person { name = "Peter" };
                person.children.Add(new Person { name = "Paul" });
                person.children.Add(new Person { name = "Marry" });
                var json = jsonMapper.Write(person);
                
                // ---
                var isPeter = new Equals {
                    left    = new StringLiteral   { value = "Peter"  },
                    right   = new Field           { field = ".name"  }
                };
                bool IsPeter(Person p) => p.name == "Peter";
                IsTrue(IsPeter(person));
                filter.Filter(json, isPeter);
                
                // ---
                var hasChildPaul = new Any {
                    lambda = new Equals {
                        right = new StringLiteral   { value = "Paul"  },    
                        left  = new Field           { field = ".children[*].name"  },
                    }
                };
                bool HasChildPaul(Person p) => p.children.Any((child) => child.name == "Paul");
                IsTrue(HasChildPaul(person));
                filter.Filter(json, hasChildPaul);
            }
        }
    }
}