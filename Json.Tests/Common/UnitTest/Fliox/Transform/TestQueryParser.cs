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
            var parser = new QueryParser();
            string error;
            // --- literals
            {
                var op = parser.Parse("1", out error);
                AreEqual("1", op.Linq);
            } {
                var op = parser.Parse("1.2", out error);
                AreEqual("1.2", op.Linq);
            } {
                var op = parser.Parse("'abc'", out error);
                AreEqual("'abc'", op.Linq);
            } {
                var op = parser.Parse("true", out error);
                That(op, Is.TypeOf<TrueLiteral>());
            } {
                var op = parser.Parse("false", out error);
                That(op, Is.TypeOf<FalseLiteral>());
            } {
                var op = parser.Parse("null", out error);
                That(op, Is.TypeOf<NullLiteral>());
            }
            /*
            // --- arithmetic operations
            {
                var op = parser.Parse("1+2", out error);
                AreEqual("1 + 2", op.Linq);
            } {
                var op = parser.Parse("1-2", out error);
                AreEqual("1 - 2", op.Linq);
            } {
                var op = parser.Parse("1*2", out error);
                AreEqual("1 * 2", op.Linq);
            } {
                var op = parser.Parse("1/2", out error);
                AreEqual("1 / 2", op.Linq);
            }
            // --- comparison operation
            {
                var op = parser.Parse("1<2", out error);
                AreEqual("1 < 2", op.Linq);
            } {
                var op = parser.Parse("1<=2", out error);
                AreEqual("1 <= 2", op.Linq);
            } {
                var op = parser.Parse("1>2", out error);
                AreEqual("1 > 2", op.Linq);
            } {
                var op = parser.Parse("1>=2", out error);
                AreEqual("1 >= 2", op.Linq);
            } {
                var op = parser.Parse("1==2", out error);
                AreEqual("1 == 2", op.Linq);
            } {
                var op = parser.Parse("1!=2", out error);
                AreEqual("1 != 2", op.Linq);
            } */
        }
        
        // [Test]
        public static void TestLogicalOperations() {
            var parser = new QueryParser();
            string error;
            // --- literals
            {
                var op = parser.Parse("true||false", out error);
                AreEqual("true || false", op.Linq);
            } {
                var op = parser.Parse("true||1<2||4>3", out error);
                AreEqual("true || 1 < 2 || 4 > 3", op.Linq);
            }
        }
    }
}