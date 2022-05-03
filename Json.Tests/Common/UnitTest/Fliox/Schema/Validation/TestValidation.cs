// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Fliox.Schema.Validation;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Validation
{
    public static class TestValidation
    {
        [Test]
        public static void ValidateNullableValue() {
            var intSchema       = NativeTypeSchema.Create(typeof(int?));
            var intValidation   = new ValidationSet(intSchema);
            var intField        = intValidation.GetValidationField(intSchema, typeof(int?));
            
            using (var validator = new TypeValidator()) {
                bool success = validator.ValidateField(new JsonValue("123"), intField, out string error);
                IsTrue(success);
                
                success = validator.ValidateField(new JsonValue("true"), intField, out error);
                IsFalse(success);
                AreEqual("Incorrect type. was: true, expect: int32 (root), pos: 4", error);
            }
        }
    }
}