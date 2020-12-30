// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Burst.Utils
{
    // Intended to be passed as ref parameter to be able notify a possible error 
    public struct ErrorCx
    {
        public         	bool		throwException; // has only effect in managed code
        public	        bool	    ErrSet { get; private set; }
        public			String128	Msg  { get; private set; }
        public			int			Pos { get; private set; }

        public bool Error (String128 msg, int pos) {
            this.ErrSet = true;
            this.Msg = msg;
            this.Pos = pos;
#if !JSON_BURST
            if (throwException)
                throw new Friflo.Json.Managed.Utils.FrifloException (msg.ToString());
#endif
            return false;
        }

        public void Clear() {
            ErrSet = false;
        }
	
        public override String ToString () {
            return Msg.ToString();
        }	
    }
}