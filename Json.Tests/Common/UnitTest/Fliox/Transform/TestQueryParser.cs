// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Transform.Query.Ops;
using Friflo.Json.Fliox.Transform.Query.Parser;
using System.Collections.Generic;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.Transform.Query.Parser.QueryBuilder;

// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable TooWideLocalVariableScope
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public static class TestQueryParser
    {
        [Test]
        public static void TestQueryLexer() {
            AssertToken(".");
            AssertToken("(");
            AssertToken(")");
            //
            AssertToken( "1");
            AssertToken( "123");
            AssertToken("-456");
            AssertToken("+1", 1, "1");
            AssertToken( "1.23");
            AssertToken( "-4.56");
            //
            AssertToken("+");
            AssertToken("-");
            AssertToken("*");
            AssertToken("/");
            //
            AssertToken("<");
            AssertToken(">");
            AssertToken("<=");
            AssertToken(">=");
            AssertToken("!");
            AssertToken("!=");
            //
            AssertToken("||");
            AssertToken("&&");
            AssertToken("==");
            AssertToken("=>");
            
            AssertToken("abc");
            AssertToken(".abc");
            AssertToken("abc.xyz");
            AssertToken("'xyz'");
            AssertToken("\"foo\"", 1, "'foo'");
            AssertToken("'☀🌎♥👋'");
            
            AssertToken("1+1",   3);
            AssertToken("a-1",   3);
            AssertToken("(1)-1", 5);
            AssertToken("o=>o.name", 3);
            
            AssertToken("a.Contains('xyz')", 3);

            AssertToken("a=>!((a.b==-1||1<2)&&(-2<=3||3>1||3>=(1-1*1)/1||-1!=2||false))", 42); // must be 42 in any case :)
        }
        
        [Test]
        public static void TestQueryLexer_Escape() {
            AssertToken("'\\''",    1, "'''");
            AssertToken("'\\\"'",   1, "'\"'");
            AssertToken("'\\a'",    1, "'a'");
            //
            AssertToken("'\\b'", 1, "'\b'"); // backspace
            AssertToken("'\\f'", 1, "'\f'"); // form feed
            AssertToken("'\\n'", 1, "'\n'"); // new line
            AssertToken("'\\r'", 1, "'\r'"); // carriage return
            AssertToken("'\\t'", 1, "'\t'"); // horizontal tabulator
            AssertToken("'\\v'", 1, "'\v'"); // vertical tabulator
        }
        
        private static void AssertToken (string str, int len = 1, string expect = null) {
            var result = QueryLexer.Tokenize(str, out string _);
            AreEqual(len,     result.items.Length);
            expect = expect ?? str;
            AreEqual(expect,   result.ToString());
        }
        
        [Test]
        public static void TestQueryBasicParser() {
            // --- literals
            {
                var op = Parse("1", out _);
                AreEqual("1", op.Linq);
            } {
                var op = Parse("1.2", out _);
                AreEqual("1.2", op.Linq);
            } {
                var op = Parse("'abc'", out _);
                AreEqual("'abc'", op.Linq);
            } {
                var op = Parse("true", out _);
                That(op, Is.TypeOf<TrueLiteral>());
            } {
                var op = Parse("false", out _);
                That(op, Is.TypeOf<FalseLiteral>());
            } {
                var op = Parse("null", out _);
                That(op, Is.TypeOf<NullLiteral>());
            }
            
            // --- arithmetic operations
            {
                var op = Parse("1+2", out _);
                AreEqual("1 + 2", op.Linq);
            } {
                var op = Parse("1-2", out _);
                AreEqual("1 - 2", op.Linq);
            } {
                var op = Parse("1*2", out _);
                AreEqual("1 * 2", op.Linq);
            } {
                var op = Parse("1/2", out _);
                AreEqual("1 / 2", op.Linq);
            } {
                var op = Parse("7%3", out _);
                AreEqual("7 % 3", op.Linq);
            }
            // --- comparison operation
            {
                var op = Parse("1<2", out _);
                AreEqual("1 < 2", op.Linq);
            } {
                var op = Parse("1<=2", out _);
                AreEqual("1 <= 2", op.Linq);
            } {
                var op = Parse("1>2", out _);
                AreEqual("1 > 2", op.Linq);
            } {
                var op = Parse("1>=2", out _);
                AreEqual("1 >= 2", op.Linq);
            } {
                var op = Parse("1==2", out _);
                AreEqual("1 == 2", op.Linq);
            } {
                var op = Parse("1!=2", out _);
                AreEqual("1 != 2", op.Linq);
            } {
                var op = Parse("!true", out _);
                AreEqual("!(true)", op.Linq);
            } {
                var op = Parse("true == false", out _);
                AreEqual("true == false", op.Linq);
            } {
                var op = Parse("true != false", out _);
                AreEqual("true != false", op.Linq);
            }
        }
        
        [Test]
        public static void TestQueryScope() {
            {
                var node    = QueryParser.CreateTree("!(true)", out _);
                AreEqual("! {( {true}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                That(op, Is.TypeOf<Not>());
                AreEqual("!(true)", op.Linq);
            } {
                var node    = QueryParser.CreateTree("(1 + 2) * 3", out _);
                AreEqual("( {* {+ {1, 2}, 3}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                That(op, Is.TypeOf<Multiply>());
                AreEqual("(1 + 2) * 3", op.Linq);
            } {
                var node    = QueryParser.CreateTree("(true || false) && true", out _);
                AreEqual("( {&& {|| {true, false}, true}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                That(op, Is.TypeOf<And>());
                AreEqual("(true || false) && true", op.Linq);
            } {
                var node    = QueryParser.CreateTree("(1 < 2 || 3 < 4) && 5 < 6", out _);
                AreEqual("( {&& {|| {< {1, 2}, < {3, 4}}, < {5, 6}}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                That(op, Is.TypeOf<And>());
                AreEqual("(1 < 2 || 3 < 4) && 5 < 6", op.Linq);
            } {
                var node    = QueryParser.CreateTree("((1))", out _);
                AreEqual("( {( {1}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("1", op.Linq);
            } {
                var node    = QueryParser.CreateTree("!(true)", out _);
                AreEqual("! {( {true}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("!(true)", op.Linq);
            } {
                var node    = QueryParser.CreateTree("(!(true))", out _);
                AreEqual("( {! {( {true}}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("!(true)", op.Linq);
            } {
                var node    = QueryParser.CreateTree("!(!(true))", out _);
                AreEqual("! {( {! {( {true}}}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("!(!(true))", op.Linq);
            } {
                var op = Parse("(1 + 2) == 3", out _);
                AreEqual("1 + 2 == 3", op.Linq);
            }  {
                var op = Parse("(a + b) == 3", out _, TestEnv);
                AreEqual("a + b == 3", op.Linq);
            }
        }
        
        [Test]
        public static void TestQueryOr() {
            {
                var op = Parse("true || false", out _);
                AreEqual("true || false", op.Linq);
            } {
                var op = Parse("true || false || true", out _);
                AreEqual("true || false || true", op.Linq);
                That(op, Is.TypeOf<Or>());
                AreEqual(3, ((Or)op).operands.Count);
            }
        }
        
        [Test]
        public static void TestQueryAnd() {
            {
                var op = Parse("true && false", out _);
                AreEqual("true && false", op.Linq);
            } {
                var op = Parse("true && false && true", out _);
                AreEqual("true && false && true", op.Linq);
                That(op, Is.TypeOf<And>());
                AreEqual(3, ((And)op).operands.Count);
            }
        }
        
        private static QueryEnv TestEnv => new QueryEnv("o", new Dictionary<string, string>{{"a", "1"}, {"b", "2"}, {"c", "3"}});
        
        
        [Test]
        public static void TestQueryPrecedence() {
            // note: the operation with lowest precedence is always on the beginning of node.ToString()
            {
                var node    = QueryParser.CreateTree("a * b + c", out _);
                AreEqual("+ {* {a, b}, c}", node.ToString());
                var op      = OperationFromNode(node, out _, TestEnv);
                AreEqual("a * b + c", op.Linq);
            } {
                var node    = QueryParser.CreateTree("a + b * c", out _);
                AreEqual("+ {a, * {b, c}}", node.ToString());
                var op      = OperationFromNode(node, out _, TestEnv);
                AreEqual("a + b * c", op.Linq);
            } {
                var node    = QueryParser.CreateTree("true && false && 1 < 2", out _);
                AreEqual("&& {true, false, < {1, 2}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("true && false && 1 < 2", op.Linq);
            } {
                var node    = QueryParser.CreateTree("true || 1 + 2 * 3 < 10", out _);
                AreEqual("|| {true, < {+ {1, * {2, 3}}, 10}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("true || 1 + 2 * 3 < 10", op.Linq);
            } {
                var node    = QueryParser.CreateTree("10 > 3 * 2 + 1 || true", out _);
                AreEqual("|| {> {10, + {* {3, 2}, 1}}, true}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("10 > 3 * 2 + 1 || true", op.Linq);
            } {
                var node    = QueryParser.CreateTree("true || false && true", out _);
                AreEqual("|| {true, && {false, true}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("true || false && true", op.Linq);
            } {
                var node    = QueryParser.CreateTree("true && false || true", out _);
                AreEqual("|| {&& {true, false}, true}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("true && false || true", op.Linq);
            }
        }
        
        [Test]
        public static void TestQueryErrors() {
            string error;
            {
                Parse("", out error);
                AreEqual("operation is empty", error);
            } {
                Parse("true < false", out error);
                AreEqual("operator < must not use boolean operands at pos 5", error);
            } {
                Parse("1 < 3 > 2", out error);
                AreEqual("operator > must not use boolean operands at pos 6", error);
            } {
                Parse("true ||", out error);
                AreEqual("expect at minimum two operands for operator || at pos 5", error);
            } {
                Parse("true || 1", out error);
                AreEqual("operator || expect boolean operands. Got: 1 at pos 8", error);
            } {
                Parse("true || ()", out error); // coverage
                AreEqual("parentheses (...) expect one operand at pos 8", error);
            } {
                Parse("1+", out error);
                AreEqual("operator + expect two operands at pos 1", error);
            } {
                Parse("if", out error);
                AreEqual("conditional statements must not be used: if at pos 0", error);
            } {
                Parse("o.children.Foo(child => child.age)", out error, TestEnv);
                AreEqual("unknown method: Foo() used by o.children at pos 0", error);
            } {
                Parse("Foo(1)", out error);
                AreEqual("unknown function: Foo() at pos 0", error);
            } {
                Parse("!123", out error);
                AreEqual("not operator ! must use a boolean operand. Was: 123 at pos 1", error);
            } {
                Parse(" )", out error);
                AreEqual("no matching open parenthesis at pos 1", error);
            } {
                Parse("123)", out error);
                AreEqual("no matching open parenthesis at pos 3", error);
            } {
                Parse("(123", out error);
                AreEqual("missing closing parenthesis at pos 0", error);
            } {
                Parse("!a b", out error);
                AreEqual("not operator expect one operand at pos 0", error);
            } {
                Parse("()", out error);
                AreEqual("parentheses (...) expect one operand at pos 0", error);
            } {
                Parse("(1 2)", out error);
                AreEqual("parentheses (...) expect one operand at pos 0", error);
            } {
                Parse("Abs()", out error);
                AreEqual("expect field or numeric operand in Abs() at pos 0", error);
            } {
                Parse("=>", out error);
                AreEqual("operator => expect one preceding operand at pos 0", error);
            } {
                Parse("||", out error);
                AreEqual("operator || expect one preceding operand at pos 0", error);
            } {
                Parse("+", out error);
                AreEqual("operator + expect one preceding operand at pos 0", error);
            } {
                Parse("!Abs(1)", out error);
                AreEqual("not operator ! must use a boolean operand. Was: Abs() at pos 1", error);
            } {
                Parse("Abs('abc')", out error);
                AreEqual("Abs() expect field or numeric operand. was: 'abc' at pos 4", error);
            } {
                Parse("Abs(true)", out error);
                AreEqual("Abs() expect field or numeric operand. was: true at pos 4", error);
            } {
                Parse("Abs(1 == 1)", out error);
                AreEqual("Abs() expect field or numeric operand. was: == at pos 6", error);
            } {
                Parse("Abs(Abs(1 == 1))", out error);
                AreEqual("Abs() expect field or numeric operand. was: == at pos 10", error);
            } {
                Parse("foo('bar') >= 1", out error);
                AreEqual("unknown function: foo() at pos 0", error);
            } {
                Parse("foo.Contains('bar')", out error);
                AreEqual("variable not found: foo at pos 0", error);
            } {
                Parse("o.foo.Contains(1)", out error, TestEnv);
                AreEqual("expect string or field operand in o.foo.Contains(). was: 1 at pos 15", error);
            } {
                Parse("o.foo.Contains()", out error, TestEnv);
                AreEqual("expect one operand in o.foo.Contains() at pos 0", error);
            } {
                Parse("'abc'.Foo('xyz')", out error);
                AreEqual("unexpected operand .Foo() on 'abc' at pos 5", error);
            } {
                Parse("123.Bar('xyz')", out error);
                AreEqual("unexpected operand Bar() on 123 at pos 4", error);
            } {
                Parse("true.Bar('xyz')", out error);
                AreEqual("variable not found: true at pos 0", error);
            } {
                Parse("o.children.Contains(foo)", out error, TestEnv);
                AreEqual("variable not found: foo at pos 20", error);
            } {
                Parse("true == foo.Contains('xxx')", out error, new QueryEnv(null)); // no variables set
                AreEqual("variable not found: foo at pos 8", error);
            } {
                Parse("1 + 'sss'", out error);
                AreEqual("operator + must use numeric operands at pos 2", error);
            } {
                Parse("true o", out error);
                AreEqual("unexpected operand o on true at pos 5", error);
            }
        }
        
        [Test]
        public static void TestQueryArrowErrors() {
            string error;
            {
                Parse("1 => 2", out error);
                AreEqual("=> can be used only as lambda in functions. Used by: 1 at pos 0", error);
            } {
                Parse(".children.Any(=> child.age)", out error);
                AreEqual("=> expect one preceding lambda argument. Used in: .children.Any() at pos 14", error);
            } {
                Parse(".children.Any(child foo => child.age)", out error);
                AreEqual("=> expect one preceding lambda argument. Used in: .children.Any() at pos 24", error);
            } {
                Parse(".children.Any(-1 => child.age)", out error);
                AreEqual("=> lambda argument must be a parameter name. Was: -1 in .children.Any() at pos 14", error);
            } {
                Parse("o =>", out error);
                AreEqual("lambda 'o =>' expect lambda body at pos 2", error);
            } {
                Parse("o => foo.age", out error);
                AreEqual("variable not found: foo at pos 5", error);
            } {
                Parse("o.children.Any(child => child.age)", out error, TestEnv);
                AreEqual("quantify operation o.children.Any() expect boolean lambda body. Was: child.age at pos 24", error);
            } {
                Parse("o.children.Any(foo)", out error, TestEnv);
                AreEqual("Invalid lambda expression in o.children.Any() at pos 0", error);
            } {
                Parse("o.children.Min(foo)", out error, TestEnv);
                AreEqual("Invalid lambda expression in o.children.Min() at pos 0", error);
            } {
                Parse("o.children.Any(foo => bar)", out error, TestEnv);
                AreEqual("variable not found: bar at pos 22", error);
            } {
                Parse("o.children.Min(foo => bar)", out error, TestEnv);
                AreEqual("variable not found: bar at pos 22", error);
            } {
                Parse("o.children.Any()", out error, TestEnv);
                AreEqual("Invalid lambda expression in o.children.Any() at pos 0", error);
            } {
                Parse("o.children.Min()", out error, TestEnv);
                AreEqual("Invalid lambda expression in o.children.Min() at pos 0", error);
            } {
                Parse(".Any()", out error, TestEnv);
                AreEqual("missing preceding variable for .Any() at pos 0", error);
            }  {
                Parse("..Any()", out error, TestEnv);
                AreEqual("missing preceding variable for ..Any() at pos 0", error);
            } {
                Parse("x.yyy.Any(a => true)", out error, TestEnv);
                AreEqual("variable not found: x at pos 0", error);
            } {
                Parse("o.Any(a => true)", out error, TestEnv);
                AreEqual("parameter already used: a at pos 6", error);
            } {
                Parse("o.children.Any(o => true)", out error, TestEnv);
                AreEqual("parameter already used: o at pos 15", error);
            } {
                Parse("o.children.Min(o => true)", out error, TestEnv);
                AreEqual("parameter already used: o at pos 15", error);
            } {
                Parse("o => true", out error, TestEnv);
                AreEqual("parameter already used: o at pos 0", error);
            }
        }
        
        [Test]
        public static void TestLexerErrors() {
            string error;
            {
                Parse("=+", out error);
                AreEqual("invalid operator '='. Use == or => at pos 1", error);
            } {
                Parse("|x", out error);
                AreEqual("unexpected character 'x' after '|'. Use || at pos 1", error);
            } {
                Parse("&x", out error);
                AreEqual("unexpected character 'x' after '&'. Use && at pos 1", error);
            } {
                Parse("#", out error);
                AreEqual("unexpected character: '#' at pos 0", error);
            } {
                Parse("'abc", out error);
                AreEqual("missing string terminator for: abc at pos 4", error);
            } {
                Parse("1.23.4", out error);
                AreEqual("invalid floating point number: 1.23. at pos 4", error);
            } {
                Parse("'abc\\", out error);
                AreEqual("missing escaped character for: abc\\ at pos 5", error);
            }
        }
        
        [Test]
        public static void TestArithmeticFunction() {
            {
                var op = Parse("Abs(-1)", out _);
                AreEqual("Abs(-1)", op.Linq);
            }  {
                var op = Parse("Ceiling(2.5)", out _);
                AreEqual("Ceiling(2.5)", op.Linq);
            }  {
                var op = Parse("Floor(2.5)", out _);
                AreEqual("Floor(2.5)", op.Linq);
            } {
                var op = Parse("Exp(2.5)", out _);
                AreEqual("Exp(2.5)", op.Linq);
            } {
                var op = Parse("Log(2.5)", out _);
                AreEqual("Log(2.5)", op.Linq);
            } {
                var op = Parse("Log(2.5)", out _);
                AreEqual("Log(2.5)", op.Linq);
            } {
                var op = Parse("Sqrt(2.5)", out _);
                AreEqual("Sqrt(2.5)", op.Linq);
            } {
                var op = Parse("Abs(Sqrt(2.5))", out _);
                AreEqual("Abs(Sqrt(2.5))", op.Linq);
            } {
                var op = Parse("Abs(1 + 1)", out _);
                AreEqual("Abs(1 + 1)", op.Linq);
            } {
                var op = Parse("PI", out _);
                AreEqual("PI", op.Linq);
            } {
                var op = Parse("E", out _);
                AreEqual("E", op.Linq);
            } {
                var op = Parse("Tau", out _);
                AreEqual("Tau", op.Linq);
            }
            // --- same operator precedence
            {
                var op = Parse("1 + 2 - 3", out _);
                That(op, Is.TypeOf<Subtract>());
                AreEqual("1 + 2 - 3", op.Linq);
            } {
                var op = Parse("1 - 2 + 3", out _);
                That(op, Is.TypeOf<Add>());
                AreEqual("1 - 2 + 3", op.Linq);
            } {
                var op = Parse("1 * 2 / 3", out _);
                That(op, Is.TypeOf<Divide>());
                AreEqual("1 * 2 / 3", op.Linq);
            } {
                var op = Parse("1 / 2 * 3", out _);
                That(op, Is.TypeOf<Multiply>());
                AreEqual("1 / 2 * 3", op.Linq);
            } {
                var op = Parse("1 * 2 % 3", out _);
                That(op, Is.TypeOf<Modulo>());
                AreEqual("1 * 2 % 3", op.Linq);
            }
            // --- test with scopes
            {
                var op = Parse("(Abs(1) == 1)", out _);
                AreEqual("Abs(1) == 1", op.Linq);
            } {
                var op = Parse("!(Abs(1) == 1)", out _);
                AreEqual("!(Abs(1) == 1)", op.Linq);
            }
        }
        
        [Test]
        public static void TestQueryAggregateMethods() {
            {
                var op = Parse("o.children.Min(child => child.age)", out _, TestEnv);
                AreEqual("o.children.Min(child => child.age)", op.Linq);
            } {
                var op = Parse("o.children.Max(child => child.age)", out _, TestEnv);
                AreEqual("o.children.Max(child => child.age)", op.Linq);
            } {
                var op = Parse("o.children.Sum(child => child.age)", out _, TestEnv);
                AreEqual("o.children.Sum(child => child.age)", op.Linq);
            } {
                var op = Parse("o.children.Average(child => child.age)", out _, TestEnv);
                AreEqual("o.children.Average(child => child.age)", op.Linq);
            }
        }
        
        [Test]
        public static void TestQueryLambda() {
            {
                var node = QueryParser.CreateTree("o => true", out _);
                AreEqual("o {=> {true}}", node.ToString());
                var op = OperationFromNode(node, out _);
                AreEqual("o => true", op.Linq);
            } {
                var node = QueryParser.CreateTree("o => o.m1 + o.m2", out _);
                AreEqual("o {=> {+ {o.m1, o.m2}}}", node.ToString());
                var op = OperationFromNode(node, out _);
                AreEqual("o => o.m1 + o.m2", op.Linq);
            } {
                var node = QueryParser.CreateTree("o => true || false", out _);
                AreEqual("o {=> {|| {true, false}}}", node.ToString());
                var op = OperationFromNode(node, out _);
                AreEqual("o => true || false", op.Linq);
            } {
                var op = Parse("o => (1 + 2)", out _);
                AreEqual("o => 1 + 2", op.Linq);
            }
        }
        
        [Test]
        public static void TestQueryQuantityMethods() {
            {
                var node = QueryParser.CreateTree("o.children.Any(child => child.age == 20)", out _);
                AreEqual("o.children.Any() {child, => {== {child.age, 20}}}", node.ToString());
                var op = OperationFromNode(node, out _, TestEnv);
                AreEqual("o.children.Any(child => child.age == 20)", op.Linq);
            } {
                var node = QueryParser.CreateTree("o.children.All(child => child.age == 20)", out _);
                AreEqual("o.children.All() {child, => {== {child.age, 20}}}", node.ToString());
                var op = OperationFromNode(node, out _, TestEnv);
                AreEqual("o.children.All(child => child.age == 20)", op.Linq);
            } {
                var node = QueryParser.CreateTree("o.children.Count(child => child.age == 20)", out _);
                AreEqual("o.children.Count() {child, => {== {child.age, 20}}}", node.ToString());
                var op = OperationFromNode(node, out _, TestEnv);
                AreEqual("o.children.Count(child => child.age == 20)", op.Linq);
            } {
                var node = QueryParser.CreateTree("o.items.Max(item => item.amount) > 1", out _);
                AreEqual("> {o.items.Max() {item, => {item.amount}}, 1}", node.ToString());
                var op = OperationFromNode(node, out _, TestEnv);
                AreEqual("o.items.Max(item => item.amount) > 1", op.Linq);
            }
        }

        [Test]
        public static void TestQueryStringMethods() {
            {
                var op = Parse("o.name.Contains('Smartphone')", out _, TestEnv);
                AreEqual("o.name.Contains('Smartphone')", op.Linq);
            } {
                var op = Parse("o.name.StartsWith('Smartphone')", out _, TestEnv);
                AreEqual("o.name.StartsWith('Smartphone')", op.Linq);
            } {
                var op = Parse("o.name.EndsWith('Smartphone')", out _, TestEnv);
                AreEqual("o.name.EndsWith('Smartphone')", op.Linq);
            } {
                var op = Parse("o.foo.EndsWith(a)", out _, TestEnv);
                AreEqual("o.foo.EndsWith(a)", op.Linq);
            } {
                var op = Parse("o.name.Length()", out _, TestEnv);
                AreEqual("o.name.Length()", op.Linq);
            }
        }
        
        [Test]
        public static void TestQueryMisc() {
            {
                var op = Parse("o.name=='Smartphone'", out _, TestEnv);
                AreEqual("o.name == 'Smartphone'", op.Linq);
            }
        }
        
        [Test]
        public static void TestQueryCoverage() {
            var shape = new TokenShape();
            AreEqual("Start          arity: Undef, precedence: 0", shape.ToString());
        }
    }
}