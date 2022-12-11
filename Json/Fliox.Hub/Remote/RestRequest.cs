// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Specialized;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Remote
{
    internal enum RestRequestType
    {
        error,
        // message / command
        command,
        message,
        // --- container operation
        read,
        readOne,
        query,
        write,
        merge,
        delete
    }
    
    internal readonly struct RestRequest
    {
        internal  readonly  RestRequestType     type;
        // --- path
        internal  readonly  string              database;
        internal  readonly  string              container;
        internal  readonly  string              id;

        internal  readonly  string              errorType;
        internal  readonly  string              errorMessage;
        internal  readonly  int                 errorStatus;
        
        // --- message / command
        internal  readonly  string              message;
        internal  readonly  JsonValue           value;
        
        // --- container operations
        internal  readonly  JsonKey[]           keys;
        
        internal  readonly  NameValueCollection queryParams;

        public    override  string              ToString() => GetString();

        /// <summary>
        /// create request specific for <see cref="RestRequestType.message"/> and <see cref="RestRequestType.command"/>
        /// </summary>
        internal RestRequest(RestRequestType type, string database, string name, in JsonValue value) {
            this.type           = type;
            this.database       = database;
                 container      = null;
            this.message        = name;
            this.value          = value;
                 id             = null;
                 errorType      = null;
                 errorMessage   = null;
                 errorStatus    = 0;
                 keys           = null;
                 queryParams    = null;
        }
        
        /// <summary>
        /// create request for a database container operation
        /// </summary>
        internal RestRequest(RestRequestType type, string database, string container, JsonKey[] keys) {
            this.type           = type;
            this.database       = database;
            this.container      = container;
                 message        = null;
                 value          = default;
                 id             = null;
                 errorType      = null;
                 errorMessage   = null;
                 errorStatus    = 0;
            this.keys           = keys;
                 queryParams     = null;
        }
        
        /// <summary>
        /// create request for a database container operation
        /// </summary>
        internal RestRequest(RestRequestType type, string database, string container, string id, in JsonValue value, NameValueCollection queryParams) {
            this.type           = type;
            this.database       = database;
            this.container      = container;
                 message        = null;
            this.value          = value;
            this.id             = id;
                 errorType      = null;
                 errorMessage   = null;
                 errorStatus    = 0;
                 keys           = null;
            this.queryParams    = queryParams;
        }
        
        /// <summary>
        /// create request for <see cref="RestRequestType.error"/>'s
        /// </summary>
        internal RestRequest(string errorType, string errorMessage, int errorStatus) {
                 type           = RestRequestType.error;
                 database       = null;
                 container      = null;
                 message        = null;
                 value          = default;
                 id             = null;
            this.errorType      = errorType;
            this.errorMessage   = errorMessage;
            this.errorStatus    = errorStatus;
                 keys           = null;
                 queryParams    = null;
        }
        
        private string GetString() {
            switch (type) {
                case RestRequestType.command:   return $"command {database} {message}({value})";
                case RestRequestType.message:   return $"message {database} {message}({value})";
                
                case RestRequestType.read:      return $"read {database}/{container}";
                case RestRequestType.query:     return $"query {database}/{container}";
                case RestRequestType.readOne:   return $"readOne {database}/{container}/{id}";
                case RestRequestType.delete:    return $"delete {database}/{container}";
                case RestRequestType.write:     return $"write {database}/{container}";
                case RestRequestType.merge:     return $"merge {database}/{container}";

                case RestRequestType.error:     return $"error {errorStatus} {errorType} {errorType}";
            }
            return null;
        }
    }
}