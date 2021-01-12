// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Prop
{
    public abstract class NativeType : IDisposable {
        public  readonly   Type            type;


        public abstract Object CreateInstance();

        protected NativeType(Type type) {
            this.type = type;

        }

        public virtual void Dispose() {
        }
    }
    public class PropCollection : NativeType
    {
        public   readonly   Type            typeInterface;
        public   readonly   Type            keyType;
        public   readonly   Type            elementType;
        public   readonly   int             rank;
        private             NativeType      elementPropType; // is set on first lookup
        public   readonly   SimpleType.Id ? id;
        internal readonly   ConstructorInfo constructor;
    
        internal PropCollection (Type typeInterface, Type nativeType, Type elementType, int rank, Type keyType) :
            base (nativeType)
        {
            this.typeInterface  = typeInterface;
            this.keyType        = keyType;
            this.elementType    = elementType;
            this.rank           = rank;
            this.id             = SimpleType.IdFromType(elementType);
            this.constructor    = GetConstructor (nativeType, typeInterface, keyType, elementType);
        }

        public NativeType GetElementPropType(PropType.Cache typeCache) {
            // simply reduce lookups
            if (elementPropType == null)
                elementPropType = typeCache.GetType(elementType);
            return elementPropType;
        }

        public override Object CreateInstance ()
        {
            return Reflect.CreateInstance(constructor);
        }

        internal static ConstructorInfo GetConstructor (Type type, Type typeInterface, Type keyType, Type elementType)
        {
            ConstructorInfo constructor = Reflect.GetDefaultConstructor(type);
            if (constructor != null)
                return constructor;
            if  (typeInterface == typeof( Array ))
            {
                return null; // For arrays Arrays.CreateInstance(componentType, length) is used
            }
            if  (typeInterface == typeof( IList<> ))
            {
                return Reflect.GetDefaultConstructor( typeof(List<>).MakeGenericType(elementType) );
            }
            if (typeInterface == typeof( IDictionary<,> ))
            {
                return Reflect.GetDefaultConstructor( typeof(Dictionary<,>).MakeGenericType(keyType, elementType) );
            }
            throw new FrifloException ("interface type not supported");
        }

        public  class Info
        {
            internal PropCollection collection;
            internal PropAccess     access;
    
            public  static Info Create (FieldInfo field)
            {
                Info info = new Info();
                info.Create (field. FieldType );
                return info;
            }
    
            public  static Info Create (PropertyInfo getter)
            {
                Info info = new Info();
                info.Create (getter. PropertyType);
                return info;
            }

            public static PropCollection CreateCollection (Type type)
            {
                Info info = new Info();
                info.Create (type );
                return info.collection;
            }
    
            private  void Create (Type type )
            {
                // If retrieving element type gets to buggy, change retrieving element type of collections.
                // In this case element type have to be specified via:
                // void Property.Set(String name, Class<?> entryType)
                if (type. IsArray) {
                    Type elementType = type.GetElementType();
                    collection =    new PropCollection  ( typeof( Array ), type, elementType, type. GetArrayRank(), null);
                    access =        new PropAccess      ( typeof( Array ), type, elementType);
                }
                else
                {
                    Type[] args;
                    args = Reflect.GetGenericInterfaceArgs (type, typeof( IList<>) );
                    if (args != null) {
                        Type elementType = args[0];
                        collection =    new PropCollection  ( typeof( IList<>), type, elementType, 1, null);
                        access =        new PropAccess      ( typeof( IList<>), type, elementType);
                    }
                    args = Reflect.GetGenericInterfaceArgs (type, typeof( IKeySet <>) );
                    if (args != null) {
                        Type elementType = args[0];
                        access = new PropAccess(typeof(IKeySet<>), type, elementType);
                    }

                    args = Reflect.GetGenericInterfaceArgs (type, typeof( IDictionary<,>) );
                    if (args != null)
                    {
                        Type elementType = args[1];
                        collection =    new PropCollection  ( typeof( IDictionary<,> ), type, elementType, 1, args[0]);
                        access =        new PropAccess      ( typeof( IDictionary<,> ), type, elementType);
                    }
                }
            }
        }


    }
}