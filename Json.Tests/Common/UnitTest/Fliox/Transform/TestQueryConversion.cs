// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Friflo.Json.Fliox.Hub.Cosmos;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;
using NUnit.Framework;

using static NUnit.Framework.Assert;
using static System.Math;
using static Friflo.Json.Tests.Common.UnitTest.Fliox.Transform.TestQueryEval;
using Contains = Friflo.Json.Fliox.Transform.Query.Ops.Contains;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public static class TestQueryConversion {
        // --------------------------------------------------------------------------------------------------
        private static FilterOperation FromFilter<T>(Expression<Func<T, bool>> filter) {
            var lambda = (Filter)Operation.FromFilter(filter);
            return lambda.body;
        }
        
        private static Operation FromLambda<T>(Expression<Func<T, object>> filter) {
            var lambda = (Lambda)Operation.FromLambda(filter);
            return lambda.body;
        }
        
        [Test]
        public static void TestQueryConversion_Compare() {
          using (var mapper   = new ObjectMapper()) {
            // --- comparision operations
            {
                var isEqual =           (Equal)             FromFilter((Person p) =>
                          p.name == "Peter");
                AreEqual("p.name == 'Peter'",   isEqual.Linq);
                AreEqual("p['name'] = \"Peter\"", isEqual.CosmosFilter());
                AssertJson(mapper, isEqual, "{'op':'equal','left':{'op':'field','name':'p.name'},'right':{'op':'string','value':'Peter'}}");
            } {
                var isNotEqual =        (NotEqual)          FromFilter((Person p) =>
                          p.name != "Peter");
                AreEqual("p.name != 'Peter'",   isNotEqual.Linq);
                AreEqual("(NOT IS_DEFINED(p['name']) or NOT IS_DEFINED(\"Peter\") or p['name'] != \"Peter\")",isNotEqual.CosmosFilter());
                AssertJson(mapper, isNotEqual, "{'op':'notEqual','left':{'op':'field','name':'p.name'},'right':{'op':'string','value':'Peter'}}");
            } {
                var isLess =            (Less)          FromFilter((Person p) =>
                          p.age < 20);
                AreEqual("p.age < 20",          isLess.Linq);
                AreEqual("p['age'] < 20",       isLess.CosmosFilter());
                AssertJson(mapper, isLess, "{'op':'less','left':{'op':'field','name':'p.age'},'right':{'op':'int64','value':20}}");
            } {            
                var isLessOrEqual =     (LessOrEqual)   FromFilter((Person p) =>
                          p.age <= 20);
                AreEqual("p.age <= 20",         isLessOrEqual.Linq);
                AreEqual("p['age'] <= 20",      isLessOrEqual.CosmosFilter());
                AssertJson(mapper, isLessOrEqual, "{'op':'lessOrEqual','left':{'op':'field','name':'p.age'},'right':{'op':'int64','value':20}}");
            } {
                var isGreater =         (Greater)       FromFilter((Person p) =>
                          p.age > 20);
                AreEqual("p.age > 20",          isGreater.Linq);
                AreEqual("p['age'] > 20",       isGreater.CosmosFilter());
                AssertJson(mapper, isGreater, "{'op':'greater','left':{'op':'field','name':'p.age'},'right':{'op':'int64','value':20}}");
            } {            
                var isGreaterOrEqual =  (GreaterOrEqual)FromFilter((Person p) =>
                          p.age >= 20);
                AreEqual("p.age >= 20",         isGreaterOrEqual.Linq);
                AreEqual("p['age'] >= 20",      isGreaterOrEqual.CosmosFilter());
                AssertJson(mapper, isGreaterOrEqual, "{'op':'greaterOrEqual','left':{'op':'field','name':'p.age'},'right':{'op':'int64','value':20}}");
            }
          }
        }
        
        [Test]
        public static void TestQueryConversion_Logical() {
            using (var mapper   = new ObjectMapper()) {
            // --- group operations
            {
                var or =    (Or)        FromFilter((Person p) =>
                          p.age >= 20 || p.name == "Peter");
                AreEqual("p.age >= 20 || p.name == 'Peter'",        or.Linq);
                AreEqual("p['age'] >= 20 OR p['name'] = \"Peter\"",   or.CosmosFilter());
                AssertJson(mapper, or, "{'op':'or','operands':[{'op':'greaterOrEqual','left':{'op':'field','name':'p.age'},'right':{'op':'int64','value':20}},{'op':'equal','left':{'op':'field','name':'p.name'},'right':{'op':'string','value':'Peter'}}]}");
            } {            
                var and =   (And)       FromFilter((Person p) =>
                          p.age >= 20 && p.name == "Peter");
                AreEqual("p.age >= 20 && p.name == 'Peter'",        and.Linq);
                AreEqual( "p['age'] >= 20 AND p['name'] = \"Peter\"", and.CosmosFilter());
                AssertJson(mapper, and, "{'op':'and','operands':[{'op':'greaterOrEqual','left':{'op':'field','name':'p.age'},'right':{'op':'int64','value':20}},{'op':'equal','left':{'op':'field','name':'p.name'},'right':{'op':'string','value':'Peter'}}]}");
            } {            
                var or2 =   (Or)        FromLambda((Person p) =>
                          p.age == 1 || p.age == 2 );
                AreEqual("p.age == 1 || p.age == 2",            or2.Linq);
                AreEqual("p['age'] = 1 OR p['age'] = 2",        or2.CosmosFilter());
                AssertJson(mapper, or2, "{'op':'or','operands':[{'op':'equal','left':{'op':'field','name':'p.age'},'right':{'op':'int64','value':1}},{'op':'equal','left':{'op':'field','name':'p.age'},'right':{'op':'int64','value':2}}]}");
            } {            
                var and2 =  (And)       FromLambda((Person p) =>
                          p.age == 1 && p.age == 2 );
                AreEqual("p.age == 1 && p.age == 2",            and2.Linq);
                AreEqual("p['age'] = 1 AND p['age'] = 2",       and2.CosmosFilter());
                AssertJson(mapper, and2, "{'op':'and','operands':[{'op':'equal','left':{'op':'field','name':'p.age'},'right':{'op':'int64','value':1}},{'op':'equal','left':{'op':'field','name':'p.age'},'right':{'op':'int64','value':2}}]}");
            } { 
                var or3 =   (Or)        FromLambda((Person p) =>
                          p.age == 1 || p.age == 2 || p.age == 3);
                AreEqual("p.age == 1 || p.age == 2 || p.age == 3",          or3.Linq);
                AreEqual("p['age'] = 1 OR p['age'] = 2 OR p['age'] = 3",    or3.CosmosFilter());
                AssertJson(mapper, or3, "{'op':'or','operands':[{'op':'or','operands':[{'op':'equal','left':{'op':'field','name':'p.age'},'right':{'op':'int64','value':1}},{'op':'equal','left':{'op':'field','name':'p.age'},'right':{'op':'int64','value':2}}]},{'op':'equal','left':{'op':'field','name':'p.age'},'right':{'op':'int64','value':3}}]}");
            }
            
            // --- unary operations
            {
                var isNot = (Not)       FromFilter((Person p) =>
                          !(p.age >= 20));
                AreEqual("!(p.age >= 20)",          isNot.Linq);
                AreEqual("NOT(p['age'] >= 20)",     isNot.CosmosFilter());
                AssertJson(mapper, isNot, "{'op':'not','operand':{'op':'greaterOrEqual','left':{'op':'field','name':'p.age'},'right':{'op':'int64','value':20}}}");
            }
          }
        }
        
        [Test]
        public static void TestQueryConversion_Quantify() {
            using (var mapper   = new ObjectMapper()) {
            // --- quantifier operations
            {
                var any =   (Any)       FromFilter((Person p) =>
                          p.children.Any(child => child.age == 20));
                AreEqual("p.children.Any(child => child.age == 20)",                                        any.Linq);
                AreEqual("EXISTS(SELECT VALUE child FROM child IN p['children'] WHERE child['age'] = 20)",  any.CosmosFilter());
                AssertJson(mapper, any, "{'op':'any','field':{'name':'p.children'},'arg':'child','predicate':{'op':'equal','left':{'op':'field','name':'child.age'},'right':{'op':'int64','value':20}}}");
            } { 
                var all =   (All)       FromFilter((Person p) =>
                          p.children.All(child => child.age == 20));
                AreEqual("p.children.All(child => child.age == 20)", all.Linq);
                AreEqual("IS_NULL(p['children']) OR NOT IS_DEFINED(p['children']) OR (SELECT VALUE Count(1) FROM child IN p['children'] WHERE child['age'] = 20) = ARRAY_LENGTH(p['children'])", all.CosmosFilter());
                AssertJson(mapper, all, "{'op':'all','field':{'name':'p.children'},'arg':'child','predicate':{'op':'equal','left':{'op':'field','name':'child.age'},'right':{'op':'int64','value':20}}}");
            }
          }
        }
        
        [Test]
        public static void TestQueryConversion_Literals() {
            // --- literals
            {
                var lng     = (LongLiteral)     FromLambda((object p) =>
                          1);
                AreEqual("1",           lng.Linq);
            } { 
                var dbl     = (DoubleLiteral)   FromLambda((object p) =>
                          1.5);
                AreEqual("1.5",         dbl.Linq);
            } {
                var str     = (StringLiteral)   FromLambda((object p) =>
                          "hello");
                AreEqual("'hello'",     str.Linq);
            } {
                var str     = (StringLiteral)   FromLambda((object p) =>
                          'C'); // char - not a string
                AreEqual("'C'",         str.Linq);
            } {
                var @null   = (NullLiteral)     FromLambda((object p) =>
                          null);
                AreEqual("null",        @null.Linq);
            }
        }
        
        [Test]
        public static void TestQueryConversion_Arithmetic() {
            // --- unary arithmetic operations
            {
                var abs     = (Abs)     FromLambda((object p) =>
                          Abs(-1));
                AreEqual("Abs(-1)", abs.Linq);
            } { 
                var ceiling = (Ceiling) FromLambda((object p) =>
                          Ceiling(2.5));
                AreEqual("Ceiling(2.5)", ceiling.Linq);
            } { 
                var floor   = (Floor)   FromLambda((object p) =>
                          Floor(2.5));
                AreEqual("Floor(2.5)", floor.Linq);
            } { 
                var exp     = (Exp)     FromLambda((object p) =>
                          Exp(2.5));
                AreEqual("Exp(2.5)", exp.Linq);
            } { 
                var log     = (Log)     FromLambda((object p) =>
                          Log(2.5));
                AreEqual("Log(2.5)", log.Linq);
            } { 
                var sqrt    = (Sqrt)    FromLambda((object p) =>
                          Sqrt(2.5));
                AreEqual("Sqrt(2.5)", sqrt.Linq);
            } { 
                var negate  = (Negate)  FromLambda((object p) =>
                          -Abs(-1));
                AreEqual("-(Abs(-1))", negate.Linq);
            } { 
                var plus    = (Abs)     FromLambda((object p) =>
                         +Abs(-1)); // + will be eliminated
                AreEqual("Abs(-1)", plus.Linq);
            }
            
            // --- binary arithmetic operations
            {
                var add         = (Add)     FromLambda((object p) =>
                          1 + Abs(1.0));
                AreEqual("1 + Abs(1)", add.Linq);
            } {
                var subtract    = (Subtract)FromLambda((object p) =>
                          1 - Abs(1.0));
                AreEqual("1 - Abs(1)", subtract.Linq);
            } {
                var multiply    = (Multiply)FromLambda((object p) =>
                          1 * Abs(1.0));
                AreEqual("1 * Abs(1)", multiply.Linq);
            } {
                var divide      = (Divide)  FromLambda((object p) =>
                          1 / Abs(1.0));
                AreEqual("1 / Abs(1)", divide.Linq);
            } 
        }
        
        [Test]
        public static void TestQueryConversion_Aggregate() {
            // --- unary aggregate operations
            {
                var min      = (Min)  FromLambda((Person p) =>
                          p.children.Min(child => child.age));
                AreEqual("p.children.Min(child => child.age)", min.Linq);
            } { 
                var max      = (Max)  FromLambda((Person p) =>
                          p.children.Max(child => child.age));
                AreEqual("p.children.Max(child => child.age)", max.Linq);
            } {
                var sum      = (Sum)  FromLambda((Person p) =>
                          p.children.Sum(child => child.age));
                AreEqual("p.children.Sum(child => child.age)", sum.Linq);
            } {
                var count    = (CountWhere)  FromLambda((Person p) =>
                          p.children.Count(child => child.age == 20));
                AreEqual("p.children.Count(child => child.age == 20)", count.Linq);
            } {
                var count    = (Count)  FromLambda((Person p) =>
                          p.children.Count()); // () -> method call
                AreEqual("p.children.Count()", count.Linq);
            } { 
                var count2   = (Count)  FromLambda((Person p) =>
                          p.children.Count); // no () -> Count property 
                AreEqual("p.children.Count()", count2.Linq);
            } {
                var average  = (Average)  FromLambda((Person p) =>
                          p.children.Average(child => child.age));
                AreEqual("p.children.Average(child => child.age)", average.Linq);
            }
        }
        
        [Test]
        public static void TestQueryConversion_String() {
            // --- binary string operations
            {
                var contains      = (Contains)  FromFilter((object p) =>
                          "12345".Contains("234"));
                AreEqual("'12345'.Contains('234')", contains.Linq);
            } {
                var startsWith    = (StartsWith)  FromFilter((object p) =>
                          "12345".StartsWith("123"));
                AreEqual("'12345'.StartsWith('123')", startsWith.Linq);
            } {
                var endsWith      = (EndsWith)  FromFilter((object p) =>
                          "12345".EndsWith("345"));
                AreEqual("'12345'.EndsWith('345')", endsWith.Linq);
            }
            // --- unary string operations
            {
                var isEqual = (Equal)  FromFilter((object p) =>
                          "12345".Length == 5);
                AreEqual("5 == 5",  isEqual.Linq);
                AreEqual("5 = 5",   isEqual.CosmosFilter());
            } {
                var isEqual =     (Equal)   FromFilter((Person p) =>
                          p.name.Length == 5);
                AreEqual("p.name.Length() == 5",    isEqual.Linq);
                AreEqual("LENGTH(p['name']) = 5",   isEqual.CosmosFilter());
            }
        }
        
        [Test]
        public static void TestQueryConversion_StringEscape() {
            {
                var filter      = FromFilter((object p) =>
                          "\'".Contains("foo"));
                AreEqual("'\''.Contains('foo')", filter.Linq);
            } {
                var filter      = FromFilter((object p) =>
                          "\b".Contains("foo"));
                AreEqual("'\\b'.Contains('foo')", filter.Linq);
            } {
                var filter      = FromFilter((object p) =>
                          "\f".Contains("foo"));
                AreEqual("'\\f'.Contains('foo')", filter.Linq);
            } {
                var filter      = FromFilter((object p) =>
                          "\n".Contains("foo"));
                AreEqual("'\\n'.Contains('foo')", filter.Linq);
            } {
                var filter      = FromFilter((object p) =>
                          "\r".Contains("foo"));
                AreEqual("'\\r'.Contains('foo')", filter.Linq);
            } {
                var filter      = FromFilter((object p) =>
                          "\t".Contains("foo"));
                AreEqual("'\\t'.Contains('foo')", filter.Linq);
            } {
                var filter      = FromFilter((object p) =>
                          "\v".Contains("foo"));
                AreEqual("'\\v'.Contains('foo')", filter.Linq);
            }
        }
        
        [Test]
        public static void TestField() {
            {
                var is20 =          (Equal)          FromFilter((Person p) =>
                          p.age == 20);
                AreEqual("p.age == 20", is20.Linq);
            } {
                var isSf =          (Equal)          FromFilter((Person p) =>
                          p.address.cityName == "San Francisco");
                AreEqual("p.address.city == 'San Francisco'", isSf.Linq);
            } {
                var isLombardSt =   (Equal)          FromFilter((Person p) =>
                          p.address.street.name == "Lombard St");
                AreEqual("p.address.street.name == 'Lombard St'", isLombardSt.Linq);
            }
        }
        
        // ------------------------------- test access to constant lambda parameters -------------------------------
        // Test access to variables defined outside lambda scope.
        // The values of these variables need to be evaluated directly and their values are used inside the
        // generated Operation.
        internal class TestClassFields
        {
            internal    int                 field_int;
            internal    bool                field_bool;
            internal    string              field_str;
            internal    TestClassFields     Prop_sub1       { get; set; }
        
            internal    int                 Prop_int        { get; set; }
            internal    TestClassFields     Prop_sub2       { get; set; }
        
            internal    int                 GetInt()        => field_int;
            internal    int                 Prop_Exception  => throw new InvalidOperationException("test property exception");
        }
        
        // ReSharper disable once ConvertToConstant.Local
        private static readonly int             FourStatic    =  4;
        private static          int             FiveStatic()  => 5;
        private static readonly TestClassFields TestStaticNull = null;
        private static readonly TestClassFields TestStatic = new TestClassFields {
                                                    field_int = 1, Prop_int = 11, field_bool = true, field_str = "foo",
                Prop_sub1 =   new TestClassFields { field_int = 2, Prop_int = 12, field_bool = true, field_str = "bar"},
                Prop_sub2 =   new TestClassFields { field_int = 3, Prop_int = 13, field_bool = true, field_str = "xyz"}   
        };
        
#pragma warning disable CS0649
        class TestObject {
            internal    string      str;
            internal    int?        nullInt;
            internal    string[]    array;
        }
        
        [Test]
        public static void TestLambdaNullableValue() {
            AreEqual("p => p.nullInt", JsonLambda.Create<TestObject>(p => p.nullInt.Value).Linq);
        }
        
        [Test]
        public static void TestLambdaMethods() {
            var         test    = TestStatic;
            AreEqual("p => 3",              JsonLambda.Create<object>    (p => test.field_str.Length).Linq);
            AreEqual("p => p.str.Length()", JsonLambda.Create<TestObject>(p => p.str.Length).Linq);
            AreEqual("p => 1",              JsonLambda.Create<object>    (p => test.GetInt()).Linq);
        }
        
        [Test]
        public static void TestLambdaMethodNullReferenceException() {
            TestClassFields test = null;
            var e = Throws<NullReferenceException>(() => {
                JsonLambda.Create<object>    (p => test.field_str.Length);
            });
            AreEqual("member: field_str, type: TestClassFields", e.Message);
            
            e = Throws<NullReferenceException>(() => {
                JsonLambda.Create<object>    (p => TestStaticNull.field_int);
            });
            AreEqual("member: field_int, type: TestClassFields", e.Message);
        }
        
        [Test]
        public static void TestLambdaPropertyException() {
            TestClassFields test = TestStatic;
            var e = Throws<TargetInvocationException>(() => {
                JsonLambda.Create<object>    (p => test.Prop_Exception);
            });
            AreEqual("property: Prop_Exception, type: TestClassFields", e.Message);
        }
        
        [Test]
        public static void TestLambdaMethodTargetInvocationException() {
            TestClassFields test = null;
            var e = Throws<TargetInvocationException>(() => {
                JsonLambda.Create<object>    (p => test.GetInt());
            });
            AreEqual("method: GetInt, type: TestClassFields", e.Message);
        }
        
        [Test]
        public static void TestLambdaStaticReference() {
            AreEqual("p => 3",              JsonLambda.Create<object>    (p => TestStatic.field_str.Length).Linq);
            AreEqual("p => 1",              JsonLambda.Create<object>    (p => TestStatic.GetInt()).Linq);
        }
        
        [Test]
        public static void TestDev() {
        }

        [Test]
        public static void TestLambdaParameters() {
            // ReSharper disable once ConvertToConstant.Local
            var         two     = 2;
            const int   three   = 3;
            var         test    = TestStatic;
            
            AreEqual("p => 1",      JsonLambda.Create<object>(p => 1).Linq);
            AreEqual("p => 2",      JsonLambda.Create<object>(p => two).Linq);
            AreEqual("p => 3",      JsonLambda.Create<object>(p => three).Linq);
            AreEqual("p => 4",      JsonLambda.Create<object>(p => FourStatic).Linq);
            AreEqual("p => 5",      JsonLambda.Create<object>(p => FiveStatic()).Linq);
            
            AreEqual("p => 1",      JsonLambda.Create<object>(p => test.field_int).Linq);
            AreEqual("p => 1",      JsonLambda.Create<object>(p => test.GetInt()).Linq);
            AreEqual("p => 11",     JsonLambda.Create<object>(p => test.Prop_int).Linq);
            AreEqual("p => true",   JsonLambda.Create<object>(p => test.field_bool).Linq);
            AreEqual("p => 'foo'",  JsonLambda.Create<object>(p => test.field_str).Linq);
            
            AreEqual("p => 2",      JsonLambda.Create<object>(p => test.Prop_sub1.field_int).Linq);
            AreEqual("p => 12",     JsonLambda.Create<object>(p => test.Prop_sub1.Prop_int).Linq);
            AreEqual("p => true",   JsonLambda.Create<object>(p => test.Prop_sub1.field_bool).Linq);
            AreEqual("p => 'bar'",  JsonLambda.Create<object>(p => test.Prop_sub1.field_str).Linq);
            
            AreEqual("p => 3",      JsonLambda.Create<object>(p => test.Prop_sub2.field_int).Linq);
            AreEqual("p => 13",     JsonLambda.Create<object>(p => test.Prop_sub2.Prop_int).Linq);
            AreEqual("p => true",   JsonLambda.Create<object>(p => test.Prop_sub2.field_bool).Linq);
            AreEqual("p => 'xyz'",  JsonLambda.Create<object>(p => test.Prop_sub2.field_str).Linq);
        }
        
        [Test]
        public static void TestUnsupportedLinqMethods() {
            var e = Throws<NotSupportedException>(() => {
                JsonLambda.Create<TestObject>    (p => p.array.Select(i => i));
            });
            AreEqual("unsupported method: Select, expression: p => p.array.Select(i => i)", e.Message);
            
            e = Throws<NotSupportedException>(() => {
                JsonLambda.Create<TestObject>    (p => p.array.Reverse());
            });
            AreEqual("unsupported method: Reverse, expression: p => p.array.Reverse()", e.Message);
        }
        
        [Test]
        public static void TestUseLinqMethodsOnConstants() {
            var array = new int[] { 1, 2, 3 };
            // Evaluation of a constant expression should obviously not done inside a query filter :)
            AreEqual("p => 3",      JsonLambda.Create<object>(p => array.Select(i => i).Count()).Linq);
        }
    }
}