// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Obj.Reflect;
using Friflo.Json.Mapper.MapIL.Utils;

namespace Friflo.Json.Mapper.MapIL.Obj
{
    public abstract class ClassLayout {

        internal readonly int   primCount;
        internal readonly int   objCount;
        
        internal ClassLayout(TypeMapper mapper) {
            PropertyFields propFields = mapper.GetPropFields();
            primCount       = propFields.primCount;
            objCount        = propFields.objCount;
        }
    }

    public class ClassLayout<T> : ClassLayout
    {
        private readonly Action<long?[], object[], T> loadObjectToPayload;
        private readonly Action<T, long?[], object[]> storePayloadToObject;
        
        internal ClassLayout(TypeMapper mapper, StoreConfig config) : base(mapper) {
            loadObjectToPayload = null;
            storePayloadToObject = null;

            // create load/store instance expression
            Action<long?[], object[], T> load = null;
            Action<T, long?[], object[]> store = null;
            if (config.useIL) {
                var loadLambda = ILCodeGen.LoadInstanceExpression<T>(mapper);
                load = loadLambda.Compile();

                var storeLambda = ILCodeGen.StoreInstanceExpression<T>(mapper);
                store = storeLambda.Compile();
            }

            loadObjectToPayload = load;
            storePayloadToObject = store;
        }

        internal void LoadObjectToMirror(long?[] dstPrim, object[] dstObj, T src) {
            loadObjectToPayload(dstPrim, dstObj, src);
        }

        internal void StoreMirrorToPayload(T dst, long?[] srcPrim, object[] srcObj) {
            storePayloadToObject(dst, srcPrim, srcObj);
        }
    }
}

#endif
