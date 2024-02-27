// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

// JSON_BURST_TAG
using Str128 = System.String;

namespace Friflo.Json.Burst
{
    [CLSCompliant(true)]
    // Intended to be passed as ref parameter to be able notify a possible error 
    public struct JsonError : IDisposable
    {
        public      bool            ErrSet  { get; private set; }
        public      Bytes           msg;
        public      int             Pos     { get; private set; }
        
        internal    int             msgBodyStart;
        internal    int             msgBodyEnd;
        // JSON_BURST_TAG - was not available with JSON_BURST
        public      IErrorHandler   errorHandler;

        public void InitJsonError(int capacity) {
            msg.InitBytes(capacity);
        }

        public void Dispose() {
            msg.Dispose();
        }

        public bool Error (int pos) {
            ErrSet = true;
            Pos = pos;
            if (errorHandler != null) {
                errorHandler.HandleError(pos, msg);
            }
            return false;
        }

        public void Clear() {
            ErrSet = false;
            msg.Clear();
        }
    
        public override string ToString () {
            return msg.AsString();
        }
        
        /// <summary>Get error message including its position</summary>
        public string GetMessageBody () {
            var body = msg;
            body.start  = msgBodyStart;
            body.end    = msgBodyEnd;
            return body.AsString();
        }
        
        /// <summary>Get error message without its position</summary>
        public string GetMessage () {
            var body = msg;
            body.start  = msgBodyStart;
            return body.AsString();
        }
    }

    public interface IErrorHandler {
        void HandleError(int pos, in Bytes message);
    }

}