// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client.Event;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public sealed class EventContext
    {
        public  readonly    JsonKey                 srcUserId;
        public              int                     EventSequence { get; }
        public              IReadOnlyList<Message>  Messages    => processor.Messages;

        private readonly    SubscriptionProcessor   processor;

        public  override    string                  ToString()  => $"source user: {srcUserId}";

        internal EventContext(SubscriptionProcessor processor, in JsonKey srcUserId) {
            this.processor  = processor;
            this.srcUserId  = srcUserId;
            EventSequence   = processor.EventSequence;
        }
        
        public EntityChanges<TKey, T> GetChanges<TKey, T>(EntitySet<TKey, T> entitySet) where T : class {
            return (EntityChanges<TKey, T>)processor.GetChanges(entitySet);
        } 
    }
}