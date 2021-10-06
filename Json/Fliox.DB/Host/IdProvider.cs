// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host
{
    /// <summary>
    /// Create a unique id when calling <see cref="NewId"/>.
    /// Its used to create unique client ids by <see cref="EntityDatabase.clientIdProvider"/>
    /// </summary>
    public abstract class IdProvider {
        public abstract JsonKey NewId();
    }
    
    public class IncrementIdProvider : IdProvider {
        private long clientIdSequence;

        public override JsonKey NewId() {
            return new JsonKey(++clientIdSequence);
        }
    }
    
    public class GuidIdProvider : IdProvider {
        public override JsonKey NewId() {
            return new JsonKey(Guid.NewGuid());
        }
    }
}