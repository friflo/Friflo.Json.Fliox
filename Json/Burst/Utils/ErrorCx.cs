// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

#if JSON_BURST
    using Str128 = Unity.Collections.FixedString128;
#else
	using Str128 = System.String;
#endif


namespace Friflo.Json.Burst.Utils
{
    // Intended to be passed as ref parameter to be able notify a possible error 
    public struct ErrorCx : IDisposable
    {
        public         	bool		throwException; // has only effect in managed code
        public	        bool	    ErrSet  { get; private set; }
        public          Bytes       Msg; //     { get; private set; }
        public			int			Pos     { get; private set; }

        public void InitErrorCx(int capacity) {
            Msg.InitBytes(capacity);
        }

        public void Dispose() {
            Msg.Dispose();
        }

        public bool Error (int pos) {
            ErrSet = true;
            Pos = pos;
#if !JSON_BURST
            if (throwException)
                throw new Friflo.Json.Managed.Utils.FrifloException (Msg.ToString());
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