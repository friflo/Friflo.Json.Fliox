// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Mapper.MapIL.Obj;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Mapper.Map
{
    // This class contains IL specific state/data which is used by JsonReader & JsonWriter. So its not thread safe.
    partial struct Reader
    {
#if !UNITY_5_3_OR_NEWER
        // internal readonly   bool                 useIL;

        internal void InitMirrorStack() {
            classLevel = 0;
        }

        internal void DisposeMirrorStack() {
            for (int n = 0; n < mirrorStack.Count; n++)
                mirrorStack[n].Dispose();
        }

        internal void ClearMirrorStack() {
            for (int n = 0; n < mirrorStack.Count; n++)
                mirrorStack[n].ClearObjectReferences();
        }

        /// <summary> Load the fields of a class instance into the <see cref="ClassMirror"/> arrays. </summary>
        internal ClassMirror InstanceLoad<T>(TypeMapper baseType, ref TypeMapper classType, ref T obj) {
            if (classLevel >= mirrorStack.Count)
                mirrorStack.Add(new ClassMirror());
            var mirror = mirrorStack[classLevel++];
            mirror.LoadInstance(typeCache, baseType, ref classType, ref obj);
            return mirror;
        }

        /// <summary>
        /// Store the "instances fields" represented by the <see cref="ClassMirror"/> arrays to the fields
        /// of a given class instance.
        /// </summary>
        internal void InstanceStore<T>(ClassMirror mirror, ref T obj) {
            mirror.StoreInstance(ref obj);
            --classLevel;
        }
#else 
        internal void InitMirrorStack() { }
        internal void DisposeMirrorStack() { }
        internal void ClearMirrorStack() { }
#endif
    }
}