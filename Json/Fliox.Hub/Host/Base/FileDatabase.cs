// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Utils;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// A <see cref="FileDatabase"/> is used to store the entities / records of its containers as <b>JSON</b>
    /// files in the <b>file-system</b>.
    /// </summary>
    /// <remarks>
    /// Each database container / table is a sub folder of the path passed to the <see cref="FileDatabase"/> constructor.<br/>
    /// The intention of a <see cref="FileDatabase"/> is providing <b>out of the box</b> persistence without the need of
    /// installation or configuration of a third party database server like: SQLite, Postgres, ...<br/>
    /// This enables the following uses cases
    /// <list type="bullet">
    ///   <item>Creating <b>proof-of-concept</b> database applications without any third party dependencies</item>
    ///   <item>Suitable for <b>TDD</b> as test records are JSON files versioned via Git and providing access to their change history</item>
    ///   <item>Using a database <b>without configuration</b> by using a relative database path within a project</item>
    ///   <item>Viewing and editing database records as JSON files with <b>text editors</b> like VSCode, vi, web browsers, ...<br/></item>
    ///   <item>Using a <see cref="FileDatabase"/> as data source to <b>seed</b> other databases with <see cref="EntityDatabase.SeedDatabase"/></item>
    /// </list>
    /// In most uses cases a <see cref="FileDatabase"/> in not suitable for production as its read / write performance
    /// cannot compete with databases like: SQLite, Postgres, ... . <br/>
    /// <see cref="FileDatabase"/> has no third party dependencies.
    /// </remarks>
    public sealed class FileDatabase : EntityDatabase
    {
        public   bool                   Pretty { get; init; } = true;
        
        private  readonly   string      databaseFolder;
        public   override   string      StorageType => "file-system";
        
        public FileDatabase(string dbName, string databaseFolder, DatabaseSchema schema = null, DatabaseService service = null)
            : base(dbName, schema, service)
        {
            this.databaseFolder = databaseFolder + "/";
            Directory.CreateDirectory(databaseFolder);
        }

        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new FileContainer(name.AsString(), this, databaseFolder, Pretty);
        }
        
        protected override Task<string[]> GetContainers() {
            var directories = Directory.GetDirectories(databaseFolder);
            var result = new string[directories.Length];
            for (int n = 0; n < directories.Length; n++) {
                result[n] = directories[n].Substring(databaseFolder.Length);
            }
            return Task.FromResult(result);
        }
        
        public override async Task DropDatabaseAsync() {
            await DropAllContainersAsync().ConfigureAwait(false);
        }
        
        protected override Task DropContainerAsync(ISyncConnection connection, string name) {
            var dir = new DirectoryInfo(databaseFolder + name);
            dir.Delete(true);
            return Task.CompletedTask;
        }
    }
    

    internal static class FileUtils
    {
        // -------------------------------------- helper methods --------------------------------------
        internal static string GetHResultDetails(int result) {
            var lng = result & 0xffffffffL;
            switch (lng) {
                case 0x0000007B:   return "invalid file name";
                case 0x80070002:   return "file not found";
                case 0x80070050:   return "file already exists";
                case 0x80070052:   return "file cannot be created";
                case 0x80070570:   return "file corrupt";
            }
            return null;
        }
        
        /// <summary>
        /// Write with <see cref="FileShare.Read"/> as on a developer machine other processes like virus scanner or file watcher
        /// may access the file concurrently resulting in:
        /// IOException: The process cannot access the file 'path' because it is being used by another process
        /// </summary>
        internal static async Task WriteText(string filePath, JsonValue json, FileMode fileMode) {
            using (var destStream = new FileStream(filePath, fileMode, FileAccess.Write, FileShare.Read, bufferSize: 4096, useAsync: false)) {
                await destStream.WriteAsync(json).ConfigureAwait(false);
                await destStream.FlushAsync().ConfigureAwait(false);
            }
        }
        
        internal static async Task<JsonValue> ReadText(string filePath, StreamBuffer buffer, MemoryBuffer memoryBuffer) {
            using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: false)) {
                var value = await KeyValueUtils.ReadToEndAsync(sourceStream, buffer).ConfigureAwait(false);
                return KeyValueUtils.CreateCopy(value, memoryBuffer);
            }
        }
        
        internal static void DeleteFile(string filePath) {
            File.Delete(filePath);
        }
    }
    
    internal sealed class FileQueryEnumerator : QueryEnumerator
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly    string              folder; // keep there for debugging
        private readonly    int                 folderLen;
        private readonly    IEnumerator<string> enumerator;
        private readonly    StreamBuffer        buffer = new StreamBuffer();
        private readonly    MemoryBuffer        memoryBuffer;
            
        internal FileQueryEnumerator (string folder, MemoryBuffer memoryBuffer)
        {
            this.folder         = folder;
            folderLen           = folder.Length;
            this.memoryBuffer   = memoryBuffer; 
#if UNITY_2020_1_OR_NEWER  || NETSTANDARD2_0
            enumerator = Directory.EnumerateFiles(folder, "*.json", SearchOption.TopDirectoryOnly).GetEnumerator();
#else
            var options = new EnumerationOptions {
                MatchCasing                 = MatchCasing.CaseSensitive,
                MatchType                   = MatchType.Simple,
                RecurseSubdirectories       = false,
                AttributesToSkip            = FileAttributes.System, // include Hidden files
                ReturnSpecialDirectories    = false,
                IgnoreInaccessible          = true
            };
            enumerator = Directory.EnumerateFiles(folder, "*.json", options).GetEnumerator();
#endif
        }
            
        public override bool MoveNext() {
            return enumerator.MoveNext();
        }

        public override JsonKey Current { get {
            var fileName = enumerator.Current;
            var len = fileName.Length;
            var id = fileName.Substring(folderLen, len - folderLen - ".json".Length);
            return new JsonKey(id);
        } }
        
        protected override void DisposeEnumerator() {
            enumerator.Dispose();
        }
        
        public async       Task<JsonValue> CurrentValueAsync() { 
            var path    = enumerator.Current;
            return await FileUtils.ReadText(path, buffer, memoryBuffer).ConfigureAwait(false);
        }
    }
}
