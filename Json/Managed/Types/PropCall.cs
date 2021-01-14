// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Managed.Types
{
    public class PropCall
    {
        public String           msg;
        
        public virtual bool Error (String msg)
        {
            this.msg = msg;
            return false;
        }
    
        class LogCall : PropCall
        {
            public override bool Error (String msg)
            {
                // FFLog.log("PropCall.log - " + msg);
                return false;
            }       
        }
        
        public static readonly PropCall log = new LogCall();
    }
}
