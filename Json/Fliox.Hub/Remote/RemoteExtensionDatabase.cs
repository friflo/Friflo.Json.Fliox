// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Fliox.Hub.Remote
{
    public class RemoteExtensionDatabase : EntityDatabase
    {
        public RemoteExtensionDatabase (RemoteClientHub hub, string extensionName, TaskHandler handler = null, DbOpt opt = null)
            : base (hub, extensionName, handler, opt)
        { }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return null;
        }
    }
}