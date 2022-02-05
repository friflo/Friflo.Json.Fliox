// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Hub.Explorer
{
    public static class HubExplorer
    {
        public static string FolderName => "www~";
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