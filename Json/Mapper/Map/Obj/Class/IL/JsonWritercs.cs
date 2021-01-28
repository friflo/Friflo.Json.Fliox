// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Obj.Class.IL;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Mapper
{
    // This class contains IL specific state/data which is used by JsonReader & JsonWriter. So its not thread safe.
    partial class JsonWriter
    {
        private  readonly   List<ClassPayload>      handlerStack = new List<ClassPayload>(16);
        private             int                     classLevel;
        internal readonly   bool                    useIL;

        private void DisposePayloads() {
            for (int n = 0; n < handlerStack.Count; n++)
                handlerStack[n].Dispose();
        }
        
        /// <summary> Load the fields of a class instance into the <see cref="ClassPayload.data"/> array. </summary>
        internal static ClassPayload InstanceLoad(JsonWriter writer, TypeMapper classType, object obj) {
            if (writer.classLevel >= writer.handlerStack.Count)
                writer.handlerStack.Add(new ClassPayload(Default.Constructor));
            ClassPayload payload = writer.handlerStack[writer.classLevel++];
            payload.LoadInstance(classType);
            return payload;
        }

        internal static void InstancePop(JsonWriter writer) {
            --writer.classLevel;
        }
    }
}