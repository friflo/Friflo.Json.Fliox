// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
{
    public static class LoadSchema
    {
        [Test]
        public static void UserStore () {
            JsonTypeSchema.FromFolder(CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore");
        }
        
        [Test]
        public static void PocStore () {
            JsonTypeSchema.FromFolder(CommonUtils.GetBasePath() + "assets/Schema/JSON/PocStore");
        }
    }
}