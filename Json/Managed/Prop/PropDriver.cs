// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Managed.Codecs;

namespace Friflo.Json.Managed.Prop
{
    public class PropDriver
    {
                        static  IPropDriver platform  = null;
    
        private class DefaultDriver : IPropDriver
        {
            public PropField CreateVariable(TypeResolver resolver,PropType declType, String name, FieldInfo field)
            {
                return new PropFieldVariable(resolver, declType, name, field);
            }       
        }   
    
        public static void SetDriver(IPropDriver driver)
        {
            platform = driver;
        }
    
        public static IPropDriver GetDriver()
        {
            if (platform != null)
                return platform;








            platform = new DefaultDriver();

            return platform;
        }
    }
}
