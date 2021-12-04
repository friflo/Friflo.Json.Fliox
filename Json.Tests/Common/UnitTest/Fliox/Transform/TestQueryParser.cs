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
            string error;
            var result = lexer.Tokenize("1", out error);
            AreEqual("1", result.ToString());
            
            result = lexer.Tokenize("+", out error);
            AreEqual("+", result.ToString());
        }
    }
}