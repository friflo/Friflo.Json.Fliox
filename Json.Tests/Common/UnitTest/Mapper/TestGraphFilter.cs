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
        public          string          name;
        public          int             age;
        public readonly List<Person>    children = new List<Person>();
    }
    
    public static class TestGraphFilter
    {
        [Test]
        public static void Test () {
            using (var filter       = new JsonFilter())
            using (var jsonMapper   = new JsonMapper())
            {
                var peter =         new Person { name = "Peter", age = 40};
                peter.children.Add( new Person { name = "Paul" , age = 20});
                peter.children.Add( new Person { name = "Marry", age = 15 });
                var peterJson = jsonMapper.Write(peter);
                
                var john =          new Person { name = "John",  age = 30 };
                john.children.Add(  new Person { name = "Simon", age = 10 });
                var johnJson = jsonMapper.Write(john);

                // ---
                var isPeter = new Equals {
                    left    = new StringLiteral   { value = "Peter"  },
                    right   = new Field           { field = ".name"  }
                };
                var isAgeGreater35 = new GreaterThan {
                    left   = new Field           { field = ".age"  },
                    right  = new NumberLiteral   { value = 35  }
                };
                bool IsPeter(Person p) => p.name == "Peter";
                IsTrue (IsPeter(peter));
                IsTrue (filter.Filter(peterJson, isPeter));
                IsFalse(filter.Filter(johnJson,  isPeter));
                
                bool IsAgeGreater35(Person p) => p.age > 35;
                IsTrue(IsAgeGreater35(peter));
                IsTrue (filter.Filter(peterJson, isAgeGreater35));
                IsFalse(filter.Filter(johnJson,  isAgeGreater35));
                
                
                // ---
                var hasChildPaul = new Any {
                    lambda = new Equals {
                        left    = new StringLiteral   { value = "Paul"  },    
                        right   = new Field           { field = ".children[*].name"  },
                    }
                };
                var hasChildAgeLess12 = new Any {
                    lambda = new LessThan {
                        left   = new Field           { field = ".children[*].age"  },
                        right  = new NumberLiteral   { value = 12  },
                    }
                };
                bool HasChildPaul(Person p) => p.children.Any((child) => child.name == "Paul");
                IsTrue (HasChildPaul(peter));
                IsTrue (filter.Filter(peterJson, hasChildPaul));
                IsFalse(filter.Filter(johnJson,  hasChildPaul));
                
                IsFalse(filter.Filter(peterJson, hasChildAgeLess12));
                IsTrue (filter.Filter(johnJson,  hasChildAgeLess12));
            }
        }
    }
}