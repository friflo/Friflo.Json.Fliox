// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;
using Friflo.Json.Fliox.Transform.Query.Parser;
using NUnit.Framework;
using static NUnit.Framework.Assert;

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
            
            AssertToken("a.Contains('xyz')", 4);

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
                var op = QueryParser.Parse("1", out _);
                AreEqual("1", op.Linq);
            } {
                var op = QueryParser.Parse("1.2", out _);
                AreEqual("1.2", op.Linq);
            } {
                var op = QueryParser.Parse("'abc'", out _);
                AreEqual("'abc'", op.Linq);
            } {
                var op = QueryParser.Parse("true", out _);
                That(op, Is.TypeOf<TrueLiteral>());
            } {
                var op = QueryParser.Parse("false", out _);
                That(op, Is.TypeOf<FalseLiteral>());
            } {
                var op = QueryParser.Parse("null", out _);
                That(op, Is.TypeOf<NullLiteral>());
            }
            
            // --- arithmetic operations
            {
                var op = QueryParser.Parse("1+2", out _);
                AreEqual("1 + 2", op.Linq);
            } {
                var op = QueryParser.Parse("1-2", out _);
                AreEqual("1 - 2", op.Linq);
            } {
                var op = QueryParser.Parse("1*2", out _);
                AreEqual("1 * 2", op.Linq);
            } {
                var op = QueryParser.Parse("1/2", out _);
                AreEqual("1 / 2", op.Linq);
            }
            // --- comparison operation
            {
                var op = QueryParser.Parse("1<2", out _);
                AreEqual("1 < 2", op.Linq);
            } {
                var op = QueryParser.Parse("1<=2", out _);
                AreEqual("1 <= 2", op.Linq);
            } {
                var op = QueryParser.Parse("1>2", out _);
                AreEqual("1 > 2", op.Linq);
            } {
                var op = QueryParser.Parse("1>=2", out _);
                AreEqual("1 >= 2", op.Linq);
            } {
                var op = QueryParser.Parse("1==2", out _);
                AreEqual("1 == 2", op.Linq);
            } {
                var op = QueryParser.Parse("1!=2", out _);
                AreEqual("1 != 2", op.Linq);
            } {
                var op = QueryParser.Parse("!true", out _);
                AreEqual("!(true)", op.Linq);
            }
        }
        
        [Test]
        public static void TestQueryScope() {
            {
                var node    = QueryTree.CreateTree("!(true)", out _);
                AreEqual("! {( {true}}", node.ToString());
                var op      = QueryParser.OperationFromNode(node, out _);
                That(op, Is.TypeOf<Not>());
                AreEqual("!(true)", op.ToString());
            } {
                var node    = QueryTree.CreateTree("(1 + 2) * 3", out _);
                AreEqual("( {* {+ {1, 2}, 3}}", node.ToString());
                var op      = QueryParser.OperationFromNode(node, out _);
                That(op, Is.TypeOf<Multiply>());
                AreEqual("(1 + 2) * 3", op.ToString());
            } {
                var node    = QueryTree.CreateTree("(true || false) && true", out _);
                AreEqual("( {&& {|| {true, false}, true}}", node.ToString());
                var op      = QueryParser.OperationFromNode(node, out _);
                That(op, Is.TypeOf<And>());
                AreEqual("(true || false) && true", op.ToString());
            } {
                var node    = QueryTree.CreateTree("(1 < 2 || 3 < 4) && 5 < 6", out _);
                AreEqual("( {&& {|| {< {1, 2}, < {3, 4}}, < {5, 6}}}", node.ToString());
                var op      = QueryParser.OperationFromNode(node, out _);
                That(op, Is.TypeOf<And>());
                AreEqual("(1 < 2 || 3 < 4) && 5 < 6", op.ToString());
            }
        }
        
        [Test]
        public static void TestQueryOr() {
            {
                var op = QueryParser.Parse("true || false", out _);
                AreEqual("true || false", op.Linq);
            } {
                var op = QueryParser.Parse("true || false || true", out _);
                AreEqual("true || false || true", op.Linq);
                That(op, Is.TypeOf<Or>());
                AreEqual(3, ((Or)op).operands.Count);
            }
        }
        
        [Test]
        public static void TestQueryAnd() {
            {
                var op = QueryParser.Parse("true && false", out _);
                AreEqual("true && false", op.Linq);
            } {
                var op = QueryParser.Parse("true && false && true", out _);
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
                var op      = QueryParser.OperationFromNode(node, out _);
                AreEqual("a * b + c", op.ToString());
            } {
                var node    = QueryTree.CreateTree("a + b * c", out _);
                AreEqual("+ {a, * {b, c}}", node.ToString());
                var op  = QueryParser.OperationFromNode(node, out _);
                AreEqual("a + b * c", op.ToString());
            } {
                var node    = QueryTree.CreateTree("true && false && 1 < 2", out _);
                AreEqual("&& {true, false, < {1, 2}}", node.ToString());
                var op      = QueryParser.OperationFromNode(node, out _);
                AreEqual("true && false && 1 < 2", op.ToString());
            } {
                var node    = QueryTree.CreateTree("true || 1 + 2 * 3 < 10", out _);
                AreEqual("|| {true, < {+ {1, * {2, 3}}, 10}}", node.ToString());
                var op      = QueryParser.OperationFromNode(node, out _);
                AreEqual("true || 1 + 2 * 3 < 10", op.ToString());
            } {
                var node    = QueryTree.CreateTree("10 > 3 * 2 + 1 || true", out _);
                AreEqual("|| {> {10, + {* {3, 2}, 1}}, true}", node.ToString());
                var op      = QueryParser.OperationFromNode(node, out _);
                AreEqual("10 > 3 * 2 + 1 || true", op.ToString());
            } {
                var node    = QueryTree.CreateTree("true || false && true", out _);
                AreEqual("|| {true, && {false, true}}", node.ToString());
                var op      = QueryParser.OperationFromNode(node, out _);
                AreEqual("true || false && true", op.ToString());
            } {
                var node    = QueryTree.CreateTree("true && false || true", out _);
                AreEqual("|| {&& {true, false}, true}", node.ToString());
                var op      = QueryParser.OperationFromNode(node, out _);
                AreEqual("true && false || true", op.ToString());
            }
        }
        
        [Test]
        public static void TestQueryErrors() {
            string error;
            {
                QueryParser.Parse("", out error);
                AreEqual("operation string is empty", error);
            } {
                QueryParser.Parse("true < false", out error);
                AreEqual("operation < must not use boolean operands", error);
            } {
                QueryParser.Parse("1 < 3 > 2", out error);
                AreEqual("operation > must not use boolean operands", error);
            } {
                QueryParser.Parse("true ||", out error);
                AreEqual("expect at minimum two operands for operation ||", error);
            } {
                QueryParser.Parse("true || 1", out error);
                AreEqual("operation || expect boolean operands. Got: 1", error);
            } {
                QueryParser.Parse("1+", out error);
                AreEqual("operation + expect two operands", error);
            } {
                QueryParser.Parse("if", out error);
                AreEqual("operation must not use conditional statement: if", error);
            } {
                QueryParser.Parse(".children.Foo(child => child.age)", out error);
                AreEqual("unknown method: Foo() used by: .children.Foo", error);
            } {
                QueryParser.Parse("Foo(1)", out error);
                AreEqual("unknown function: Foo()", error);
            }
        }
        
        [Test]
        public static void TestQueryQuantifyErrors() {
            string error;
            QueryParser.Parse(".children.Any(child => child.age)", out error);
            AreEqual("quantify operation .children.Any() expect boolean lambda body. Was: child.age", error);
        }
        
        [Test]
        public static void TestQueryArrowErrors() {
            string error;
            {
                QueryParser.Parse("1 => 2", out error);
                AreEqual("=> can be used only as lambda in functions. Was used by: 1", error);
            } {
                QueryParser.Parse(".children.Any(=> child.age)", out error);
                AreEqual("=> expect one preceding lambda argument. Was used in: .children.Any()", error);
            } {
                QueryParser.Parse(".children.Any(child foo => child.age)", out error);
                AreEqual("=> expect one preceding lambda argument. Was used in: .children.Any()", error);
            } {
                QueryParser.Parse(".children.Any(-1 => child.age)", out error);
                AreEqual("=> lambda argument must by a symbol name. Was: -1 in .children.Any()", error);
            }
        }
        
        [Test]
        public static void TestLexerErrors() {
            string error;
            {
                QueryParser.Parse("=+", out error);
                AreEqual("unexpected character '+' after '='. Use == or =>", error);
            } {
                QueryParser.Parse("|x", out error);
                AreEqual("unexpected character 'x' after '|'. Use ||", error);
            } {
                QueryParser.Parse("&x", out error);
                AreEqual("unexpected character 'x' after '&'. Use &&", error);
            } {
                QueryParser.Parse("#", out error);
                AreEqual("unexpected character: '#'", error);
            } {
                QueryParser.Parse("'abc", out error);
                AreEqual("missing string terminator ' for: abc", error);
            }  {
                QueryParser.Parse("1.23.4", out error);
                AreEqual("invalid floating point number: 1.23.", error);
            }
        }
        
        [Test]
        public static void TestArithmeticFunction() {
            {
                var op = QueryParser.Parse("Abs(-1)", out _);
                AreEqual("Abs(-1)", op.ToString());
            }  {
                var op = QueryParser.Parse("Ceiling(2.5)", out _);
                AreEqual("Ceiling(2.5)", op.ToString());
            }  {
                var op = QueryParser.Parse("Floor(2.5)", out _);
                AreEqual("Floor(2.5)", op.ToString());
            } {
                var op = QueryParser.Parse("Exp(2.5)", out _);
                AreEqual("Exp(2.5)", op.ToString());
            } {
                var op = QueryParser.Parse("Log(2.5)", out _);
                AreEqual("Log(2.5)", op.ToString());
            }  {
                var op = QueryParser.Parse("Log(2.5)", out _);
                AreEqual("Log(2.5)", op.ToString());
            }
        }
        
        [Test]
        public static void TestQueryAggregateMethods() {
            {
                var op = QueryParser.Parse(".children.Min(child => child.age)", out _);
                AreEqual(".children.Min(child => child.age)", op.ToString());
            } {
                var op = QueryParser.Parse(".children.Max(child => child.age)", out _);
                AreEqual(".children.Max(child => child.age)", op.ToString());
            } {
                var op = QueryParser.Parse(".children.Sum(child => child.age)", out _);
                AreEqual(".children.Sum(child => child.age)", op.ToString());
            } {
                var op = QueryParser.Parse(".children.Average(child => child.age)", out _);
                AreEqual(".children.Average(child => child.age)", op.ToString());
            }
        }
        
        [Test]
        public static void TestQueryQuantityMethods() {
            {
                var node = QueryTree.CreateTree(".children.Any(child => child.age == 20)", out _);
                AreEqual(".children.Any {child, == {child.age, 20}}", node.ToString());
                var op = QueryParser.OperationFromNode(node, out _);
                AreEqual(".children.Any(child => child.age == 20)", op.ToString());
            } {
                var node = QueryTree.CreateTree(".children.All(child => child.age == 20)", out _);
                AreEqual(".children.All {child, == {child.age, 20}}", node.ToString());
                var op = QueryParser.OperationFromNode(node, out _);
                AreEqual(".children.All(child => child.age == 20)", op.ToString());
            } {
                var node = QueryTree.CreateTree(".children.Count(child => child.age == 20)", out _);
                AreEqual(".children.Count {child, == {child.age, 20}}", node.ToString());
                var op = QueryParser.OperationFromNode(node, out _);
                AreEqual(".children.Count(child => child.age == 20)", op.ToString());
            } {
                var node = QueryTree.CreateTree(".items.Max(item => item.amount) > 1", out _);
                AreEqual("> {.items.Max {item, item.amount}, 1}", node.ToString());
                var op = QueryParser.OperationFromNode(node, out _);
                AreEqual(".items.Max(item => item.amount) > 1", op.ToString());
            }
        }

        [Test]
        public static void TestQueryStringMethods() {
            {
                var op = QueryParser.Parse(".name.Contains('Smartphone')", out _);
                AreEqual(".name.Contains('Smartphone')", op.ToString());
            } {
                var op = QueryParser.Parse(".name.StartsWith('Smartphone')", out _);
                AreEqual(".name.StartsWith('Smartphone')", op.ToString());
            } {
                var op = QueryParser.Parse(".name.EndsWith('Smartphone')", out _);
                AreEqual(".name.EndsWith('Smartphone')", op.ToString());
            }
        }
        
        [Test]
        public static void TestQueryMisc() {
            {
                var op = QueryParser.Parse(".name=='Smartphone'", out _);
                AreEqual(".name == 'Smartphone'", op.ToString());
            }
        }
        
        // ------------------------ evaluate operations ------------------------ 
        [Test]
        public static void TestFilterUndefinedScalar() {
            using (var eval = new JsonEvaluator()) {
                // use an aggregate (Max) of an empty array and compare it to a scalar
                AssertFilterUndefinedScalar (".items.Max(item => item.amount) == 1", eval);
                AssertFilterUndefinedScalar (".items.Max(item => item.amount) != 1", eval);
                AssertFilterUndefinedScalar (".items.Max(item => item.amount) <  1", eval);
                AssertFilterUndefinedScalar (".items.Max(item => item.amount) <= 1", eval);
                AssertFilterUndefinedScalar (".items.Max(item => item.amount) >  1", eval);
                AssertFilterUndefinedScalar (".items.Max(item => item.amount) >= 1", eval);
            }
        }
        
        private static void AssertFilterUndefinedScalar(string operation, JsonEvaluator eval) {
            var json    = @"{ ""items"": [] }";
            var op      = (FilterOperation)QueryParser.Parse(operation, out _);
            var filter  = new JsonFilter(op);
            var result  = eval.Filter(new JsonValue(json), filter);
            IsFalse(result);
        } 
    }
}