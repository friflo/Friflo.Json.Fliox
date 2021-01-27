// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Class.Reflect
{
    public interface IProperties
    {
        // void     SetProperties (Property prop) ; 
    }
    public abstract class  Property
    {
        private static readonly     Type[] Types = new Type [] { typeof( Property ) };

        protected abstract void    Set(String name) ;
        public    abstract void    Set(String name, String field) ;

        private static MethodInfo GetPropertiesDeclaration (Type type)
        {
            return ReflectUtils.GetMethodEx(type, "SetProperties", Types);
        }

        internal void SetProperties (Type type)
        {
            try
            {
                MethodInfo method = GetPropertiesDeclaration(type);
                if (method != null)
                {
                    Object[] args = new Object[] { this };
                    ReflectUtils.Invoke (method, null, args);
                }
                else
                {
                    PropertyInfo[] properties = ReflectUtils.GetProperties(type);
                    for (int n = 0; n < properties. Length; n++)
                        Set(properties[n]. Name);

                    FieldInfo[] field = ReflectUtils.GetFields(type);
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
