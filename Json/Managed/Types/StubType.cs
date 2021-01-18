using System;
using Friflo.Json.Managed.Map;

namespace Friflo.Json.Managed.Types
{
    public enum TypeCat
    {
        None,
        String,
        Number,
        Bool,
        Object,
        Array
    }
    
    public abstract class StubType : IDisposable {
        public  readonly    Type        type;
        public  readonly    IJsonMapper map;
        public  readonly    bool        isNullable;
        public  readonly    TypeCat     typeCat;

        /// <summary>
        /// Need to be overriden, in case the derived <see cref="StubType"/> uses <see cref="System.Type"/>'s
        /// which are required in a <see cref="IJsonMapper"/> implementation returning a <see cref="StubType"/>.<br/>
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

        public StubType(Type type, IJsonMapper map, bool isNullable, TypeCat typeCat) {
            this.type =         type;
            this.map =        map;
            this.isNullable =   isNullable;
            this.typeCat =     typeCat;
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