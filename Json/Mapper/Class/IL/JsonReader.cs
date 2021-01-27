// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Mapper.Class.IL;
using Friflo.Json.Mapper.Map;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Mapper
{
    // This class contains IL specific state/data which is used by JsonReader & JsonWriter. So its not thread safe.
    partial class JsonReader
    {
        private  readonly   List<ClassPayload>      handlerStack = new List<ClassPayload>(16);
        private             int                     classLevel;
        internal readonly   bool                    useIL;

        internal ClassPayload BeginPayload(TypeMapper classType) {
            if (classLevel >= handlerStack.Count)
                handlerStack.Add(new ClassPayload());
            var handler = handlerStack[classLevel++];
            handler.data.Resize(classType.layout.size);
            return handler;
        }

        void DisposeClassPayloads() {
            for (int n = 0; n < handlerStack.Count; n++)
                handlerStack[n].Dispose();
        }

        /*
        internal void DropPayload() {
            --classLevel;
        } */
        
        internal void ApplyPayload(object obj) {
            --classLevel;
        }
    }
}