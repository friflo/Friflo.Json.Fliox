// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Database
{
    public readonly struct Command<TValue> {
        public              string          Name    { get; }
        public              TValue          Value   => reader.Read<TValue>(json);
        
        private readonly    string          json;
        
        private readonly    ObjectReader    reader;
        
        internal Command(string name, string json, ObjectReader reader) {
            Name        = name;
            this.json   = json;  
            this.reader = reader;
        }
    }
    
    public delegate TResult CommandHandler<TValue, out TResult>(Command<TValue> command);
    
    internal abstract class CommandCallback
    {
        internal abstract Task<string> InvokeCallback(ObjectMapper mapper, string messageName, JsonValue messageValue);
    }
    
    internal class CommandCallback<TValue, TResult> : CommandCallback
    {
        private  readonly   CommandHandler<TValue, Task<TResult>>   handler;
        
        internal CommandCallback (string name, CommandHandler<TValue, Task<TResult>> handler) {
            this.handler = handler;
        }
        
        internal override async Task<string> InvokeCallback(ObjectMapper mapper, string messageName, JsonValue messageValue) {
            var     cmd     = new Command<TValue>(messageName, messageValue.json, mapper.reader);
            TResult result  = await handler(cmd);
            var jsonResult  = mapper.Write(result);
            return jsonResult;
        }
    }
}