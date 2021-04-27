// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Flow.Graph
{
    // ----------------------------------------- DeleteTask -----------------------------------------
    public class DeleteTask
    {
        private readonly    string      id;

        public   override   string      ToString()  => id;
        
        internal DeleteTask(string id) {
            this.id = id;
        }
    }
}