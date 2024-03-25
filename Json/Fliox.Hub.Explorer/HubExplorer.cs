// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Hub.Explorer
{
    /// <summary>
    /// <see cref="HubExplorer"/> provide the path to the static Web files for the <b>Hub Explorer</b>.<br/>
    /// The explorer features are listed at:
    /// <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Json/Fliox.Hub.Explorer/README.md">Explorer README.md</a>
    /// </summary>
    public static class HubExplorer
    {
        public static string FolderName => "www~";
        /// Return the path of static web files for the Hub Explorer.
        /// The Hub Explorer is a SPA used for development and administration of a Fliox Hub.
        public static string Path       => GetPath();
        
        private static string GetPath() {
            // return System.AppDomain.CurrentDomain.BaseDirectory + FolderName;
            
            var assembly = typeof(HubExplorer).Assembly;
            if (assembly == null)
                throw new InvalidOperationException("HubExplorer.Path failed");
            var folder = System.IO.Path.GetDirectoryName(assembly.Location);
            return folder + "/" + FolderName;
        }
    }
}