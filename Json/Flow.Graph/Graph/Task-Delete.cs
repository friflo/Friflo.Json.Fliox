// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Flow.Graph
{
    // ----------------------------------------- DeleteTask -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class DeleteTask<T> where T : Entity
    {
        private readonly    string      id;

        private             string      Label       => $"DeleteTask<{typeof(T).Name}> id: {id}";
        public   override   string      ToString()  => Label;
        
        internal DeleteTask(string id) {
            this.id = id;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class DeleteRangeTask<T> where T : Entity
    {
        private readonly    ICollection<string> ids;

        private             string      Label       => $"DeleteRangeTask<{typeof(T).Name}> #ids: {ids.Count}";
        public   override   string      ToString()  => Label;
        
        internal DeleteRangeTask(ICollection<string> ids) {
            this.ids = ids;
        }
    }
}