// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Friflo.Json.Fliox.Hub.Utils
{
    internal interface IFileHandler {
        Task<byte[]>    ReadFile(string path);
        string[]        GetFiles(string folder);
        string[]        BaseFolders { get; }
    }
    
    internal sealed class  FileHandler : IFileHandler {
        private     readonly    string      rootFolder;
        public                  string[]    BaseFolders { get; }

        internal FileHandler (string rootFolder) {
            this.rootFolder = rootFolder;
            BaseFolders     = GetBaseFolders();
        }
        
        public async Task<byte[]> ReadFile(string path) {
            var filePath    = rootFolder + path;
            var fileInfo    = new FileInfo(filePath);
            if (!fileInfo.Exists) {
                return null;
            }
            using (var sourceStream = fileInfo.OpenRead()) {
                var memoryStream    = new MemoryStream();
                byte[] buffer       = new byte[0x1000];
                int numRead;
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) != 0) {
                    memoryStream.Write(buffer, 0, numRead);
                }
                return memoryStream.ToArray();
            }
        }
        
        public string[] GetFiles(string folder) {
            var path = rootFolder + folder;
            if (!Directory.Exists(path)) {
                return null;
            }
            string[] fileNames = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
            for (int n = 0; n < fileNames.Length; n++) {
                fileNames[n] = fileNames[n].Substring(path.Length + 1);
            }
            return fileNames;
        }
        
        private string[] GetBaseFolders() {
            var path = rootFolder;
            if (!Directory.Exists(rootFolder)) {
                return Array.Empty<string>();
            }
            string[] folderNames = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
            for (int n = 0; n < folderNames.Length; n++) {
                folderNames[n] = "/" + folderNames[n].Substring(path.Length + 1);
            }
            return folderNames;
        }
    }
}