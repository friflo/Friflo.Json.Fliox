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
                var  isPeter         = new Equal(new Field (".name"), new StringLiteral ("Peter"));
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
                var  hasChildPaul = new Any (new Equal (new Field (".children[*].name"), new StringLiteral ("Paul")));
                bool HasChildPaul(Person p) => p.children.Any(child => child.name == "Paul");
                
                var hasChildAgeLess12 = new Any (new LessThan (new Field (".children[*].age"), new NumberLiteral (12)));
                
                IsTrue (HasChildPaul(peter));
                IsTrue (filter.Filter(peterJson, hasChildPaul));
                IsFalse(filter.Filter(johnJson,  hasChildPaul));
                
                IsFalse(filter.Filter(peterJson, hasChildAgeLess12));
                IsTrue (filter.Filter(johnJson,  hasChildAgeLess12));
                
                // --- All
                var allChildAgeEquals20 = new All (new Equal(new Field (".children[*].age"), new NumberLiteral (20)));
                IsTrue (filter.Filter(peterJson, allChildAgeEquals20));
                IsFalse(filter.Filter(johnJson,  allChildAgeEquals20));
                
                
                // --- test with arithmetic operations
                var  isAge40  = new Equal(new Field (".age"), new Add(new NumberLiteral (35), new NumberLiteral(5)));
                IsTrue  (filter.Filter(peterJson, isAge40));
                
                var  isChildAge20  = new Equal(new Field (".children[*].age"), new Add(new NumberLiteral (15), new NumberLiteral(5)));
                IsTrue  (filter.Filter(peterJson, isChildAge20));
                
                
                
                // ------------------------------ Test runtime assertions ------------------------------
                Exception e;
                // --- compare operators must not be reused
                var reuseCompareOp = new Equal(isAgeGreater35, isAgeGreater35);
                e = Throws<InvalidOperationException>(() => filter.Filter(peterJson, reuseCompareOp));
                AreEqual("Used operator instance is not applicable for reuse. Use a clone. Type: GreaterThan, instance: .age > 35", e.Message);
                
                // --- group operators must not be reused
                var testGroupOp = new And(new List<BoolOp> {new Equal(new StringLiteral("A"), new StringLiteral("B"))});
                var reuseGroupOp = new Equal(testGroupOp, testGroupOp);
                e = Throws<InvalidOperationException>(() => filter.Filter(peterJson, reuseGroupOp));
                AreEqual("Used operator instance is not applicable for reuse. Use a clone. Type: And, instance: \"A\" == \"B\"", e.Message);

                // --- literal and field operators are applicable for reuse
                var testLiteral = new StringLiteral("Test");
                var reuseLiterals = new Equal(testLiteral, testLiteral);
                filter.Filter(peterJson, reuseLiterals);
                
                var testField = new Field (".name");
                var reuseField = new Equal(testField, testField);
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

        [Test]
        public static void TestQueryConversion() {
            
            // --- comparision operators
            var isEqual = Operator.FromFilter((Person p) => p.name == "Peter");
            AreEqual("name == \"Peter\"", isEqual.ToString());
            
            var isNotEqual = Operator.FromFilter((Person p) => p.name != "Peter");
            AreEqual("name != \"Peter\"", isNotEqual.ToString());

            var isLess = Operator.FromFilter((Person p) => p.age < 20);
            AreEqual("age < 20", isLess.ToString());
            
            var isLessOrEqual = Operator.FromFilter((Person p) => p.age <= 20);
            AreEqual("age <= 20", isLessOrEqual.ToString());

            var isGreater = Operator.FromFilter((Person p) => p.age > 20);
            AreEqual("age > 20", isGreater.ToString());
            
            var isGreaterOrEqual = Operator.FromFilter((Person p) => p.age >= 20);
            AreEqual("age >= 20", isGreaterOrEqual.ToString());

            
            // --- unary operators
            var isNot = Operator.FromFilter((Person p) => !(p.age >= 20));
            AreEqual("!(age >= 20)", isNot.ToString());
                
            var any = Operator.FromFilter((Person p) => p.children.Any(child => child.age == 20));
            AreEqual("Any(age == 20)", any.ToString()); // todo fix Any
            
            var all = Operator.FromFilter((Person p) => p.children.All(child => child.age == 20));
            AreEqual("All(age == 20)", all.ToString()); // todo fix Any
            
            
            // --- unary arithmetic operators
            var abs = Operator.FromLambda((Person p) => Math.Abs(-1));
            AreEqual("Abs(-1)", abs.ToString());
            
            var ceiling = Operator.FromLambda((Person p) => Math.Ceiling(2.5));
            AreEqual("Ceiling(2.5)", ceiling.ToString());
            
            var floor = Operator.FromLambda((Person p) => Math.Floor(2.5));
            AreEqual("Floor(2.5)", floor.ToString());
            
            var exp = Operator.FromLambda((Person p) => Math.Exp(2.5));
            AreEqual("Exp(2.5)", exp.ToString());
            
            var log = Operator.FromLambda((Person p) => Math.Log(2.5));
            AreEqual("Log(2.5)", log.ToString());
            
            var sqrt = Operator.FromLambda((Person p) => Math.Sqrt(2.5));
            AreEqual("Sqrt(2.5)", sqrt.ToString());

        }
    }
}