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
    public struct ValueError : IDisposable
    {
        public	Bytes	err;
        private	bool	errSet;

        public void InitValueError() {
            err.InitBytes(128);
        }

        public void Dispose() {
            err.Dispose();
        }

        public bool IsErrSet()
        {
            return errSet;
        }
		
        public void	ClearError ()
        {
            err.Clear();
            errSet = false;
        }
		
        public bool SetErrorFalse (Str128 error, ref Bytes value)
        {
            errSet = true;
            if (err.buffer.IsCreated()) {
                err.Clear();
                err.AppendStr128(ref error);
                err.AppendBytes(ref value);
            }
            return false;
        }
        
        public override String ToString () {
            return err.ToString();
        }	
    }
}