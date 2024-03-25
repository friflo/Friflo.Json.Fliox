// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Specialized;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Remote.Rest
{
    internal enum RestRequestType
    {
        error   = 1,
        // message / command
        command = 2,
        message = 3,
        // --- container operation
        read    = 4,
        readOne = 5,
        query   = 6,
        write   = 7,
        merge   = 8,
        delete  = 9
    }
    
    internal readonly struct RestRequest
    {
        internal  readonly  RestRequestType     type;
        // --- path
        internal  readonly  ShortString         db;
        internal  readonly  ShortString         container;
        internal  readonly  string              id;

        internal  readonly  string              errorType;
        internal  readonly  string              errorMessage;
        internal  readonly  int                 errorStatus;
        
        // --- message / command
        internal  readonly  string              message;
        internal  readonly  JsonValue           value;
        
        // --- container operations
        internal  readonly  JsonKey[]           keys;
        /// query string of the url. E.g. ?cmd=std.Echo
        internal  readonly  NameValueCollection query;

        public    override  string              ToString() => GetString();

        /// <summary>
        /// create request specific for <see cref="RestRequestType.message"/> and <see cref="RestRequestType.command"/>
        /// </summary>
        internal RestRequest(RestRequestType type, string db, string name, in JsonValue value) {
            this.type           = type;
            this.db             = new ShortString(db);
                 container      = default;
            this.message        = name;
            this.value          = value;
                 id             = null;
                 errorType      = null;
                 errorMessage   = null;
                 errorStatus    = 0;
                 keys           = null;
                 query          = null;
        }
        
        /// <summary>
        /// create request for a database container operation
        /// </summary>
        internal RestRequest(RestRequestType type, string db, string container, JsonKey[] keys) {
            this.type           = type;
            this.db             = new ShortString(db);
            this.container      = new ShortString(container);
                 message        = null;
                 value          = default;
                 id             = null;
                 errorType      = null;
                 errorMessage   = null;
                 errorStatus    = 0;
            this.keys           = keys;
                 query          = null;
        }
        
        /// <summary>
        /// create request for a database container operation
        /// </summary>
        internal RestRequest(RestRequestType type, string db, string container, string id, in JsonValue value, NameValueCollection query) {
            this.type           = type;
            this.db             = new ShortString(db);
            this.container      = new ShortString(container);
                 message        = null;
            this.value          = value;
            this.id             = id;
                 errorType      = null;
                 errorMessage   = null;
                 errorStatus    = 0;
                 keys           = null;
            this.query          = query;
        }
        
        /// <summary>
        /// create request for <see cref="RestRequestType.error"/>'s
        /// </summary>
        internal RestRequest(string errorType, string errorMessage, int errorStatus) {
                 type           = RestRequestType.error;
                 db             = default;
                 container      = default;
                 message        = null;
                 value          = default;
                 id             = null;
            this.errorType      = errorType;
            this.errorMessage   = errorMessage;
            this.errorStatus    = errorStatus;
                 keys           = null;
                 query          = null;
        }
        
        private string GetString() {
            switch (type) {
                case RestRequestType.command:   return $"command {db} {message}({value})";
                case RestRequestType.message:   return $"message {db} {message}({value})";
                
                case RestRequestType.read:      return $"read {db}/{container}";
                case RestRequestType.query:     return $"query {db}/{container}";
                case RestRequestType.readOne:   return $"readOne {db}/{container}/{id}";
                case RestRequestType.delete:    return $"delete {db}/{container}";
                case RestRequestType.write:     return $"write {db}/{container}";
                case RestRequestType.merge:     return $"merge {db}/{container}";

                case RestRequestType.error:     return $"error {errorStatus}: {errorType} > {errorMessage}";
            }
            return null;
        }
    }
}