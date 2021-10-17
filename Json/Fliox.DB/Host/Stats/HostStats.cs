// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.DB.Host.Stats
{
    public class HostStats
    {
        public readonly RequestHistories requestHistories = new RequestHistories();

        public void Update() {
            requestHistories.Update();
        }
    }
}