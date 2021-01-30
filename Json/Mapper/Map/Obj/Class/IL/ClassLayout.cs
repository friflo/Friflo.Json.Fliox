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

        public abstract void LoadObjectToMirror  (long[] dstPrim, object[] dstObj, object src);
        public abstract void StoreMirrorToPayload(object dst, long[] srcPrim, object[] srcObj);
    }

    public class ClassLayout<T> : ClassLayout
    {
        internal ClassLayout(PropertyFields propFields) : base(propFields) {
            loadObjectToPayload = null;
            storePayloadToObject = null;
        }

        internal void InitClassLayout(Type type, PropertyFields propFields, ResolverConfig config) {

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

        public override void LoadObjectToMirror(long[] dstPrim, object[] dstObj, object src) {
            loadObjectToPayload(dstPrim, dstObj, (T) src);
        }

        public override void StoreMirrorToPayload(object dst, long[] srcPrim, object[] srcObj) {
            storePayloadToObject((T) dst, srcPrim, srcObj);
        }

        internal Action<long[], object[], T> loadObjectToPayload;
        internal Action<T, long[], object[]> storePayloadToObject;

    
        // Unity dummies
        // internal        ClassLayout(PropertyFields propFields) : base(propFields) { }
        // internal void   InitClassLayout (Type type, PropertyFields propFields, ResolverConfig config) { }
    }
}

#endif
