// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Protocol.Models;
using Friflo.Json.Fliox.DB.Protocol.Tasks;


namespace Friflo.Json.Fliox.DB.Host.Internal
{
    internal class ExtensionDatabase : EntityDatabase
    {
        internal ExtensionDatabase (FlioxHub hub, string extensionName, TaskHandler taskHandler, DbOpt opt)
            : base (hub, extensionName, taskHandler, opt)
        {
            // extensionBase.extensionDbs.Add(extensionName, this);
            // local = extensionBase.extensionDbs[extensionName];
        }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new ExtensionContainer(name, database);
        }
    }
    
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
}