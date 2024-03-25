// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    public partial class TestRemote
    {
        [Test, Order(0)]
        public static void Batch_main_db() {
            ExecuteHttpFile("Batch/main_db/sync.http", "Batch/main_db/sync.result.http");
        }
    }
}
