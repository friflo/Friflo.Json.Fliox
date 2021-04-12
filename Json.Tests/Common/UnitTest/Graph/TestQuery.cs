using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Graph.Query;
using Friflo.Json.Flow.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable CollectionNeverQueried.Global
namespace Friflo.Json.Tests.Common.UnitTest.Graph
{
    public class Person
    {
        public          string          name;
        public          int             age;
        public readonly List<Person>    children = new List<Person>();
        public readonly List<Hobby>     hobbies = new List<Hobby>();
    }
    
    public class Hobby
    {
        public          string          name;
    }
    
    
    public static class TestQuery
    {
        static readonly Person Peter =         new Person {
            name = "Peter", age = 40,
            children = {
                new Person {
                    name = "Paul" , age = 20,
                    hobbies = {
                        new Hobby{ name= "Biking"},
                        new Hobby{ name= "Surfing"}
                    }
                },
                new Person {
                    name = "Marry", age = 20,
                    hobbies = {
                        new Hobby{ name= "Biking"}
                    }
                }
            }
        };
        
        static readonly Person John =         new Person {
            name = "John",  age = 30,
            children = {
                new Person {
                    name = "Simon", age = 10,
                    hobbies = {
                        new Hobby{ name= "Biking"}
                    }
                }
            }
        };
        
        [Test]
        public static void TestFilter () {
            using (var eval         = new JsonEvaluator())
            using (var jsonMapper   = new JsonMapper())
            {
                jsonMapper.Pretty = true;
                var peter = jsonMapper.Write(Peter);
                var john  = jsonMapper.Write(John);

                // ---
                var  isPeter         = new JsonLambda(new Equal(new Field (".name"), new StringLiteral ("Peter")));
                bool IsPeter(Person p) => p.name == "Peter";
                
                var  isAgeGreater35  = new GreaterThan(new Field (".age"), new LongLiteral (35));
                bool IsAgeGreater35(Person p) => p.age > 35;
                
                var isNotAgeGreater35  = new Not(isAgeGreater35);
            
                IsTrue  (IsPeter(Peter));
                IsTrue  (eval.Filter(peter, isPeter));
                IsFalse (eval.Filter(john,  isPeter));
                
                IsTrue  (IsAgeGreater35(Peter));
                IsTrue  (eval.Filter(peter, isAgeGreater35));
                IsFalse (eval.Filter(john,  isAgeGreater35));
                // Not
                IsFalse (eval.Filter(peter, isNotAgeGreater35));
                IsTrue  (eval.Filter(john,  isNotAgeGreater35));

                // --- Any
                var  hasChildPaul = new Any (new Equal (new Field (".children[*].name"), new StringLiteral ("Paul")));
                bool HasChildPaul(Person p) => p.children.Any(child => child.name == "Paul");
                
                var hasChildAgeLess12 = new Any (new LessThan (new Field (".children[*].age"), new LongLiteral (12)));
                
                IsTrue (HasChildPaul(Peter));
                IsTrue (eval.Filter(peter, hasChildPaul));
                IsFalse(eval.Filter(john,  hasChildPaul));
                
                IsFalse(eval.Filter(peter, hasChildAgeLess12));
                IsTrue (eval.Filter(john,  hasChildAgeLess12));
                
                // --- All
                var allChildAgeEquals20 = new All (new Equal(new Field (".children[*].age"), new LongLiteral (20)));
                IsTrue (eval.Filter(peter, allChildAgeEquals20));
                IsFalse(eval.Filter(john,  allChildAgeEquals20));
                
                
                // --- test with arithmetic operations
                var  isAge40  = new Equal(new Field (".age"), new Add(new LongLiteral (35), new LongLiteral(5)));
                IsTrue  (eval.Filter(peter, isAge40));
                
                var  isChildAge20  = new Equal(new Field (".children[*].age"), new Add(new LongLiteral (15), new LongLiteral(5)));
                IsTrue  (eval.Filter(peter, isChildAge20));
                
                
                
                // ------------------------------ Test runtime assertions ------------------------------
                Exception e;
                // --- compare operators must not be reused
                var reuseCompareOp = new Equal(isAgeGreater35, isAgeGreater35);
                e = Throws<InvalidOperationException>(() => eval.Filter(peter, reuseCompareOp));
                AreEqual("Used operator instance is not applicable for reuse. Use a clone. Type: GreaterThan, instance: .age > 35", e.Message);
                
                // --- group operators must not be reused
                var testGroupOp = new And(new List<BoolOp> {new Equal(new StringLiteral("A"), new StringLiteral("B"))});
                var reuseGroupOp = new Equal(testGroupOp, testGroupOp);
                e = Throws<InvalidOperationException>(() => eval.Filter(peter, reuseGroupOp));
                AreEqual("Used operator instance is not applicable for reuse. Use a clone. Type: And, instance: ('A' == 'B')", e.Message);

                // --- literal and field operators are applicable for reuse
                var testLiteral = new StringLiteral("Test");
                var reuseLiterals = new Equal(testLiteral, testLiteral);
                eval.Filter(peter, reuseLiterals);
                
                var testField = new Field (".name");
                var reuseField = new Equal(testField, testField);
                eval.Filter(peter, reuseField);
            }
        }
        
