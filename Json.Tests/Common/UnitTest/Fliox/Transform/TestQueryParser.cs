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
            AssertToken(lexer, "-1");
            AssertToken(lexer, "+1", "1");
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
        }
        
        private static void AssertToken (QueryLexer lexer, string str, string expect = null) {
            var result = lexer.Tokenize(str, out string _);
            AreEqual(1,     result.items.Length);
            expect = expect ?? str;
            AreEqual(expect,   result.ToString());
        }
    }
}