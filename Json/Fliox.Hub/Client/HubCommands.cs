// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Client
{
    // currently concept validation only
    public class HubCommands
    {
        private readonly FlioxClient client;
        
        protected HubCommands (FlioxClient client) {
            this.client = client;
        }
        
        protected CommandTask<TResult> SendCommand<TParam, TResult>(string name, TParam param) {
            return client.SendCommand<TParam, TResult>(name, param);
        }
    }
}