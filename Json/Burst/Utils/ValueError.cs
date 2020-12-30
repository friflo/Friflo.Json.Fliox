using System;

#if JSON_BURST
	using Str128 = Unity.Collections.FixedString128;
#else
    using Str128 = System.String;
#endif

namespace Friflo.Json.Burst.Utils
{
    // Intended to be passed as ref parameter to be able notify a possible error 
    public struct ValueError
    {
        private	String128	err;
        private	bool	    errSet;
		
        public bool IsErrSet()
        {
            return errSet;
        }
		
        public String128 GetError()
        {
            return err;
        }
		
        public void	ClearError ()
        {
            errSet = false;
        }
		
        public bool SetErrorFalse (Str128 error)
        {
            this.err = new String128(error);
            errSet = true;
            return false;
        }
        
        public override String ToString () {
            return err.ToString();
        }	
    }
}