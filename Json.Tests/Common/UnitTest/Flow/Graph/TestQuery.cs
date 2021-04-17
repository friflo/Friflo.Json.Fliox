using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Graph.Query;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable CollectionNeverQueried.Global
namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
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
        public static readonly Person Peter =         new Person {
            name = "Peter", age = 40,
            children = {
                new Person {
                    name = "Paul" , age = 20,
                    hobbies = {
                        new Hobby{ name= "Gaming"},
                        new Hobby{ name= "Biking"},
                        new Hobby{ name= "Travelling"},
                    }
                },
                new Person {
                    name = "Marry", age = 20,
                    hobbies = {
                        new Hobby{ name= "Biking"},
                        new Hobby{ name= "Surfing"}
                    }
                }
            }
        };

        public static readonly Person John =         new Person {
            name = "John",  age = 30,
            children = {
                new Person {
                    name = "Simon", age = 10,
                    hobbies = {
                        new Hobby{ name= "Biking"}
                    }
                },
                new Person {
                    name = "Garfunkel", age = 11,
                    hobbies = {
                        new Hobby{ name= "Biking"}
                    }
                }
            }
        };
        
        [Test]
        public static void TestFilter () {
            using (var eval         = new JsonEvaluator())
            using (var jsonMapper   = new ObjectMapper())
            {
                jsonMapper.Pretty = true;
                var peter = jsonMapper.Write(Peter);
                var john  = jsonMapper.Write(John);

                // ---
                var  isPeter         = new Equal(new Field (".name"), new StringLiteral ("Peter")).Filter();
                AreEqual(".name == 'Peter'", isPeter.ToString());
                var  isPeter2        = JsonFilter.Create<Person>(p => p.name == "Peter");
                AreEqual(".name == 'Peter'", isPeter2.ToString());
                
                bool IsPeter(Person p) => p.name == "Peter";

                var  isAgeGreater35Op = new GreaterThan(new Field(".age"), new LongLiteral(35));
                var  isAgeGreater35  = isAgeGreater35Op.Filter();
                bool IsAgeGreater35(Person p) => p.age > 35;
                
                var isNotAgeGreater35  = new Not(isAgeGreater35Op).Filter();
            
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
                var  hasChildPaul = new Any (new Field (".children[=>]"), "child", new Equal (new Field ("child.name"), new StringLiteral ("Paul"))).Filter();
                bool HasChildPaul(Person p) => p.children.Any(child => child.name == "Paul");
                
                var hasChildAgeLess12 = new Any (new Field (".children[=>]"), "child", new LessThan (new Field ("child.age"), new LongLiteral (12))).Filter();
                
                IsTrue (HasChildPaul(Peter));
                IsTrue (eval.Filter(peter, hasChildPaul));
                IsFalse(eval.Filter(john,  hasChildPaul));
                
                IsFalse(eval.Filter(peter, hasChildAgeLess12));
                IsTrue (eval.Filter(john,  hasChildAgeLess12));
                
                // --- All
                var allChildAgeEquals20 = new All (new Field (".children[=>]"), "child", new Equal(new Field ("child.age"), new LongLiteral (20))).Filter();
                IsTrue (eval.Filter(peter, allChildAgeEquals20));
                IsFalse(eval.Filter(john,  allChildAgeEquals20));
                
                
                // --- test with arithmetic operations
                var  isAge40  = new Equal(new Field (".age"), new Add(new LongLiteral (35), new LongLiteral(5))).Filter();
                IsTrue  (eval.Filter(peter, isAge40));
                
                var  isChildAge20  = new Equal(new Field (".children[*].age"), new Add(new LongLiteral (15), new LongLiteral(5))).Filter();
                IsTrue  (eval.Filter(peter, isChildAge20));
                
                
                
                // ------------------------------ Test runtime assertions ------------------------------
                Exception e;
                // --- compare operators must not be reused
                e = Throws<InvalidOperationException>(() => _ = new Equal(isAgeGreater35Op, isAgeGreater35Op).Filter());
                AreEqual("Used operator instance is not applicable for reuse. Use a clone. Type: GreaterThan, instance: .age > 35", e.Message);
                
                // --- group operators must not be reused
                var testGroupOp = new And(new List<BoolOp> {new Equal(new StringLiteral("A"), new StringLiteral("B"))});
                e = Throws<InvalidOperationException>(() => _ = new Equal(testGroupOp, testGroupOp).Filter());
                AreEqual("Used operator instance is not applicable for reuse. Use a clone. Type: And, instance: 'A' == 'B'", e.Message);

                // --- literal and field operators are applicable for reuse
                var testLiteral = new StringLiteral("Test");
                var reuseLiterals = new Equal(testLiteral, testLiteral).Filter();
                eval.Filter(peter, reuseLiterals);
                
                var testField = new Field (".name");
                var reuseField = new Equal(testField, testField).Filter();
                eval.Filter(peter, reuseField);
            }
        }

        [Test]
        public static void NoAllocFilter() {
            var memLog = new MemoryLogger(10, 10, MemoryLog.Enabled);
            using (var eval = new JsonEvaluator())
            using (var jsonMapper = new ObjectMapper()) {
                jsonMapper.Pretty = true;
                var peter = jsonMapper.Write(Peter);
                
                var anyChildAgeWithin10And20 = JsonFilter.Create<Person>(p => p.children.All(child => child.age >= 20 && child.age <= 20));
                bool result = false;
                for (int n = 0; n < 100; n++) {
                    result = eval.Filter(peter, anyChildAgeWithin10And20);
                    memLog.Snapshot();
                }
                IsTrue(result);
            }
            memLog.AssertNoAllocations();
        }

        [Test]
        public static void TestGroupFilter () {
            using (var eval         = new JsonEvaluator())
            using (var jsonMapper   = new ObjectMapper())
            {
                jsonMapper.Pretty = true;
                var peter = jsonMapper.Write(Peter);
                var john  = jsonMapper.Write(John);

                // --- Any
                var  hasChildHobbySurfing = new Any (new Field (".children"), "child", new Equal (new Field ("child.hobbies[*].name"), new StringLiteral ("Surfing"))).Filter();
                bool HasChildHobbySurfing(Person p) => p.children.Any(child => child.hobbies.Any(hobby => hobby.name == "Surfing"));
                
                AreEqual(".children.Any(child => child.hobbies[*].name == 'Surfing')", hasChildHobbySurfing.ToString());
                IsTrue (HasChildHobbySurfing(Peter));
                IsTrue (eval.Filter(peter, hasChildHobbySurfing));
                IsFalse(eval.Filter(john,  hasChildHobbySurfing));
            }
        }

        [Test]
        public static void TestEval() {
            using (var eval         = new JsonEvaluator())
            using (var jsonMapper   = new ObjectMapper())
            {
                jsonMapper.Pretty = true;
                var john  = jsonMapper.Write(John);
                
                // --- use expression
                AreEqual("hello",   eval.Eval("{}", JsonLambda.Create<object>(p => "hello")));
                
                // use operator
                AreEqual("hello",   eval.Eval("{}", new StringLiteral("hello").Lambda()));
                AreEqual(42.0,      eval.Eval("{}", new DoubleLiteral(42.0).Lambda()));
                AreEqual(true,      eval.Eval("{}", new BoolLiteral(true).Lambda()));
                AreEqual(null,      eval.Eval("{}", new NullLiteral().Lambda()));

                
                // --- unary arithmetic operations
                var abs     = new Abs(new LongLiteral(-2));
                AreEqual(2,         eval.Eval("{}", abs.Lambda()));
                
                var ceiling = new Ceiling(new DoubleLiteral(2.5));
                AreEqual(3,         eval.Eval("{}", ceiling.Lambda()));
                
                var floor   = new Floor(new DoubleLiteral(2.5));
                AreEqual(2,         eval.Eval("{}", floor.Lambda()));
                
                var exp     = new Exp(new DoubleLiteral(Math.Log(2)));
                AreEqual(2,         eval.Eval("{}", exp.Lambda()));
                
                var log     = new Log(new DoubleLiteral(Math.Exp(3)));
                AreEqual(3,         eval.Eval("{}", log.Lambda()));
                
                var sqrt    = new Sqrt(new DoubleLiteral(9));
                AreEqual(3,         eval.Eval("{}", sqrt.Lambda()));
                
                var negate  = new Negate(new DoubleLiteral(1));
                AreEqual(-1,        eval.Eval("{}", negate.Lambda()));
                

                // --- binary arithmetic operations
                var add      = new Add(new LongLiteral(1), new LongLiteral(2));
                AreEqual(3,         eval.Eval("{}", add.Lambda()));
                
                var subtract = new Subtract(new LongLiteral(1), new LongLiteral(2));
                AreEqual(-1,        eval.Eval("{}", subtract.Lambda()));
                
                var multiply = new Multiply(new LongLiteral(2), new LongLiteral(3));
                AreEqual(6,         eval.Eval("{}", multiply.Lambda()));
                
                var divide   = new Divide(new LongLiteral(10), new LongLiteral(2));
                AreEqual(5,         eval.Eval("{}", divide.Lambda()));
                
                
                // --- unary aggregate operations
                var min         = new Min(new Field(".children"), "child", new Field("child.age"));
                AreEqual(10,         eval.Eval(john, min.Lambda()));
                AreEqual(".children.Min(child => child.age)", min.ToString());
                
                var max         = new Max(new Field(".children"), "child", new Field("child.age"));
                AreEqual(11,         eval.Eval(john, max.Lambda()));
                AreEqual(".children.Max(child => child.age)", max.ToString());
                
                var sum         = new Sum(new Field(".children"), "child", new Field("child.age"));
                AreEqual(21,         eval.Eval(john, sum.Lambda()));
                AreEqual(".children.Sum(child => child.age)", sum.ToString());
                
                var average     = new Average(new Field(".children"), "child", new Field("child.age"));
                AreEqual(10.5,       eval.Eval(john, average.Lambda()));
                AreEqual(".children.Average(child => child.age)", average.ToString());
                
                var count       = new Count(new Field(".children"));
                AreEqual(2,          eval.Eval(john, count.Lambda()));
                AreEqual(".children.Count()", count.ToString());
            }
        }

        [Test]
        public static void TestQueryConversion() {

            // --- comparision operators
            var isEqual =           (Equal)             Operator.FromFilter((Person p) => p.name == "Peter");
            AreEqual(".name == 'Peter'", isEqual.ToString());
            
            var isNotEqual =        (NotEqual)          Operator.FromFilter((Person p) => p.name != "Peter");
            AreEqual(".name != 'Peter'", isNotEqual.ToString());

            var isLess =            (LessThan)          Operator.FromFilter((Person p) => p.age < 20);
            AreEqual(".age < 20", isLess.ToString());
            
            var isLessOrEqual =     (LessThanOrEqual)   Operator.FromFilter((Person p) => p.age <= 20);
            AreEqual(".age <= 20", isLessOrEqual.ToString());

            var isGreater =         (GreaterThan)       Operator.FromFilter((Person p) => p.age > 20);
            AreEqual(".age > 20", isGreater.ToString());
            
            var isGreaterOrEqual =  (GreaterThanOrEqual)Operator.FromFilter((Person p) => p.age >= 20);
            AreEqual(".age >= 20", isGreaterOrEqual.ToString());
            
            
            // --- group operators
            var or =    (Or)        Operator.FromFilter((Person p) => p.age >= 20 || p.name == "Peter");
            AreEqual(".age >= 20 || .name == 'Peter'", or.ToString());
            
            var and =   (And)       Operator.FromFilter((Person p) => p.age >= 20 && p.name == "Peter");
            AreEqual(".age >= 20 && .name == 'Peter'", and.ToString());
            
            var or2 =   (Or)        Operator.FromLambda((Person p) => p.age == 1 || p.age == 2 );
            AreEqual(".age == 1 || .age == 2", or2.ToString());
            
            var and2 =  (And)       Operator.FromLambda((Person p) => p.age == 1 && p.age == 2 );
            AreEqual(".age == 1 && .age == 2", and2.ToString());
            
            var or3 =   (Or)        Operator.FromLambda((Person p) => p.age == 1 || p.age == 2 || p.age == 3);
            AreEqual(".age == 1 || .age == 2 || .age == 3", or3.ToString());


            // --- unary operators
            var isNot = (Not)       Operator.FromFilter((Person p) => !(p.age >= 20));
            AreEqual("!(.age >= 20)", isNot.ToString());
                
            
            // --- quantifier operators
            var any =   (Any)       Operator.FromFilter((Person p) => p.children.Any(child => child.age == 20));
            AreEqual(".children.Any(child => child.age == 20)", any.ToString());
            
            var all =   (All)       Operator.FromFilter((Person p) => p.children.All(child => child.age == 20));
            AreEqual(".children.All(child => child.age == 20)", all.ToString());


            // --- literals
            var lng     = (LongLiteral)     Operator.FromLambda((object p) => 1);
            AreEqual("1",           lng.ToString());
            
            var dbl     = (DoubleLiteral)   Operator.FromLambda((object p) => 1.5);
            AreEqual("1.5",         dbl.ToString());

            var str     = (StringLiteral)   Operator.FromLambda((object p) => "hello");
            AreEqual("'hello'",     str.ToString());
            
            var @true   = (BoolLiteral)     Operator.FromLambda((object p) => true);
            AreEqual("true",        @true.ToString());

            var @null   = (NullLiteral)     Operator.FromLambda((object p) => null);
            AreEqual("null",        @null.ToString());

            
            // --- unary arithmetic operators
            var abs     = (Abs)     Operator.FromLambda((object p) => Math.Abs(-1));
            AreEqual("Abs(-1)", abs.ToString());
            
            var ceiling = (Ceiling) Operator.FromLambda((object p) => Math.Ceiling(2.5));
            AreEqual("Ceiling(2.5)", ceiling.ToString());
            
            var floor   = (Floor)   Operator.FromLambda((object p) => Math.Floor(2.5));
            AreEqual("Floor(2.5)", floor.ToString());
            
            var exp     = (Exp)     Operator.FromLambda((object p) => Math.Exp(2.5));
            AreEqual("Exp(2.5)", exp.ToString());
            
            var log     = (Log)     Operator.FromLambda((object p) => Math.Log(2.5));
            AreEqual("Log(2.5)", log.ToString());
            
            var sqrt    = (Sqrt)    Operator.FromLambda((object p) => Math.Sqrt(2.5));
            AreEqual("Sqrt(2.5)", sqrt.ToString());
            
            var negate  = (Negate)  Operator.FromLambda((object p) => -Math.Abs(-1));
            AreEqual("-(Abs(-1))", negate.ToString());
            
            var plus    = (Abs)     Operator.FromLambda((object p) => +Math.Abs(-1)); // + will be eliminated
            AreEqual("Abs(-1)", plus.ToString());
            
            
            // --- binary arithmetic operators
            var add         = (Add)     Operator.FromLambda((object p) => 1 + Math.Abs(1.0));
            AreEqual("1 + Abs(1)", add.ToString());

            var subtract    = (Subtract)Operator.FromLambda((object p) => 1 - Math.Abs(1.0));
            AreEqual("1 - Abs(1)", subtract.ToString());

            var multiply    = (Multiply)Operator.FromLambda((object p) => 1 * Math.Abs(1.0));
            AreEqual("1 * Abs(1)", multiply.ToString());

            var divide      = (Divide)  Operator.FromLambda((object p) => 1 / Math.Abs(1.0));
            AreEqual("1 / Abs(1)", divide.ToString());
            
            
            // --- unary aggregate operators
            var min      = (Min)  Operator.FromLambda((Person p) => p.children.Min(child => child.age));
            AreEqual(".children.Min(child => child.age)", min.ToString());
            
            var max      = (Max)  Operator.FromLambda((Person p) => p.children.Max(child => child.age));
            AreEqual(".children.Max(child => child.age)", max.ToString());

            var sum      = (Sum)  Operator.FromLambda((Person p) => p.children.Sum(child => child.age));
            AreEqual(".children.Sum(child => child.age)", sum.ToString());

            var count    = (Count)  Operator.FromLambda((Person p) => p.children.Count()); // () -> method call
            AreEqual(".children.Count()", count.ToString());
            
            var count2    = (Count)  Operator.FromLambda((Person p) => p.children.Count); // no () -> Count property 
            AreEqual(".children.Count()", count2.ToString());

            var average  = (Average)  Operator.FromLambda((Person p) => p.children.Average(child => child.age));
            AreEqual(".children.Average(child => child.age)", average.ToString());
        }
    }
}