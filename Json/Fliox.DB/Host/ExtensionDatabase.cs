// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Protocol;

namespace Friflo.Json.Fliox.DB.Host
{
    public class ExtensionDatabase : EntityDatabase
    {
        private readonly    EntityDatabase  defaultDatabase;
        private readonly    string          extensionName;
        
        public ExtensionDatabase (EntityDatabase defaultDatabase, string extensionName) {
            this.defaultDatabase    = defaultDatabase;
            this.extensionName      = extensionName;
            extensionDbs.Add(extensionName, this);
        }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return defaultDatabase.CreateContainer(name, database);
        }
        
        public override async Task<MsgResponse<SyncResponse>> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            syncRequest.database = extensionName;
            return await defaultDatabase.ExecuteSync(syncRequest, messageContext).ConfigureAwait(false);
        }
    }
}