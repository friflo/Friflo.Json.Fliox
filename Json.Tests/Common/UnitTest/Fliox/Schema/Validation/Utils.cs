// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Schema.Validation;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema.Validation
{
    public static class Utils
    {
        public static bool ValidateObject (this TypeValidator validator, string json, ValidationType type, out string error) {
            return validator.ValidateObject(new JsonValue(json), type, out error);
        }
    }
}