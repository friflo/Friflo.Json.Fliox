// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    public static class HttpHostExtensions
    {
        /// <summary>
        /// Serve static files in the given <paramref name="folder"/> via HTTP by the given <paramref name="httpHost"/>.
        /// </summary>
        /// <remarks>
        /// Its main purpose is to add the static web files of the <b>Hub Explorer</b>.<br/>
        /// As the Hub Explorer is optional and only required by server applications they are published in
        /// a separate nuget package https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.Explorer/<br/>
        /// To add its files use:
        /// <br/>
        /// <code>
        /// httpHost.UseStaticFiles(HubExplorer.Path); // nuget: https://www.nuget.org/packages/Friflo.Json.Fliox.Hub.Explorer
        /// </code>
        /// </remarks>
        public static void UseStaticFiles(this HttpHost httpHost, string folder) {
            httpHost.AddHandler(new StaticFileHandler(folder));
        }
    }
}