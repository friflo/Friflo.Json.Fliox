// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

#if JSON_BURST
    using Str128 = Unity.Collections.FixedString128;
#else
    using Str128 = System.String;
    // ReSharper disable InconsistentNaming
#endif


namespace Friflo.Json.Burst
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    // Intended to be passed as ref parameter to be able notify a possible error 
    public struct JsonError : IDisposable
    {
        public  bool            ErrSet  { get; private set; }
        public  Bytes           msg;
        public  int             Pos     { get; private set; }
        
#if !JSON_BURST
        public  IErrorHandler   errorHandler;
#endif
        public void InitJsonError(int capacity) {
            msg.InitBytes(capacity);
        }

        public void Dispose() {
            msg.Dispose();
        }

        public bool Error (int pos) {
            ErrSet = true;
            Pos = pos;
#if !JSON_BURST
            if (errorHandler != null)
                errorHandler.HandleException();
#endif
            return false;
        }

        public void Clear() {
            ErrSet = false;
            msg.Clear();
        }
    
        public override String ToString () {
            return msg.ToString();
        }   
    }

    public interface IErrorHandler {
        void HandleException();
    }

}