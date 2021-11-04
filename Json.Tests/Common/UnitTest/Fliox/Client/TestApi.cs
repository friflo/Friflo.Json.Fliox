// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class TestApi
    {
        [Test]
        public void EnsurePassingTypeStoreToSharedEnv() {
            using (var typeStore    = new TypeStore())
            using (var _            = new SharedEnv(typeStore)) {
            }
        }
    }
}