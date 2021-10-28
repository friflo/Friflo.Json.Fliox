// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


namespace Friflo.Json.Fliox.Hub.Host
{
    public class ExtensionDatabase : EntityDatabase
    {
        public ExtensionDatabase (FlioxHub hub, string extensionName, TaskHandler handler = null, DbOpt opt = null)
            : base (hub, extensionName, handler, opt)
        { }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return null; // new ExtensionContainer(name, database);
        }
    }
    /*
    internal class ExtensionContainer : EntityContainer
    {
        public ExtensionContainer(string name, EntityDatabase database) : base(name, database) {
        }

        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, MessageContext messageContext) {
            throw new NotImplementedException();
        }

        public override Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, MessageContext messageContext) {
            throw new NotImplementedException();
        }

        public override Task<ReadEntitiesSetResult> ReadEntitiesSet(ReadEntitiesSet command, MessageContext messageContext) {
            throw new NotImplementedException();
        }

        public override Task<QueryEntitiesResult> QueryEntities(QueryEntities command, MessageContext messageContext) {
            throw new NotImplementedException();
        }

        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, MessageContext messageContext) {
            throw new NotImplementedException();
        }
    }
    */
}