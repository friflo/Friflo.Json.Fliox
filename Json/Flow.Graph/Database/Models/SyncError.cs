// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Flow.Database.Models
{
    public readonly struct SyncError
    {
        public readonly     SyncErrorType   type;
        public readonly     string          container;
        public readonly     string          id;
        public readonly     string          message;

        public override     string  ToString() {
            switch (type) {
                case SyncErrorType.ParseError: return $"Failed parsing entity: {container} '{id}', {message}";
                default:
                    throw new InvalidOperationException($"missing ToString for type: {type}");
            }
        }

        public SyncError(SyncErrorType type, string container, string  id, string message) {
            this.type       = type;
            this.container  = container;
            this.id         = id;
            this.message    = message;
        }
    }

    public enum SyncErrorType
    {
        ParseError
    }
}