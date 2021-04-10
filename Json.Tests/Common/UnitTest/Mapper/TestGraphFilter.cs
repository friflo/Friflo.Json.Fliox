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
        public          string          name;
        public          int             age;
        public readonly List<Person>    children = new List<Person>();
    }
    
    public static class TestGraphFilter
    {
        [Test]
        public static void TestFilter () {
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
                var isPeterOp = Operator.FromFilter((Person p) => p.name == "Peter");
                AreEqual("name == \"Peter\"", isPeterOp.ToString());
                
                
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
                
                
                // --- test with arithmetic operations
                var  isAge40  = new Equals(new Field (".age"), new Add(new NumberLiteral (35), new NumberLiteral(5)));
                IsTrue  (filter.Filter(peterJson, isAge40));
                
                var  isChildAge20  = new Equals(new Field (".children[*].age"), new Add(new NumberLiteral (15), new NumberLiteral(5)));
                IsTrue  (filter.Filter(peterJson, isChildAge20));
                
                
                
                // ------------------------------ Test runtime assertions ------------------------------
                Exception e;
                // --- compare operators must not be reused
                var reuseCompareOp = new Equals(isAgeGreater35, isAgeGreater35);
                e = Throws<InvalidOperationException>(() => filter.Filter(peterJson, reuseCompareOp));
                AreEqual("Used operator instance is not applicable for reuse. Use a clone. Type: GreaterThan, instance: .age > 35", e.Message);
                
                // --- group operators must not be reused
                var testGroupOp = new And(new List<BoolOp> {new Equals(new StringLiteral("A"), new StringLiteral("B"))});
                var reuseGroupOp = new Equals(testGroupOp, testGroupOp);
                e = Throws<InvalidOperationException>(() => filter.Filter(peterJson, reuseGroupOp));
                AreEqual("Used operator instance is not applicable for reuse. Use a clone. Type: And, instance: \"A\" == \"B\"", e.Message);

                // --- literal and field operators are applicable for reuse
                var testLiteral = new StringLiteral("Test");
                var reuseLiterals = new Equals(testLiteral, testLiteral);
                filter.Filter(peterJson, reuseLiterals);
                
                var testField = new Field (".name");
                var reuseField = new Equals(testField, testField);
                filter.Filter(peterJson, reuseField);
            }
        }

        [Test]
        public static void TestEval() {
            using (var filter       = new JsonFilter())
            using (var jsonMapper   = new JsonMapper())
            {
                jsonMapper.Pretty = true;
                AreEqual("hello",   filter.Eval("{}", new StringLiteral("hello")));
                AreEqual(42.0,      filter.Eval("{}", new NumberLiteral(42.0)));
                AreEqual(true,      filter.Eval("{}", new BooleanLiteral(true)));
                AreEqual(null,      filter.Eval("{}", new NullLiteral()));

                // unary arithmetic operations
                var abs     = new Abs(new NumberLiteral(-2));
                AreEqual(2,         filter.Eval("{}", abs));
                
                var ceiling = new Ceiling(new NumberLiteral(2.5));
                AreEqual(3,         filter.Eval("{}", ceiling));
                
                var floor   = new Floor(new NumberLiteral(2.5));
                AreEqual(2,         filter.Eval("{}", floor));
                
                var exp     = new Exp(new NumberLiteral(Math.Log(2)));
                AreEqual(2,         filter.Eval("{}", exp));
                
                var log     = new Log(new NumberLiteral(Math.Exp(3)));
                AreEqual(3,         filter.Eval("{}", log));
                
                var sqrt    = new Sqrt(new NumberLiteral(9));
                AreEqual(3,         filter.Eval("{}", sqrt));

                

                // binary arithmetic operations
                var add      = new Add(new NumberLiteral(1), new NumberLiteral(2));
                AreEqual(3,         filter.Eval("{}", add));
                
                var subtract = new Subtract(new NumberLiteral(1), new NumberLiteral(2));
                AreEqual(-1,        filter.Eval("{}", subtract));
                
                var multiply = new Multiply(new NumberLiteral(2), new NumberLiteral(3));
                AreEqual(6,         filter.Eval("{}", multiply));
                
                var divide   = new Divide(new NumberLiteral(10), new NumberLiteral(2));
                AreEqual(5,         filter.Eval("{}", divide));
            }
        }
    }
}