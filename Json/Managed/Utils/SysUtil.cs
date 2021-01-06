// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Managed.Utils
{
    public class SysUtil
    {
        public static String ToString <K,V>(IDictionary<K,V> dict)
        {
            StringBuilder itemString = new StringBuilder(); 
            bool first = true;
            foreach(KeyValuePair<K,V> item in dict )
            {
                if (first)
                    first = false;
                else
                    itemString. Append (", ");
                itemString. Append (item. Key);
                itemString. Append ('=');
                itemString. Append (item. Value);
            }
            return itemString. ToString();  
        }
    }
}
