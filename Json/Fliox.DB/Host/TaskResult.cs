// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

/* not used
using Friflo.Json.Fliox.DB.Protocol;

namespace Friflo.Json.Fliox.DB.Host
{
    public struct TaskResult<TResult> where TResult : SyncTaskResult 
    {
        public readonly TResult         success;
        public readonly CommandError    error;
        
        public TaskResult(TResult result) {
            success = result;
            error   = null;
        }
        
        public TaskResult(string error) {
            this.error  = new CommandError (error);
            success     = null;
        }
        
        public  SyncTaskResult Result { get {
            if (success != null)
                return success;
            return error;
        } }

        public override string ToString() => success != null ? success.ToString() : error.ToString();
        
    }
}
*/