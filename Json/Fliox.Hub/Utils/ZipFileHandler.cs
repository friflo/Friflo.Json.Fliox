// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Hub.Utils
{
    internal sealed class  ZipFileHandler : IFileHandler {
        private readonly    Dictionary<string, byte[]>          files   = new Dictionary<string, byte[]>();
        private readonly    Dictionary<string, List<string>>    folders = new Dictionary<string, List<string>>();
        
        public              string[]                            BaseFolders => throw new NotImplementedException();
        
        public static ZipFileHandler Create (string zipPath, string baseFolder) {
            var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 0x1000, false);
            return new ZipFileHandler(fs, baseFolder);
        }
        
        // ReSharper disable once MemberCanBePrivate.Global
        public ZipFileHandler (Stream zipStream, string baseFolder)
        {
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read, false)) {
                using (var ms = new MemoryStream()) {
                    foreach (ZipArchiveEntry entry in archive.Entries) {
                        var path    = entry.FullName;
                        if (!path.StartsWith(baseFolder))
                            continue;
                        path = path.Substring(baseFolder.Length);
                        if (path.EndsWith("/")) {
                            continue;
                        }
                        var fileName    = entry.Name;
                        var folderName  = path.Substring(0, path.Length - fileName.Length - 1);
                        if (!folders.TryGetValue(folderName, out var folderFiles)) {
                            folderFiles = new List<string>();
                            folders.Add(folderName, folderFiles);
                        }
                        folderFiles.Add(path);

                        ms.SetLength(0);
                        using (var stream = entry.Open()) {
                            stream.CopyTo(ms);
                            var content = ms.ToArray();
                            files.Add(path, content);
                        }
                    }
                }
            } 
        } 
        
        public Task<byte[]> ReadFile(string path) {
            if (!files.TryGetValue(path, out byte[] content))
                return null;
            return Task.FromResult(content);
        }
        
        public string[] GetFiles(string folder) {
            if (!folders.TryGetValue(folder, out var folderFiles))
                return Array.Empty<string>();
            return folderFiles.ToArray();
        }
    }
}