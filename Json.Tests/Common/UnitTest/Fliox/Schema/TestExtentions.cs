// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Validation;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema
{
    public static class SchemaExtensions
    {
        public static bool ValidateObject (this TypeValidator validator, string json, ValidationType type, out string error) {
            return validator.ValidateObject(new Utf8Array(json), type, out error);
        }
    }
}