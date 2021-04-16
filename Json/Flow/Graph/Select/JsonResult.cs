// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Friflo.Json.Flow.Graph.Select
{
    public class JsonResult
    {
        public   readonly   List<string>    values          = new List<string>();


        internal void Init() {
            values.Clear();
        }

        internal static void Add(string scalar, List<PathSelector<JsonResult>> selectors) {
            foreach (var selector in selectors) {
                var result = selector.result;
                result.values.Add(scalar);
            }
        }
    }
}