// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst;
using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Flow.Schema.Validation;
using Friflo.Json.Flow.UserAuth;
using Friflo.Json.Tests.Common.UnitTest.Flow.Schema.Misc;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema.Validation
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TestJsonValidator : LeakTestsFixture
    {
        static readonly string JsonSchemaFolder = CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore";
        
        [Test]
        public static void Run() {
            var schemas         = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var jsonSchema      = new JsonTypeSchema(schemas);
            using (var parser   = new Local<JsonParser>())
            using (var schema   = new ValidationSchema(jsonSchema))
            using (var validator= new JsonValidator()) {
                var roleJsonType    = SchemaTest.JsonTypeFromType (typeof(Role), "Friflo.Json.Flow.UserAuth.");
                var roleTypeDef     = jsonSchema.TypeAsTypeDef(roleJsonType);  
                var roleValidation  = schema.TypeAsValidationType(roleTypeDef);
                var json = "{}";
                validator.Validate(ref parser.value, json, roleValidation, out _);
            }
        }
    }
}