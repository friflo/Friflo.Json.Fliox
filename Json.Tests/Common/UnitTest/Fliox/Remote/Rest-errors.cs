// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using NUnit.Framework;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    public partial class TestRemote
    {
        [Test, Order(1)]
        public static void Rest_main_db_errors() {
            ExecuteRestFile("Rest/main_db/errors.http", "Rest/main_db/errors.result.http");
        }
    }
}
