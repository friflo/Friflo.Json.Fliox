// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Types
{
    public interface IProperties
    {
        // void     SetProperties (Property prop) ; 
    }
    public abstract class  Property
    {
        private static readonly     Type[] Types = new Type [] { typeof( Property ) };

        public abstract void    Set(String name) ;
        public abstract void    Set(String name, String field) ;

        public static MethodInfo GetPropertiesDeclaration (Type type)
        {
            return Reflect.GetMethodEx(type, "SetProperties", Types);
        }

        internal void SetProperties (Type type)
        {
            try
            {
                MethodInfo method = GetPropertiesDeclaration(type);
                if (method != null)
                {
                    Object[] args = new Object[] { this };
                    Reflect.Invoke (method, null, args);
                }
                else
                {
                    PropertyInfo[] properties = Reflect.GetProperties(type);
                    for (int n = 0; n < properties. Length; n++)
                        Set(properties[n]. Name);

                    FieldInfo[] field = Reflect.GetFields(type);
                    for (int n = 0; n < field. Length; n++)
                        Set(field[n]. Name);
                }
            }
            catch (Exception e)
            {
                throw new FrifloException("SetProperties() failed for type: " + type, e);
            }
        }
    }
}
