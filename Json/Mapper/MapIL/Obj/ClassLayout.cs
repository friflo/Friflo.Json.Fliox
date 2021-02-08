// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.MapIL.Utils;

namespace Friflo.Json.Mapper.MapIL.Obj
{
    public abstract class ClassLayout {

        internal readonly int   primCount;
        internal readonly int   objCount;
        
        internal ClassLayout(TypeMapper mapper) {
            primCount       = mapper.propFields.primCount;
            objCount        = mapper.propFields.objCount;
        }

        internal abstract void LoadObjectToMirror  (long?[] dstPrim, object[] dstObj, object src);
        internal abstract void StoreMirrorToPayload(object dst, long?[] srcPrim, object[] srcObj);
    }

    public class ClassLayout<T> : ClassLayout
    {
        private readonly Action<long?[], object[], T> loadObjectToMirror;
        private readonly Action<T, long?[], object[]> storeMirrorToObject;
        
        internal ClassLayout(TypeMapper mapper, StoreConfig config) : base(mapper) {
            loadObjectToMirror = null;
            storeMirrorToObject = null;

            // create load/store instance expression
            Action<long?[], object[], T> load  = null;
            Action<T, long?[], object[]> store = null;
            if (config.useIL) {
                var loadLambda = ILCodeGen.LoadInstanceExpression<T>(mapper);
                load = loadLambda.Compile();

                var storeLambda = ILCodeGen.StoreInstanceExpression<T>(mapper);
                store = storeLambda.Compile();
            }

            loadObjectToMirror  = load;
            storeMirrorToObject = store;
        }

        internal void LoadObjectToMirror(long?[] dstPrim, object[] dstObj, T src) {
            loadObjectToMirror(dstPrim, dstObj, src);
        }

        internal void StoreMirrorToPayload(T dst, long?[] srcPrim, object[] srcObj) {
            storeMirrorToObject(dst, srcPrim, srcObj);
        }

        internal override void LoadObjectToMirror(long?[] dstPrim, object[] dstObj, object src) {
            loadObjectToMirror(dstPrim, dstObj, (T)src);
        }

        internal override void StoreMirrorToPayload(object dst, long?[] srcPrim, object[] srcObj) {
            storeMirrorToObject((T)dst, srcPrim, srcObj);
        }
    }
}

#endif
