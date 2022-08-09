// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Fliox.Hub.Utils
{
    internal readonly struct Result <T>
    {
        internal  readonly  T       value;
        internal  readonly  string  error;
        
        internal            bool    Success => error == null;      
        
        private Result (T value) {
            this.value    = value;
            this.error      = null;
        }
        
        private Result (string error) {
            this.value  = default;
            this.error  = error;
        }
        
        public static implicit operator Result<T>(T      value) => new Result<T>(value);
        public static implicit operator Result<T>(string error) => new Result<T>(error);
    }
}