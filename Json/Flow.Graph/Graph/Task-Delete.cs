// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Flow.Graph
{
    // ----------------------------------------- DeleteTask -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class DeleteTask
    {
        private readonly    string      id;

        public   override   string      ToString()  => id;
        
        internal DeleteTask(string id) {
            this.id = id;
        }
    }
}