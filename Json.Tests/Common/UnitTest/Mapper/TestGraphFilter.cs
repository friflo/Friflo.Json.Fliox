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
                jsonMapper.Pretty = true;
                var peter =         new Person { name = "Peter", age = 40 };
                peter.children.Add( new Person { name = "Paul" , age = 20 });
                peter.children.Add( new Person { name = "Marry", age = 20 });
                var peterJson = jsonMapper.Write(peter);
                
                var john =          new Person { name = "John",  age = 30 };
                john.children.Add(  new Person { name = "Simon", age = 10 });
                var johnJson = jsonMapper.Write(john);

                // ---
                var  isPeter         = new Equals(new Field (".name"), new StringLiteral ("Peter"));
                bool IsPeter(Person p) => p.name == "Peter";
                
                var  isAgeGreater35  = new GreaterThan(new Field (".age"), new NumberLiteral (35));
                bool IsAgeGreater35(Person p) => p.age > 35;
                
                var isNotAgeGreater35  = new Not(isAgeGreater35);
            
                IsTrue  (IsPeter(peter));
                IsTrue  (filter.Filter(peterJson, isPeter));
                IsFalse (filter.Filter(johnJson,  isPeter));
                
                IsTrue  (IsAgeGreater35(peter));
                IsTrue  (filter.Filter(peterJson, isAgeGreater35));
                IsFalse (filter.Filter(johnJson,  isAgeGreater35));
                // Not
                IsFalse (filter.Filter(peterJson, isNotAgeGreater35));
                IsTrue  (filter.Filter(johnJson,  isNotAgeGreater35));

                // --- Any
                var  hasChildPaul = new Any (new Equals (new Field (".children[*].name"), new StringLiteral ("Paul")));
                bool HasChildPaul(Person p) => p.children.Any(child => child.name == "Paul");
                
                var hasChildAgeLess12 = new Any (new LessThan (new Field (".children[*].age"), new NumberLiteral (12)));
                
                IsTrue (HasChildPaul(peter));
                IsTrue (filter.Filter(peterJson, hasChildPaul));
                IsFalse(filter.Filter(johnJson,  hasChildPaul));
                
                IsFalse(filter.Filter(peterJson, hasChildAgeLess12));
                IsTrue (filter.Filter(johnJson,  hasChildAgeLess12));
                
                // --- All
                var allChildAgeEquals20 = new All (new Equals(new Field (".children[*].age"), new NumberLiteral (20)));
                IsTrue (filter.Filter(peterJson, allChildAgeEquals20));
                IsFalse(filter.Filter(johnJson,  allChildAgeEquals20));
            }
        }
    }
}