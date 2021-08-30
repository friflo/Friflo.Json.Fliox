// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Fliox.Schema.Utils
{
    // [Topological Sorting in C# - CodeProject] https://www.codeproject.com/articles/869059/topological-sorting-in-csharp
    public static class TopologicalSort
    {
        public static List<T> Sort<T>(IEnumerable<T> source, Func<T, IEnumerable<T>> getDependencies)
        {
            var sorted  = new List<T>();
            var visited = new Dictionary<T, bool>();
            foreach (var item in source) {
                Visit(item, getDependencies, sorted, visited);
            }
            return sorted;
        }

        private static void Visit<T>(T item, Func<T, IEnumerable<T>> getDependencies, List<T> sorted, Dictionary<T, bool> visited)
        {
            var alreadyVisited = visited.TryGetValue(item, out var inProcess);
            if (alreadyVisited) {
                if (inProcess) {
                    throw new ArgumentException("Cyclic dependency found.");
                }
            } else {
                visited[item] = true;
                var dependencies = getDependencies(item);
                if (dependencies != null) {
                    foreach (var dependency in dependencies) {
                        Visit(dependency, getDependencies, sorted, visited);
                    }
                }
                visited[item] = false;
                sorted.Add(item);
            }
        }
    }
}