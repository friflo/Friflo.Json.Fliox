// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Transform.Query.Ops;
using Friflo.Json.Fliox.Transform.Query.Parser;
using NUnit.Framework;
using static NUnit.Framework.Assert;


namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public static class TestQueryParser
    {
        [Test]
        public static void TestLexer() {
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
            AssertToken("'xyz'");
            AssertToken("'☀🌎♥👋'");
            
            AssertToken("1+1",   3);
            AssertToken("a-1",   3);
            AssertToken("(1)-1", 5);
            
            AssertToken("a.Contains('xyz')", 6);

            AssertToken("a=>!((a.b==-1||1<2)&&(-2<=3||3>1||3>=(1-1*1)/1||-1!=2))", 42);
        }
        
        private static void AssertToken (string str, int len = 1, string expect = null) {
            var result = QueryLexer.Tokenize(str, out string _);
            AreEqual(len,     result.items.Length);
            expect = expect ?? str;
            AreEqual(expect,   result.ToString());
        }
        
        [Test]
        public static void TestParser() {
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
            }
        }
        
        [Test]
        public static void TestOr() {
            // --- literals
            {
                var op = QueryParser.Parse("true||false", out _);
                AreEqual("true || false", op.Linq);
            } {
                var op = QueryParser.Parse("true||false||true", out _);
                AreEqual("true || false || true", op.Linq);
                That(op, Is.TypeOf<Or>());
                AreEqual(3, ((Or)op).operands.Count);
            }
        }
        
        [Test]
        public static void TestAnd() {
            // --- literals
            {
                var op = QueryParser.Parse("true&&false", out _);
                AreEqual("true && false", op.Linq);
            } {
                var op = QueryParser.Parse("true&&false&&true", out _);
                AreEqual("true && false && true", op.Linq);
                That(op, Is.TypeOf<And>());
                AreEqual(3, ((And)op).operands.Count);
            }
        }
        
        [Test]
        public static void TestNested() {
            {
                var op = QueryParser.Parse("a*b+c", out _);
                AreEqual("a * b + c", op.Linq);
                That(op, Is.TypeOf<Add>());
            } {
                var op = QueryParser.Parse("a+b*c", out _);
                AreEqual("a + b * c", op.Linq);
                That(op, Is.TypeOf<Add>());
            }
        }
    }
}