        [Test]
        public static void TestGroupFilter () {
            using (var eval         = new JsonEvaluator())
            using (var jsonMapper   = new JsonMapper())
            {
                jsonMapper.Pretty = true;
                var peter = jsonMapper.Write(Peter);
                var john  = jsonMapper.Write(John);

                // --- Any
                var  hasChildHobbySurfing = new Any (new Equal (new Field (".children[*].hobbies[*].name"), new StringLiteral ("Surfing")));
                bool HasChildHobbySurfing(Person p) => p.children.Any(child => child.hobbies.Any(hobby => hobby.name == "Surfing"));
                
                AreEqual("Any(.children[*].hobbies[*].name == 'Surfing')", hasChildHobbySurfing.ToString());
                IsTrue (HasChildHobbySurfing(Peter));
                IsTrue (eval.Filter(peter, hasChildHobbySurfing));
                IsFalse(eval.Filter(john,  hasChildHobbySurfing));
            }
        }

        [Test]
        public static void TestEval() {
            using (var eval         = new JsonEvaluator())
            using (var jsonMapper   = new JsonMapper())
            {
                jsonMapper.Pretty = true;
                // use expression
                AreEqual("hello",   eval.Eval("{}", JsonLambda.Create<Person>(p => "hello")));
                // use lambda
                AreEqual("hello",   eval.Eval("{}", new StringLiteral("hello").Lambda()));
                // use operator
                AreEqual("hello",   eval.Eval("{}", new StringLiteral("hello")));
                AreEqual(42.0,      eval.Eval("{}", new DoubleLiteral(42.0)));
                AreEqual(true,      eval.Eval("{}", new BoolLiteral(true)));
                AreEqual(null,      eval.Eval("{}", new NullLiteral()));

                // unary arithmetic operations
                var abs     = new Abs(new LongLiteral(-2));
                AreEqual(2,         eval.Eval("{}", abs));
                
                var ceiling = new Ceiling(new DoubleLiteral(2.5));
                AreEqual(3,         eval.Eval("{}", ceiling));
                
                var floor   = new Floor(new DoubleLiteral(2.5));
                AreEqual(2,         eval.Eval("{}", floor));
                
                var exp     = new Exp(new DoubleLiteral(Math.Log(2)));
                AreEqual(2,         eval.Eval("{}", exp));
                
                var log     = new Log(new DoubleLiteral(Math.Exp(3)));
                AreEqual(3,         eval.Eval("{}", log));
                
                var sqrt    = new Sqrt(new DoubleLiteral(9));
                AreEqual(3,         eval.Eval("{}", sqrt));

                

                // binary arithmetic operations
                var add      = new Add(new LongLiteral(1), new LongLiteral(2));
                AreEqual(3,         eval.Eval("{}", add));
                
                var subtract = new Subtract(new LongLiteral(1), new LongLiteral(2));
                AreEqual(-1,        eval.Eval("{}", subtract));
                
                var multiply = new Multiply(new LongLiteral(2), new LongLiteral(3));
                AreEqual(6,         eval.Eval("{}", multiply));
                
                var divide   = new Divide(new LongLiteral(10), new LongLiteral(2));
                AreEqual(5,         eval.Eval("{}", divide));
            }
        }

