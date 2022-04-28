// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System.Threading.Tasks;
using NUnit.Framework;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    public partial class TestRemote
    {
        [Test]
        public static async Task Static_index() {
            var request = await RestRequest("GET", "/");
            AssertRequest(request, 200, "text/html; charset=UTF-8");
        }
        
        [Test]
        public static async Task Static_explorer() {
            var request = await RestRequest("GET", "/explorer");
            AssertRequest(request, 200, "application/json", "[\n]");
        }
        
        [Test]
        public static async Task Static_swagger() {
            var request = await RestRequest("GET", "/swagger");
            AssertRequest(request, 200, "application/json");
        }
    }
}

#endif
