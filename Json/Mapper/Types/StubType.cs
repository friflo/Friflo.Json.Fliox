// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.Mapper.Types
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class StubType : IDisposable {
        public  readonly    Type        type;
        public  readonly    ITypeMapper map;
        public  readonly    bool        isNullable;
        /// <summary>Use <see cref="JsonEvent.ValueNull"/>, if value can be either a string, number or a bool</summary>
        public  readonly    JsonEvent  expectedEvent;
        public  readonly    VarType     varType;

        /// <summary>
        /// Need to be overriden, in case the derived <see cref="StubType"/> uses <see cref="System.Type"/>'s
        /// which are required in a <see cref="ITypeMapper"/> implementation returning a <see cref="StubType"/>.<br/>
        /// 
        /// In this case <see cref="InitStubType"/> is used to map a <see cref="System.Type"/> to a required
        /// <see cref="StubType"/> by calling <see cref="TypeStore.GetType(System.Type)"/> and storing the returned
        /// reference also in the created <see cref="StubType"/> instance.<br/>
        ///
        /// This enables deferred initialization of StubType references by their related Type to support circular type dependencies.
        /// The goal is to support also type hierarchies without a 'directed acyclic graph' (DAG) of type dependencies.
        /// </summary>
        public abstract void InitStubType(TypeStore typeStore);
        
        public virtual Object CreateInstance() {
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="map"></param>
        /// <param name="isNullable"></param>
        /// <param name="expectedEvent">Use <see cref="JsonEvent.ValueNull"/>, if value can be either a string, number or a bool</param>
        public StubType(Type type, ITypeMapper map, bool isNullable, JsonEvent expectedEvent) {
            this.type =             type;
            this.map =              map;
            this.isNullable =       isNullable;
            this.expectedEvent =    expectedEvent;
            this.varType =          Var.GetVarType(type);
        }

        public virtual void Dispose() {
        }
        
        public static bool IsStandardType(Type type) {
            return type.IsPrimitive || type == typeof(string) || type.IsArray;
        }
        
        public static bool IsGenericType(Type type) {
            while (type != null) {
                if (type.IsConstructedGenericType)
                    return true;
                type = type.BaseType;
            }
            return false;
        } 
    }
}