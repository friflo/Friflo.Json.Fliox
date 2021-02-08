// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Linq.Expressions;
using System.Reflection;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map
{
    public abstract class CollectionMapper<TVal, TElm> : TypeMapper<TVal>
    {
        // ReSharper disable once UnassignedReadonlyField
        // field ist set via reflection below to enable using a readonly field
        public   readonly   TypeMapper<TElm>    elementType;
        private  readonly   Type                elementTypeNative;
        private  readonly   ConstructorInfo     constructor;
        private  readonly   Func<int, TElm[]>   createArray;
        
        // ReSharper disable NotAccessedField.Local
        private  readonly   int                 rank;
        private  readonly   Type                keyType;

        internal CollectionMapper (
            Type                type,
            Type                elementType,
            int                 rank,
            Type                keyType,
            ConstructorInfo     constructor) : base (type, true, false)
        {
            this.keyType        = keyType;
            elementTypeNative   = elementType;
            if (elementType == null)
                throw new NullReferenceException("elementType is required");
            this.rank           = rank;
            // constructor can be null. E.g. All array types have none.
            this.constructor    = constructor;
            
            var lambda = CreateArrayExpression();
            createArray = lambda.Compile();
        }
        
        public override void InitTypeMapper(TypeStore typeStore) {
            FieldInfo fieldInfo = GetType().GetField(nameof(elementType));
            TypeMapper mapper = typeStore.GetTypeMapper(elementTypeNative);
            // ReSharper disable once PossibleNullReferenceException
            fieldInfo.SetValue(this, mapper);
        }
        
        public override Object CreateInstance ()
        {
            return ReflectUtils.CreateInstance(constructor);
        }

        private static Expression<Func<int, TElm[]>> CreateArrayExpression () {
            var sizeParam       = Expression.Parameter(typeof(int),  "size");
            // var bounds       = new List<Expression>();
            // bounds.Add(sizeParam);
            Expression create = Expression.NewArrayBounds(typeof(TElm), sizeParam);
            return Expression.Lambda<Func<int, TElm[]>> (create, sizeParam);
        }

        protected TElm[] CopyArray(TElm[] src, int length) {
            // TElm[] dst = new TElm[length];
            TElm[] dst = createArray(length);
            if (src != null) {
                int min = Math.Min(length, src.Length);
                Array.Copy(src, dst, min);
            }
            return dst;
        }

    }
}
