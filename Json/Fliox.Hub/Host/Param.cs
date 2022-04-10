// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Host
{
    public readonly struct Param<TParam>
    {
        public                      JsonValue       JsonParam       => param;
        
        public    override          string          ToString() => param.AsString();
        
        [DebuggerBrowsable(Never)]
        private   readonly          JsonValue       param;
        [DebuggerBrowsable(Never)]
        private   readonly          ExecuteContext  executeContext;


        internal Param(in JsonValue param, ExecuteContext  executeContext) {
            this.param          = param;
            this.executeContext = executeContext;
        }
        
        /*  public TParam Param { get {
            using (var pooled = executeContext.pool.ObjectMapper.Get()) {
                var reader = pooled.instance.reader;
                return reader.Read<TParam>(param);
            }
        }} */

        /// <summary>Return the command <paramref name="param"/></summary> without validation 
        /// <param name="param">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public bool Get(out TParam param, out string error) {
            return Get<TParam>(out param, out error);
        }
        
        /// <summary>Return the command <paramref name="param"/> as the given type <typeparamref name="T"/> without validation</summary>
        /// <param name="param">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public bool Get<T>(out T param, out string error) {
            using (var pooled = executeContext.ObjectMapper.Get()) {
                var reader  = pooled.instance.reader;
                param       = reader.Read<T>(this.param);
                if (reader.Error.ErrSet) {
                    error   = reader.Error.msg.ToString();
                    return false;
                }
                error = null;
                return true;
            }
        }

        /// <summary>Return the validated command <paramref name="param"/></summary>
        /// <param name="param">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public bool GetValidate(out TParam param, out string error) {
            return GetValidate<TParam>(out param, out error);
        }
        
        /// <summary>Return the validated command <paramref name="param"/> as the given type <typeparamref name="T"/></summary>
        /// <param name="param">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public bool GetValidate<T>(out T param, out string error) {
            if (!Validate<T>(out error)) {
                param = default;
                return false;
            }
            return Get(out param, out error);
        }
        
        public bool Validate(out string error) {
            return Validate<TParam>(out error);
        }
        
        public bool Validate<T>(out string error) {
            var paramValidation = executeContext.sharedCache.GetValidationType(typeof(T));
            using (var pooled = executeContext.pool.TypeValidator.Get()) {
                var validator   = pooled.instance;
                if (!validator.ValidateField(this.param, paramValidation, out error)) {
                    return false;
                }
            }
            return true;
        }
    }
}