// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Transform.Query.Ops;
using Friflo.Json.Fliox.Transform.Query.Parser;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Fliox.Transform.Query.Parser.QueryParser;

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
            AssertToken("'☀🌎♥👋'");
            
            AssertToken("1+1",   3);
            AssertToken("a-1",   3);
            AssertToken("(1)-1", 5);
            AssertToken("o=>o.name", 3);
            
            AssertToken("a.Contains('xyz')", 3);

            AssertToken("a=>!((a.b==-1||1<2)&&(-2<=3||3>1||3>=(1-1*1)/1||-1!=2||false))", 42); // must be 42 in any case :)
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
                var node    = QueryTree.CreateTree("!(true)", out _);
                AreEqual("! {( {true}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                That(op, Is.TypeOf<Not>());
                AreEqual("!(true)", op.Linq);
            } {
                var node    = QueryTree.CreateTree("(1 + 2) * 3", out _);
                AreEqual("( {* {+ {1, 2}, 3}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                That(op, Is.TypeOf<Multiply>());
                AreEqual("(1 + 2) * 3", op.Linq);
            } {
                var node    = QueryTree.CreateTree("(true || false) && true", out _);
                AreEqual("( {&& {|| {true, false}, true}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                That(op, Is.TypeOf<And>());
                AreEqual("(true || false) && true", op.Linq);
            } {
                var node    = QueryTree.CreateTree("(1 < 2 || 3 < 4) && 5 < 6", out _);
                AreEqual("( {&& {|| {< {1, 2}, < {3, 4}}, < {5, 6}}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                That(op, Is.TypeOf<And>());
                AreEqual("(1 < 2 || 3 < 4) && 5 < 6", op.Linq);
            } {
                var node    = QueryTree.CreateTree("((1))", out _);
                AreEqual("( {( {1}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("1", op.Linq);
            } {
                var node    = QueryTree.CreateTree("!(true)", out _);
                AreEqual("! {( {true}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("!(true)", op.Linq);
            } {
                var node    = QueryTree.CreateTree("(!(true))", out _);
                AreEqual("( {! {( {true}}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("!(true)", op.Linq);
            } {
                var node    = QueryTree.CreateTree("!(!(true))", out _);
                AreEqual("! {( {! {( {true}}}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("!(!(true))", op.Linq);
            } {
                var op = Parse("(1 + 2) == 3", out _);
                AreEqual("1 + 2 == 3", op.Linq);
            }  {
                var op = Parse("(.val1 + .val2) == 3", out _);
                AreEqual(".val1 + .val2 == 3", op.Linq);
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
        
        [Test]
        public static void TestQueryPrecedence() {
            // note: the operation with lowest precedence is always on the beginning of node.ToString()
            {
                var node    = QueryTree.CreateTree("a * b + c", out _);
                AreEqual("+ {* {a, b}, c}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("a * b + c", op.Linq);
            } {
                var node    = QueryTree.CreateTree("a + b * c", out _);
                AreEqual("+ {a, * {b, c}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("a + b * c", op.Linq);
            } {
                var node    = QueryTree.CreateTree("true && false && 1 < 2", out _);
                AreEqual("&& {true, false, < {1, 2}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("true && false && 1 < 2", op.Linq);
            } {
                var node    = QueryTree.CreateTree("true || 1 + 2 * 3 < 10", out _);
                AreEqual("|| {true, < {+ {1, * {2, 3}}, 10}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("true || 1 + 2 * 3 < 10", op.Linq);
            } {
                var node    = QueryTree.CreateTree("10 > 3 * 2 + 1 || true", out _);
                AreEqual("|| {> {10, + {* {3, 2}, 1}}, true}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("10 > 3 * 2 + 1 || true", op.Linq);
            } {
                var node    = QueryTree.CreateTree("true || false && true", out _);
                AreEqual("|| {true, && {false, true}}", node.ToString());
                var op      = OperationFromNode(node, out _);
                AreEqual("true || false && true", op.Linq);
            } {
                var node    = QueryTree.CreateTree("true && false || true", out _);
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
                Parse(".children.Foo(child => child.age)", out error);
                AreEqual("unknown method: Foo() used by: .children.Foo at pos 0", error);
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
                Parse("Abs()", out error);
                AreEqual("function Abs() expect one operand at pos 0", error);
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
                Parse("foo('bar') >= 1", out error);
                AreEqual("unknown function: foo() at pos 0", error);
            } {
                Parse("foo.Contains('bar')", out error);
                AreEqual("expect . in field name foo at pos 0", error);
            } {
                Parse(".foo.Contains(1)", out error);
                AreEqual("expect string or field operand in .foo.Contains(). was: 1 at pos 14", error);
            }
        }
        
        [Test]
        public static void TestQueryQuantifyErrors() {
            string error;
            Parse(".children.Any(child => child.age)", out error);
            AreEqual("quantify operation .children.Any() expect boolean lambda body. Was: child.age at pos 23", error);
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
                AreEqual("=> lambda argument must by a symbol name. Was: -1 in .children.Any() at pos 14", error);
            } {
                Parse("o =>", out error);
                AreEqual("lambda 'o =>' expect one subsequent operand as body at pos 2", error);
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
            }  {
                Parse("1.23.4", out error);
                AreEqual("invalid floating point number: 1.23. at pos 4", error);
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
                var op = Parse(".children.Min(child => child.age)", out _);
                AreEqual(".children.Min(child => child.age)", op.Linq);
            } {
                var op = Parse(".children.Max(child => child.age)", out _);
                AreEqual(".children.Max(child => child.age)", op.Linq);
            } {
                var op = Parse(".children.Sum(child => child.age)", out _);
                AreEqual(".children.Sum(child => child.age)", op.Linq);
            } {
                var op = Parse(".children.Average(child => child.age)", out _);
                AreEqual(".children.Average(child => child.age)", op.Linq);
            }
        }
        
        [Test]
        public static void TestQueryLambda() {
            {
                var node = QueryTree.CreateTree("o => true", out _);
                AreEqual("o {=> {true}}", node.ToString());
                var op = OperationFromNode(node, out _);
                AreEqual("o => true", op.Linq);
            } {
                var node = QueryTree.CreateTree("o => o.m1 + o.m2", out _);
                AreEqual("o {=> {+ {o.m1, o.m2}}}", node.ToString());
                var op = OperationFromNode(node, out _);
                AreEqual("o => o.m1 + o.m2", op.Linq);
            } {
                var node = QueryTree.CreateTree("o => true || false", out _);
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
                var node = QueryTree.CreateTree(".children.Any(child => child.age == 20)", out _);
                AreEqual(".children.Any() {child, => {== {child.age, 20}}}", node.ToString());
                var op = OperationFromNode(node, out _);
                AreEqual(".children.Any(child => child.age == 20)", op.Linq);
            } {
                var node = QueryTree.CreateTree(".children.All(child => child.age == 20)", out _);
                AreEqual(".children.All() {child, => {== {child.age, 20}}}", node.ToString());
                var op = OperationFromNode(node, out _);
                AreEqual(".children.All(child => child.age == 20)", op.Linq);
            } {
                var node = QueryTree.CreateTree(".children.Count(child => child.age == 20)", out _);
                AreEqual(".children.Count() {child, => {== {child.age, 20}}}", node.ToString());
                var op = OperationFromNode(node, out _);
                AreEqual(".children.Count(child => child.age == 20)", op.Linq);
            } {
                var node = QueryTree.CreateTree(".items.Max(item => item.amount) > 1", out _);
                AreEqual("> {.items.Max() {item, => {item.amount}}, 1}", node.ToString());
                var op = OperationFromNode(node, out _);
                AreEqual(".items.Max(item => item.amount) > 1", op.Linq);
            }
        }

        [Test]
        public static void TestQueryStringMethods() {
            {
                var op = Parse(".name.Contains('Smartphone')", out _);
                AreEqual(".name.Contains('Smartphone')", op.Linq);
            } {
                var op = Parse(".name.StartsWith('Smartphone')", out _);
                AreEqual(".name.StartsWith('Smartphone')", op.Linq);
            } {
                var op = Parse(".name.EndsWith('Smartphone')", out _);
                AreEqual(".name.EndsWith('Smartphone')", op.Linq);
            } {
                var op = Parse(".foo.EndsWith(.bar)", out _);
                AreEqual(".foo.EndsWith(.bar)", op.Linq);
            }
        }
        
        [Test]
        public static void TestQueryMisc() {
            {
                var op = Parse(".name=='Smartphone'", out _);
                AreEqual(".name == 'Smartphone'", op.Linq);
            }
        }
        
        [Test]
        public static void TestQueryCoverage() {
            var shape = new TokenShape();
            AreEqual("Start          arity: Undef, precedence: 0", shape.ToString());
        }
    }
}