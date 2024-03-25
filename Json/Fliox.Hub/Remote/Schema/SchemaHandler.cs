// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Utils;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Language;
using static System.Diagnostics.DebuggerBrowsableState;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote.Schema
{
    public delegate byte[] CreateZip(IDictionary<string, string> files);

    internal sealed class SchemaHandler : IRequestHandler
    {
        private   const     string                              SchemaBase  = "/schema";
        internal            string                              image       = "explorer/img/Json-Fliox-53x43.svg";
        internal  readonly  CreateZip                           zip;
        [DebuggerBrowsable(Never)]
        private   readonly  Dictionary<string, SchemaResource>  schemas         = new Dictionary<string, SchemaResource>();
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             IReadOnlyCollection<SchemaResource> Schemas         => schemas.Values;
        private   readonly  List<CustomGenerator>               generators      = new List<CustomGenerator>();
        private             string                              cacheControl    = HttpHost.DefaultCacheControl;
        internal            ICollection<CustomGenerator>        Generators      => generators;

        public    override  string                              ToString()      => $"{SchemaBase} schemas: {schemas.Count}";

        internal SchemaHandler() {
            this.zip = ZipUtils.Zip;
        }
        
        public string CacheControl {
            get => cacheControl;
            set => cacheControl   = value; 
        }
        
        public string[]  Routes => new []{ SchemaBase };

        public bool IsMatch(RequestContext context) {
            if (context.method != "GET")
                return false;
            return RequestContext.IsBasePath(SchemaBase, context.route);
        }
        
        public Task<bool> HandleRequest(RequestContext context) {
            if (context.route.Length == SchemaBase.Length) {
                context.WriteError("invalid schema path", "missing database / protocol name", 400);
                return Task.FromResult(true);
            }
            var hub         = context.hub;
            var route       = context.route.Substring(SchemaBase.Length + 1);
            var firstSlash  = route.IndexOf('/');
            var name        = firstSlash == -1 ? route : route.Substring(0, firstSlash);
            var schema      = GetSchemaResource(hub, name, out string error);
            if (schema == null) {
                context.WriteError(error, name, 404);
                return Task.FromResult(true);
            }
            var schemaPath  = route.Substring(firstSlash + 1);
            var result      = schema.GetSchemaFile(schemaPath, this, context);
            if (!result.success) {
                context.WriteError("schema error", result.content, 404);
                return Task.FromResult(true);
            }
            if (cacheControl != null) {
                context.AddHeader("Cache-Control", cacheControl); // seconds
            }
            if (result.isText) {
                context.WriteString(result.content, result.contentType, 200);
                return Task.FromResult(true);
            }
            context.Write(result.bytes, result.contentType, 200);
            return Task.FromResult(true);
        }
        
        private SchemaResource GetSchemaResource(FlioxHub hub, string name, out string error) {
            if (schemas.TryGetValue(name, out var schema)) {
                error = null;
                return schema;
            }
            if (!hub.TryGetDatabase(name, out var database)) {
                error = "schema not found";
                return null;
            }
            var typeSchema = database.Schema.typeSchema;
            if (typeSchema == null) {
                error = "missing schema for database";
                return null;
            }
            error = null;
            return AddSchema(name, typeSchema);
        }
        
        internal SchemaResource AddSchema(string name, TypeSchema typeSchema, ICollection<TypeDef> sepTypes = null) {
            sepTypes    = sepTypes ?? typeSchema.GetEntityTypes().Values;
            var schema  = new SchemaResource(name, typeSchema, sepTypes);
            schemas.Add(name, schema);
            return schema;
        }
        
        internal void AddGenerator(string type, string name, SchemaGenerator schemaGenerator) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            var generator = new CustomGenerator(type, name, schemaGenerator);
            generators.Add(generator);
        }
    }
    
    internal sealed class ModelResource {
        internal  readonly  SchemaModel     schemaModel;
        internal  readonly  string          zipNameSuffix;  // .csharp.zip, json-schema.zip, ...
        private             byte[]          zipArchive;
        internal  readonly  JsonValue       fullSchema;

        public    override  string          ToString() => schemaModel.type;

        internal ModelResource(SchemaModel schemaModel, in JsonValue fullSchema) {
            this.schemaModel    = schemaModel;
            this.fullSchema     = fullSchema;
            zipNameSuffix       = $".{schemaModel.type}.zip";
        }
        
        internal byte[] GetZipArchive (CreateZip zip) {
            if (zipArchive == null && zip != null ) {
                zipArchive = zip(schemaModel.files);
            }
            return zipArchive;
        }
    }

    internal readonly struct SchemaResult {
        internal  readonly  bool        success;
        internal  readonly  string      content;
        internal  readonly  string      contentType;
        internal  readonly  JsonValue   bytes;
        internal  readonly  bool        isText;
        
        private SchemaResult (string content, string contentType, in JsonValue bytes, bool isText, bool success) {
            this.content        = content;
            this.contentType    = contentType;
            this.bytes          = bytes;
            this.isText         = isText;
            this.success        = success;
        }
        
        internal static SchemaResult Success(string  content, string  contentType) {
            return new SchemaResult(content, contentType, default, true, true);
        }
        
        internal static  SchemaResult Success(in JsonValue content, string  contentType) {
            return new SchemaResult(null, contentType, content, false, true);
        }
        
        internal static SchemaResult Error(string  content) {
            return new SchemaResult(content, "text/plain", default, true, false);
        }
    }
}