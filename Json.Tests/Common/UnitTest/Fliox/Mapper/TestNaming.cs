// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Linq;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

#pragma warning disable 649 // Field 'field' is never assigned

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    public class TestNaming : LeakTestsFixture
    {
        [NamingPolicy(NamingPolicyType.CamelCase)]
        class Naming {
            public int      lower;
            public int      Upper;

            // ignored members
            [Json.Fliox.Ignore]
            public int      ignoredField;
            
            [Json.Fliox.Ignore]
            public int      ignoredProperty { get; set; }

            // custom member names
            [Serialize       ("field")]
            public int         namedField;
            
            [Serialize       ("property")]
            public int         namedProperty { get; set; }
        }
        
        [Test]
        public void CamelCase() {
            string json = @"
            {
                ""property"":   10,
                ""lower"":      11,
                ""upper"":      12,
                ""field"":      13
            }";
            using (var typeStore =  new TypeStore(new StoreConfig())) // ,CamelCaseNaming.Instance)))
            using (var m = new ObjectMapper(typeStore)) {
                var naming = m.Read<Naming>(json);
                var result = m.Write(naming);
                string expect = string.Concat(json.Where(c => !char.IsWhiteSpace(c)));
                
                AreEqual(expect, result);
            }
        }
        
        [NamingPolicy(NamingPolicyType.CamelCase)]
        class CamelCaseClass {
            public int      lowerProperty { get; set; }
            public int      UpperProperty { get; set; }
            
            public int      lowerField;
            public int      UpperField;
        }
        
        [Test] public void TestCamelCase()
        {
            var camelCase = new CamelCaseClass { lowerProperty = 1, UpperProperty = 2, lowerField = 3, UpperField = 4,  };
            var json = JsonSerializer.Serialize(camelCase, new SerializerOptions { Pretty = false });
            AreEqual("{\"lowerProperty\":1,\"upperProperty\":2,\"lowerField\":3,\"upperField\":4}", json);
            
            var result = JsonSerializer.Deserialize<CamelCaseClass>(json);
            AreEqual(1, result.lowerProperty);
            AreEqual(2, result.UpperProperty);
            AreEqual(3, result.lowerField);
            AreEqual(4, result.UpperField);
        }
        
        [NamingPolicy(NamingPolicyType.PascalCase)]
        class PascalCaseClass {
            public int      lowerProperty { get; set; }
            public int      UpperProperty { get; set; }
            
            public int      lowerField;
            public int      UpperField;
        }

        
        [Test] public void TestPascalCase()
        {
            var camelCase = new PascalCaseClass { lowerProperty = 1, UpperProperty = 2, lowerField = 3, UpperField = 4,  };
            var json = JsonSerializer.Serialize(camelCase, new SerializerOptions { Pretty = false });
            AreEqual("{\"LowerProperty\":1,\"UpperProperty\":2,\"LowerField\":3,\"UpperField\":4}", json);
            
            var result = JsonSerializer.Deserialize<PascalCaseClass>(json);
            AreEqual(1, result.lowerProperty);
            AreEqual(2, result.UpperProperty);
            AreEqual(3, result.lowerField);
            AreEqual(4, result.UpperField);
        }
        
    }
}