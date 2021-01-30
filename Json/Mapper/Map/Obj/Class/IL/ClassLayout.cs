// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Mapper.Map.Obj.Class.Reflect;

#if !UNITY_5_3_OR_NEWER

namespace Friflo.Json.Mapper.Map.Obj.Class.IL
{
    public abstract class ClassLayout {

        internal readonly int   primCount;
        internal readonly int   objCount;
        
        internal ClassLayout(PropertyFields  propFields) {
            primCount       = propFields.primCount;
            objCount        = propFields.objCount;
        }
    }

    public class ClassLayout<T> : ClassLayout
    {
        private Action<long[], object[], T> loadObjectToPayload;
        private Action<T, long[], object[]> storePayloadToObject;
        
        internal ClassLayout(PropertyFields propFields) : base(propFields) {
            loadObjectToPayload = null;
            storePayloadToObject = null;
        }

        internal void InitClassLayout(PropertyFields propFields, ResolverConfig config) {

            // create load/store instance expression
            Action<long[], object[], T> load = null;
            Action<T, long[], object[]> store = null;
            if (config.useIL) {
                var loadLambda = ILCodeGen.LoadInstanceExpression<T>(propFields);
                load = loadLambda.Compile();

                var storeLambda = ILCodeGen.StoreInstanceExpression<T>(propFields);
                store = storeLambda.Compile();
            }

            loadObjectToPayload = load;
            storePayloadToObject = store;
        }

        internal void LoadObjectToMirror(long[] dstPrim, object[] dstObj, T src) {
            loadObjectToPayload(dstPrim, dstObj, (T) src);
        }

        internal void StoreMirrorToPayload(T dst, long[] srcPrim, object[] srcObj) {
            storePayloadToObject((T) dst, srcPrim, srcObj);
        }
    }
}

#endif
