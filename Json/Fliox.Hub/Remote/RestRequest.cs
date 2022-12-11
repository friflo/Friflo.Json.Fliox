// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Specialized;

namespace Friflo.Json.Fliox.Hub.Remote
{
    internal enum RestRequestType
    {
        error,
        command,
        message,
        read,
        delete,
        query,
        readOne,
        write,
        merge
    }
    
    internal readonly struct RestRequest
    {
        internal  readonly  RestRequestType     type;
        // --- path
        internal  readonly  int                 length;
        private   readonly  string              path;
        
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
        
        internal RestRequest(RestRequestType type, string database, string message, in JsonValue value) {
            this.type       = type;
            length          = 2;
            path            = "/";
            this.database   = database;
            container       = null;
            this.message    = message;
            this.value      = value;
            id              = null;
            errorType       = null;
            errorMessage    = null;
            errorStatus     = 0;
            keys            = null;
            queryParams     = null;
        }
        
        internal RestRequest(string errorType, string errorMessage, int errorStatus) {
            type                = RestRequestType.error;
            length              = 0;
            path                = "/";
            database            = null;
            container           = null;
            message             = null;
            value               = default;
            id                  = null;
            this.errorType      = errorType;
            this.errorMessage   = errorMessage;
            this.errorStatus    = errorStatus;
            keys                = null;
            queryParams         = null;
        }
    }
}