        [Test]
        public static void TestQueryConversion() {

            // --- comparision operators
            var isEqual =           (Equal)             Operator.FromFilter((Person p) => p.name == "Peter");
            AreEqual("name == 'Peter'", isEqual.ToString());
            
            var isNotEqual =        (NotEqual)          Operator.FromFilter((Person p) => p.name != "Peter");
            AreEqual("name != 'Peter'", isNotEqual.ToString());

            var isLess =            (LessThan)          Operator.FromFilter((Person p) => p.age < 20);
            AreEqual("age < 20", isLess.ToString());
            
            var isLessOrEqual =     (LessThanOrEqual)   Operator.FromFilter((Person p) => p.age <= 20);
            AreEqual("age <= 20", isLessOrEqual.ToString());

            var isGreater =         (GreaterThan)       Operator.FromFilter((Person p) => p.age > 20);
            AreEqual("age > 20", isGreater.ToString());
            
            var isGreaterOrEqual =  (GreaterThanOrEqual)Operator.FromFilter((Person p) => p.age >= 20);
            AreEqual("age >= 20", isGreaterOrEqual.ToString());
            
            // --- group operators
            var or =    (Or)        Operator.FromFilter((Person p) => p.age >= 20 || p.name == "Peter");
            AreEqual("(age >= 20 || name == 'Peter')", or.ToString());
            
            var and =   (And)       Operator.FromFilter((Person p) => p.age >= 20 && p.name == "Peter");
            AreEqual("(age >= 20 && name == 'Peter')", and.ToString());

            
            // --- unary operators
            var isNot = (Not)       Operator.FromFilter((Person p) => !(p.age >= 20));
            AreEqual("!(age >= 20)", isNot.ToString());
                
            var any =   (Any)       Operator.FromFilter((Person p) => p.children.Any(child => child.age == 20));
            AreEqual("Any(age == 20)", any.ToString()); // todo fix Any
            
            var all =   (All)       Operator.FromFilter((Person p) => p.children.All(child => child.age == 20));
            AreEqual("All(age == 20)", all.ToString()); // todo fix Any
            
            
            // --- literals
            var lng     = (LongLiteral)     Operator.FromLambda((Person p) => 1);
            AreEqual("1",           lng.ToString());
            
            var dbl     = (DoubleLiteral)   Operator.FromLambda((Person p) => 1.5);
            AreEqual("1.5",         dbl.ToString());

            var str     = (StringLiteral)   Operator.FromLambda((Person p) => "hello");
            AreEqual("'hello'",     str.ToString());
            
            var @true   = (BoolLiteral)     Operator.FromLambda((Person p) => true);
            AreEqual("true",        @true.ToString());

            var @null   = (NullLiteral)     Operator.FromLambda((Person p) => null);
            AreEqual("null",        @null.ToString());

            
            // --- unary arithmetic operators
            var abs     = (Abs)     Operator.FromLambda((Person p) => Math.Abs(-1));
            AreEqual("Abs(-1)", abs.ToString());
            
            var ceiling = (Ceiling) Operator.FromLambda((Person p) => Math.Ceiling(2.5));
            AreEqual("Ceiling(2.5)", ceiling.ToString());
            
            var floor   = (Floor)   Operator.FromLambda((Person p) => Math.Floor(2.5));
            AreEqual("Floor(2.5)", floor.ToString());
            
            var exp     = (Exp)     Operator.FromLambda((Person p) => Math.Exp(2.5));
            AreEqual("Exp(2.5)", exp.ToString());
            
            var log     = (Log)     Operator.FromLambda((Person p) => Math.Log(2.5));
            AreEqual("Log(2.5)", log.ToString());
            
            var sqrt    = (Sqrt)    Operator.FromLambda((Person p) => Math.Sqrt(2.5));
            AreEqual("Sqrt(2.5)", sqrt.ToString());
            
            
            // --- binary arithmetic operators
            var add         = (Add)     Operator.FromLambda((Person p) => 1 + Math.Abs(1.0));
            AreEqual("1 + Abs(1)", add.ToString());

            var subtract    = (Subtract)Operator.FromLambda((Person p) => 1 - Math.Abs(1.0));
            AreEqual("1 - Abs(1)", subtract.ToString());

            var multiply    = (Multiply)Operator.FromLambda((Person p) => 1 * Math.Abs(1.0));
            AreEqual("1 * Abs(1)", multiply.ToString());

            var divide      = (Divide)  Operator.FromLambda((Person p) => 1 / Math.Abs(1.0));
            AreEqual("1 / Abs(1)", divide.ToString());

        }
    }
}