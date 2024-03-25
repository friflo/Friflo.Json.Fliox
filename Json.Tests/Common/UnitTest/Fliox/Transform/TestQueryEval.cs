// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Cosmos;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static System.Math;

using Contains = Friflo.Json.Fliox.Transform.Query.Ops.Contains;

// ReSharper disable InconsistentNaming
// ReSharper disable CollectionNeverQueried.Global
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public class Person
    {
        public          string          name;
        public          int             age;
        public          Address         address;
        public readonly List<Person>    children = new List<Person>();
        public readonly List<Hobby>     hobbies = new List<Hobby>();
    }
    
    public class Hobby
    {
        public          string          name;
    }
    
    public class Address
    {
        public          Street          street;
        [Serialize                    ("city")]
        public          string          cityName;
    }
    
    public class Street
    {
        public          string          name;
        public          string          houseNumber;
    }

    public static class TestQueryEval
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
                    },
                    address = new Address {
                        street  = new Street {
                            name        = "Lombard St",
                            houseNumber = "11"
                        },
                        cityName = "San Francisco"
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
                var  isPeter         = new Filter("p", new Equal(new Field ("p.name"), new StringLiteral ("Peter")));
                AreEqual("p => p.name == 'Peter'",  isPeter.Linq);
                AreEqual("c['name'] = \"Peter\"",     isPeter.CosmosFilter());
                var  isPeter2        = JsonFilter.Create<Person>(p => p.name == "Peter");
                AreEqual("p => p.name == 'Peter'", isPeter2.Linq);
                
                bool IsPeter(Person p) => p.name == "Peter";

                var  isAgeGreater35Op = new Greater(new Field("p.age"), new LongLiteral(35));
                var  isAgeGreater35  = isAgeGreater35Op;
                bool IsAgeGreater35(Person p) => p.age > 35;
                
                var isNotAgeGreater35  = new Not(isAgeGreater35Op);
            
                IsTrue  (IsPeter(Peter));
                IsTrue  (eval.Filter(peter, isPeter));
                IsFalse (eval.Filter(john,  isPeter));
                
                IsTrue  (IsAgeGreater35(Peter));
                IsTrue  (eval.Filter(peter, isAgeGreater35));
                IsFalse (eval.Filter(john,  isAgeGreater35));
                // Not
                IsFalse (eval.Filter(peter, isNotAgeGreater35));
                IsTrue  (eval.Filter(john,  isNotAgeGreater35));
                
                var  equalUnknownField         = new Equal(new Field ("p.unknown"), new StringLiteral ("SomeString"));
                IsFalse(eval.Filter(john, equalUnknownField));

                // --- Any
                var hasChildPaul = new Any (new Field ("p.children"), "child", new Equal (new Field ("child.name"), new StringLiteral ("Paul")));
                bool HasChildPaul(Person p) => p.children.Any(child => child.name == "Paul");
                
                var hasChildAgeLess12 = new Any (new Field ("p.children"), "child", new Less (new Field ("child.age"), new LongLiteral (12)));
                
                IsTrue (HasChildPaul(Peter));
                IsTrue (eval.Filter(peter, hasChildPaul));
                IsFalse(eval.Filter(john,  hasChildPaul));
                
                IsFalse(eval.Filter(peter, hasChildAgeLess12));
                IsTrue (eval.Filter(john,  hasChildAgeLess12));

                var  anyEqualUnknownField  = new Any (new Field ("p.children"), "child", new Equal (new Field ("child.unknown"), new StringLiteral ("SomeString")));
                IsFalse(eval.Filter(john, anyEqualUnknownField));

                // --- All
                var allChildAgeEquals20 = new All (new Field ("p.children"), "child", new Equal(new Field ("child.age"), new LongLiteral (20)));
                IsTrue (eval.Filter(peter, allChildAgeEquals20));
                IsFalse(eval.Filter(john,  allChildAgeEquals20));
                
                var  allEqualUnknownField  = new All (new Field ("p.children"), "child", new Equal (new Field ("child.unknown"), new StringLiteral ("SomeString")));
                IsFalse(eval.Filter(john, allEqualUnknownField));
                
                // --- Count() with lambda parameter -> is not a Filter
                var countChildAgeEquals20 = new Lambda ("p", new CountWhere (new Field ("p.children"), "child", new Equal(new Field ("child.age"), new LongLiteral (20)))).Lambda();
                AreEqual(2, eval.Eval(peter, countChildAgeEquals20));
                AreEqual(0, eval.Eval(john,  countChildAgeEquals20));

                // --- test with arithmetic operations
                var  isAge40  = new Equal(new Field ("p.age"), new Add(new LongLiteral (35), new LongLiteral(5)));
                IsTrue  (eval.Filter(peter, isAge40));
                
                // var  isChildAge20  = new Equal(new Field (".children[*].age"), new Add(new LongLiteral (15), new LongLiteral(5))).Filter();
                var isChildAge20 = new Filter("p", new All(new Field ("p.children"), "child", new Equal(new Field ("child.age"), new LongLiteral (20))));
                var isChildAge20Expect = Operation.FromFilter((Person p) => p.children.All(child => child.age == 20));
                AreEqual(isChildAge20Expect.Linq, isChildAge20.Linq);
                IsTrue  (eval.Filter(peter, isChildAge20));
                
                
                
                // ------------------------------ Test runtime assertions ------------------------------
                Exception e;
                // --- compare operations must not be reused
                e = Throws<InvalidOperationException>(() => _ = new JsonFilter(new Filter("p", new Equal(isAgeGreater35Op, isAgeGreater35Op))));
                NotNull(e);
                AreEqual("Used operation instance is not applicable for reuse. Use a clone. Type: Greater, instance: p.age > 35", e.Message);
                
                // --- group operations must not be reused
                var testGroupOp = new And(new List<FilterOperation> {new Equal(new StringLiteral("A"), new StringLiteral("B"))});
                e = Throws<InvalidOperationException>(() => _ = new JsonFilter(new Equal(testGroupOp, testGroupOp)));
                NotNull(e);
                AreEqual("Used operation instance is not applicable for reuse. Use a clone. Type: And, instance: 'A' == 'B'", e.Message);

                // --- literal and field operations are applicable for reuse
                var testLiteral = new StringLiteral("Test");
                var reuseLiterals = new Equal(testLiteral, testLiteral);
                eval.Filter(peter, reuseLiterals);
                
                var testField = new Field ("p.name");
                var reuseField = new Equal(testField, testField);
                eval.Filter(peter, reuseField);
            }
        }

        // [Test]
        public static void NoAllocFilter() {
            var memLog = new MemoryLogger(10, 10, MemoryLog.Enabled);
            using (var eval = new JsonEvaluator())
            using (var jsonMapper = new ObjectMapper()) {
                jsonMapper.Pretty   = true;
                var peter           = jsonMapper.WriteAsValue(Peter);
                
                var anyChildAgeWithin10And20 = JsonFilter.Create<Person>(p => p.children.All(child => child.age >= 20 && child.age <= 20));
                bool result = false;
                for (int n = 0; n < 100; n++) {
                    result = eval.Filter(peter, anyChildAgeWithin10And20, out _);
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
                // var  hasChildHobbySurfing = new Any (new Field (".children"), "child", new Equal (new Field ("child.hobbies[*].name"), new StringLiteral ("Surfing"))).Filter();
                var hasHobbySurfing      = new Any (new Field("child.hobbies"), "hobby", new Equal(new Field("hobby.name"), new StringLiteral ("Surfing")));
                var hasChildHobbySurfing = new Filter("p", new Any (new Field ("p.children"), "child", hasHobbySurfing));
                var hasChildHobbySurfingExp = Filter((Person p) => p.children.Any(child => child.hobbies.Any(hobby => hobby.name == "Surfing")), out string exp);
                AreEqual("p => p.children.Any(child => child.hobbies.Any(hobby => (hobby.name == 'Surfing')))", exp);
                AreEqual("p => p.children.Any(child => child.hobbies.Any(hobby => hobby.name == 'Surfing'))", hasChildHobbySurfing.Linq);
                
                IsTrue (hasChildHobbySurfingExp(Peter));
                IsFalse(hasChildHobbySurfingExp(John));
                
                IsTrue (eval.Filter(peter, hasChildHobbySurfing));
                IsFalse(eval.Filter(john,  hasChildHobbySurfing));
            }
        }

        private static Func<T, bool> Filter<T>(Expression<Func<T, bool>> filter, out string name) {
            name = filter.ToString();
            name = name.Replace('"', '\'');
            return filter.Compile();
        }

        [Test]
        public static void TestEval() {
          using (var eval     = new JsonEvaluator())
          using (var mapper   = new ObjectMapper())
          {
            mapper.Pretty = true;
            var john  = mapper.Write(John);
            mapper.Pretty = false;

            // --- use expression
            AreEqual("hello",   eval.Eval("{}", JsonLambda.Create<object>(p => "hello")));
            
            // --- nullary operation (boolean literals)
            {
                var @true   = new TrueLiteral();
                AssertJson(mapper, @true, "{'op':'true'}");
                AreEqual(true,   eval.Eval("{}", @true.Lambda()));
            } {
                var @false   = new FalseLiteral();
                AssertJson(mapper, @false, "{'op':'false'}");
                AreEqual(false,  eval.Eval("{}", @false.Lambda()));
            }
            
            // --- unary literal operations
            {
                var stringLiteral   = new StringLiteral("hello");
                AssertJson(mapper, stringLiteral, "{'op':'string','value':'hello'}");
                AreEqual("hello",   eval.Eval("{}", stringLiteral.Lambda()));
            } {
                var doubleLiteral   = new DoubleLiteral(42.0);
                AssertJson(mapper, doubleLiteral, "{'op':'double','value':42}");
                AreEqual(42.0,      eval.Eval("{}", doubleLiteral.Lambda()));
            } {
                var longLiteral   = new LongLiteral(42);
                AssertJson(mapper, longLiteral, "{'op':'int64','value':42}");
                AreEqual(42.0,      eval.Eval("{}", longLiteral.Lambda()));
            } {
                var nullLiteral     = new NullLiteral();
                AssertJson(mapper, nullLiteral, "{'op':'null'}");
                AreEqual(null,      eval.Eval("{}", nullLiteral.Lambda()));
            } 
            
            // --- unary arithmetic operations
            {
                var abs     = new Abs(new LongLiteral(-2));
                AssertJson(mapper, abs, "{'op':'abs','value':{'op':'int64','value':-2}}");
                AreEqual(2,         eval.Eval("{}", abs.Lambda()));
            } {
                var ceiling = new Ceiling(new DoubleLiteral(2.5));
                AssertJson(mapper, ceiling, "{'op':'ceiling','value':{'op':'double','value':2.5}}");
                AreEqual(3,         eval.Eval("{}", ceiling.Lambda()));
            } {
                var floor   = new Floor(new DoubleLiteral(2.5));
                AssertJson(mapper, floor, "{'op':'floor','value':{'op':'double','value':2.5}}");
                AreEqual(2,         eval.Eval("{}", floor.Lambda()));
            } {
                var exp     = new Exp(new DoubleLiteral(Log(E)));
                AssertJson(mapper, exp, "{'op':'exp','value':{'op':'double','value':1}}");
                AreEqual(2.7182818284590451d,         eval.Eval("{}", exp.Lambda()));
            } {
                var log     = new Log(new DoubleLiteral(1));
                AssertJson(mapper, log, "{'op':'log','value':{'op':'double','value':1}}");
                AreEqual(0.0d,         eval.Eval("{}", log.Lambda()));
            } {
                var sqrt    = new Sqrt(new DoubleLiteral(9));
                AssertJson(mapper, sqrt, "{'op':'sqrt','value':{'op':'double','value':9}}");
                AreEqual(3,         eval.Eval("{}", sqrt.Lambda()));
            } {
                var negate  = new Negate(new DoubleLiteral(1));
                AssertJson(mapper, negate, "{'op':'negate','value':{'op':'double','value':1}}");
                AreEqual(-1,        eval.Eval("{}", negate.Lambda()));
            }
            
            // --- binary arithmetic operations
            {
                var add      = new Add(new LongLiteral(1), new LongLiteral(2));
                AssertJson(mapper, add, "{'op':'add','left':{'op':'int64','value':1},'right':{'op':'int64','value':2}}");
                AreEqual(3,         eval.Eval("{}", add.Lambda()));
            } {
                var subtract = new Subtract(new LongLiteral(1), new LongLiteral(2));
                AssertJson(mapper, subtract, "{'op':'subtract','left':{'op':'int64','value':1},'right':{'op':'int64','value':2}}");
                AreEqual(-1,        eval.Eval("{}", subtract.Lambda()));
            } {
                var multiply = new Multiply(new LongLiteral(2), new LongLiteral(3));
                AssertJson(mapper, multiply, "{'op':'multiply','left':{'op':'int64','value':2},'right':{'op':'int64','value':3}}");
                AreEqual(6,         eval.Eval("{}", multiply.Lambda()));
            } {
                var divide   = new Divide(new LongLiteral(10), new LongLiteral(2));
                AssertJson(mapper, divide, "{'op':'divide','left':{'op':'int64','value':10},'right':{'op':'int64','value':2}}");
                AreEqual(5,         eval.Eval("{}", divide.Lambda()));
            } {
                var modulo   = new Modulo(new LongLiteral(7), new LongLiteral(3));
                AssertJson(mapper, modulo, "{'op':'modulo','left':{'op':'int64','value':7},'right':{'op':'int64','value':3}}");
                AreEqual(1,         eval.Eval("{}", modulo.Lambda()));
            }
            
            // --- unary aggregate operations
            {
                var min         = new Lambda("o", new Min(new Field("o.children"), "child", new Field("child.age")));
                AssertJson(mapper, min, "{'op':'lambda','arg':'o','body':{'op':'min','field':{'name':'o.children'},'arg':'child','array':{'op':'field','name':'child.age'}}}");
                AreEqual(10,         eval.Eval(john, min.Lambda()));
                AreEqual("o => o.children.Min(child => child.age)", min.Linq);
            } {
                var max         = new Lambda("o", new Max(new Field("o.children"), "child", new Field("child.age")));
                AssertJson(mapper, max, "{'op':'lambda','arg':'o','body':{'op':'max','field':{'name':'o.children'},'arg':'child','array':{'op':'field','name':'child.age'}}}");
                AreEqual(11,         eval.Eval(john, max.Lambda()));
                AreEqual("o => o.children.Max(child => child.age)", max.Linq);
            } {
                var sum         = new Lambda("o", new Sum(new Field("o.children"), "child", new Field("child.age")));
                AssertJson(mapper, sum, "{'op':'lambda','arg':'o','body':{'op':'sum','field':{'name':'o.children'},'arg':'child','array':{'op':'field','name':'child.age'}}}");
                AreEqual(21,         eval.Eval(john, sum.Lambda()));
                AreEqual("o => o.children.Sum(child => child.age)", sum.Linq);
            } {
                var average     = new Lambda("o", new Average(new Field("o.children"), "child", new Field("child.age")));
                AssertJson(mapper, average, "{'op':'lambda','arg':'o','body':{'op':'average','field':{'name':'o.children'},'arg':'child','array':{'op':'field','name':'child.age'}}}");
                AreEqual(10.5,       eval.Eval(john, average.Lambda()));
                AreEqual("o => o.children.Average(child => child.age)", average.Linq);
            } {
                var count       = new Lambda("o", new Count(new Field("o.children")));
                AssertJson(mapper, count, "{'op':'lambda','arg':'o','body':{'op':'count','field':{'name':'o.children'}}}");
                AreEqual(2,          eval.Eval(john, count.Lambda()));
                AreEqual("o => o.children.Count()", count.Linq);
            }
            
            // --- binary string operations
            {
                var contains     = new Contains(new StringLiteral("12345"), new StringLiteral("234"));
                AssertJson(mapper, contains, "{'op':'contains','left':{'op':'string','value':'12345'},'right':{'op':'string','value':'234'}}");
                AreEqual(true,         eval.Eval("{}", contains.Lambda()));
            } {
                var startsWith     = new StartsWith(new StringLiteral("12345"), new StringLiteral("123"));
                AssertJson(mapper, startsWith, "{'op':'startsWith','left':{'op':'string','value':'12345'},'right':{'op':'string','value':'123'}}");
                AreEqual(true,         eval.Eval("{}", startsWith.Lambda()));
            } {
                var endsWith     = new EndsWith(new StringLiteral("12345"), new StringLiteral("345"));
                AssertJson(mapper, endsWith, "{'op':'endsWith','left':{'op':'string','value':'12345'},'right':{'op':'string','value':'345'}}");
                AreEqual(true,         eval.Eval("{}", endsWith.Lambda()));
            }
          }
        }

        internal static void AssertJson(ObjectMapper mapper, Operation op, string json) {
            var result = mapper.Write(op);
            var singleQuoteResult = result.Replace('\"', '\'');
            if (json != singleQuoteResult) {
                Fail($"Expected: {json}\nBut was:  {singleQuoteResult}");
            }
            // assert mapping of JSON string to/from Operation
            Operation opRead  = mapper.Read<Operation>(result);
            string   opWrite = mapper.Write(opRead);
            AreEqual(result, opWrite);
            
            // assert mapping of JSON string to/from FilterOperation
            if (opRead is FilterOperation) {
                FilterOperation boolOpRead  = mapper.Read<FilterOperation>(result);
                string boolOpWrite = mapper.Write(boolOpRead);
                AreEqual(result, boolOpWrite);
            }
        }
    }
}