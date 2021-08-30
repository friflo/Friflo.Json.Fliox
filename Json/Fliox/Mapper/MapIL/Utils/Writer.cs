// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Fliox.Mapper.MapIL.Obj;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Mapper.Map
{
    // This class contains IL specific state/data which is used by JsonReader & JsonWriter. So its not thread safe.
    partial struct Writer
    {
        // internal readonly   bool                 useIL;
#if !UNITY_5_3_OR_NEWER


        internal void InitMirrorStack() {
            classLevel = 0;
        }
        private void DisposeMirrorStack() {
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

        internal void InstancePop() {
            --classLevel;
        }
#else
        internal    void        InitMirrorStack()       { }
        private     void        DisposeMirrorStack()    { }
        internal    void        ClearMirrorStack()      { }
        internal    ClassMirror InstanceLoad(TypeMapper classType, object obj) { return null; }
        internal    void        InstancePop()           { }
#endif
    }
}