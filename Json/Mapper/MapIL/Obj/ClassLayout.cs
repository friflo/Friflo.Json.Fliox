// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.MapIL.Utils;

namespace Friflo.Json.Flow.Mapper.MapIL.Obj
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
    
    
    public delegate void LoadObject<in T1, in T2, T3>(T1 arg1, T2 arg2, ref T3 arg3);
    public delegate void StoreObject<T1, in T2, in T3>(ref T1 arg1, T2 arg2, T3 arg3);

    public class ClassLayout<T> : ClassLayout
    {
        private readonly LoadObject<long?[], object[], T> loadObjectToMirror;
        private readonly StoreObject<T, long?[], object[]> storeMirrorToObject;
        
        internal ClassLayout(TypeMapper mapper, StoreConfig config) : base(mapper) {
            loadObjectToMirror = null;
            storeMirrorToObject = null;

            // create load/store instance expression
            LoadObject<long?[], object[], T> load  = null;
            StoreObject<T, long?[], object[]> store = null;
            if (config.useIL) {
                var loadLambda = ILCodeGen.LoadInstanceExpression<T>(mapper);
                load = loadLambda.Compile();

                var storeLambda = ILCodeGen.StoreInstanceExpression<T>(mapper);
                store = storeLambda.Compile();
            }

            loadObjectToMirror  = load;
            storeMirrorToObject = store;
        }

        internal void LoadObjectToMirror(long?[] dstPrim, object[] dstObj, ref T src) {
            loadObjectToMirror(dstPrim, dstObj, ref src);
        }

        internal void StoreMirrorToPayload(ref T dst, long?[] srcPrim, object[] srcObj) {
            storeMirrorToObject(ref dst, srcPrim, srcObj);
        }

        internal override void LoadObjectToMirror(long?[] dstPrim, object[] dstObj, object src) {
            T srcVal = (T)src;
            loadObjectToMirror(dstPrim, dstObj, ref srcVal);
        }

        internal override void StoreMirrorToPayload(object dst, long?[] srcPrim, object[] srcObj) {
            T dstVal = (T)dst;
            storeMirrorToObject(ref dstVal, srcPrim, srcObj);
        }
    }
}

#endif
