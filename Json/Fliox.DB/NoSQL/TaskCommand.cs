// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.NoSQL
{
    public readonly struct Command<TValue> {
        public              string          Name    { get; }
        public              TValue          Value   => reader.Read<TValue>(json);
        
        private  readonly   Utf8Array       json;
        private  readonly   ObjectReader    reader;

        public   override   string          ToString() => Name;

        internal Command(string name, Utf8Array json, ObjectReader reader) {
            Name        = name;
            this.json   = json;  
            this.reader = reader;
        }
    }
    
    public delegate TResult CommandHandler<TValue, out TResult>(Command<TValue> command);
    
    internal abstract class CommandCallback
    {
        internal abstract Task<Utf8Array> InvokeCallback(ObjectMapper mapper, string messageName, JsonValue messageValue);
    }
    
    internal class CommandCallback<TValue, TResult> : CommandCallback
    {
        private  readonly   string                              name;
        private  readonly   CommandHandler<TValue, TResult>     handler;

        public   override   string                              ToString() => name;

        internal CommandCallback (string name, CommandHandler<TValue, TResult> handler) {
            this.name       = name;
            this.handler    = handler;
        }
        
        internal override Task<Utf8Array> InvokeCallback(ObjectMapper mapper, string messageName, JsonValue messageValue) {
            var     cmd     = new Command<TValue>(messageName, messageValue.json, mapper.reader);
            TResult result  = handler(cmd);
            var jsonResult  = mapper.WriteAsArray(result);
            return Task.FromResult(new Utf8Array(jsonResult));
        }
    }
    
    internal class CommandAsyncCallback<TValue, TResult> : CommandCallback
    {
        private  readonly   string                                  name;
        private  readonly   CommandHandler<TValue, Task<TResult>>   handler;

        public   override   string                                  ToString() => name;

        internal CommandAsyncCallback (string name, CommandHandler<TValue, Task<TResult>> handler) {
            this.name       = name;
            this.handler    = handler;
        }
        
        internal override async Task<Utf8Array> InvokeCallback(ObjectMapper mapper, string messageName, JsonValue messageValue) {
            var     cmd     = new Command<TValue>(messageName, messageValue.json, mapper.reader);
            TResult result  = await handler(cmd).ConfigureAwait(false);
            var jsonResult  = mapper.WriteAsArray(result);
            return new Utf8Array(jsonResult);
        }
    }
}