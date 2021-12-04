// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Transform.Query.Parser;
using NUnit.Framework;
using static NUnit.Framework.Assert;


namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Transform
{
    public static class TestQueryParser
    {
        [Test]
        public static void TestLexer() {
            var lexer = new QueryLexer();
            AssertToken(lexer, ".");
            AssertToken(lexer, "(");
            AssertToken(lexer, ")");
            //
            AssertToken(lexer,  "1");
            AssertToken(lexer,  "123");
            AssertToken(lexer, "-1");
            AssertToken(lexer, "+1", 1, "1");
            //
            AssertToken(lexer, "+");
            AssertToken(lexer, "-");
            AssertToken(lexer, "*");
            AssertToken(lexer, "/");
            //
            AssertToken(lexer, "<");
            AssertToken(lexer, ">");
            AssertToken(lexer, "<=");
            AssertToken(lexer, ">=");
            AssertToken(lexer, "!");
            AssertToken(lexer, "!=");
            //
            AssertToken(lexer, "||");
            AssertToken(lexer, "&&");
            AssertToken(lexer, "==");
            AssertToken(lexer, "=>");
            
            AssertToken(lexer, "abc");
            AssertToken(lexer, "'xyz'");
            
            AssertToken(lexer, "1+1",   3);
            AssertToken(lexer, "a-1",   3);
            AssertToken(lexer, "(1)-1", 5);
            
            AssertToken(lexer, "a.Contains('xyz')", 6);

            AssertToken(lexer, "a=>!((a.b==-1||1<2)&&(-2<=3||3>1||3>=(1-1*1)/1||-1!=2))", 42);
        }
        
        private static void AssertToken (QueryLexer lexer, string str, int len = 1, string expect = null) {
            var result = lexer.Tokenize(str, out string _);
            AreEqual(len,     result.items.Length);
            expect = expect ?? str;
            AreEqual(expect,   result.ToString());
        }
    }
}