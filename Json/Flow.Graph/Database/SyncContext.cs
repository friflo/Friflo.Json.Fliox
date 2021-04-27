// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Database.Models;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Database
{
    // ------------------------------------ SyncContext ------------------------------------
    /// <summary>
    /// One <see cref="SyncContext"/> is created per <see cref="EntityContainer"/> to enable multi threaded
    /// request handling for different <see cref="EntityContainer"/> instances.
    ///
    /// The <see cref="EntityContainer.SyncContext"/> for a specific <see cref="EntityContainer"/> must not be used
    /// multi threaded.
    ///
    /// E.g. Reading key/values of a database can be executed multi threaded, but serializing for them
    /// for a <see cref="SyncResponse"/> in <see cref="DatabaseTask.Execute"/> need to be single threaded. 
    /// </summary>
    public class SyncContext : IDisposable
    {
        public readonly  JsonPatcher        jsonPatcher     = new JsonPatcher();
        public readonly  ScalarSelector     scalarSelector  = new ScalarSelector();
        public readonly  JsonEvaluator      jsonEvaluator   = new JsonEvaluator();

        public void Dispose() {
            jsonEvaluator.Dispose();
            scalarSelector.Dispose();
            jsonPatcher.Dispose();
        }
    }
}