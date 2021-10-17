// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Protocol;

namespace Friflo.Json.Fliox.DB.Host
{
    internal class ExtensionDatabase : EntityDatabase
    {
        internal ExtensionDatabase (EntityDatabase extensionBase, string extensionName, DbOpt opt)
            : base (extensionBase, extensionName, opt) { }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return extensionBase.CreateContainer(name, database);
        }
        
        public override async Task<MsgResponse<SyncResponse>> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            return await extensionBase.ExecuteSync(syncRequest, messageContext).ConfigureAwait(false);
        }
    }
}