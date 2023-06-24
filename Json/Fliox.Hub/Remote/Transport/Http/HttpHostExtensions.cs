// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    public static class HttpHostExtensions
    {
        public static void UseStaticFiles(this HttpHost httpHost, string folder) {
            httpHost.AddHandler(new StaticFileHandler(folder));
        }
    }
}