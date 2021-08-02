// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst;
using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Flow.Schema.Validation;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema.Validation
{
    public static class TestJsonValidator
    {
        static readonly string JsonSchemaFolder = CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore";
        
        // [Test]
        public static void Run() {
            var schemas     = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var jsonSchema      = new JsonTypeSchema(schemas);
            using (var parser   = new Local<JsonParser>())
            using (var schema   = new ValidationSchema(jsonSchema)) {
                var validator   = new JsonValidator();
                validator.Validate(ref parser.value, schema);
            }
        }
    }
}