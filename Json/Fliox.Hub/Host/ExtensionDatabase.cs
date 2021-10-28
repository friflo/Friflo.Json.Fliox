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
            return null;
        }
    }
}