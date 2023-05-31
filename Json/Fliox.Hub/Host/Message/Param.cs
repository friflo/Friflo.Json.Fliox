// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    public readonly struct Param<TParam>
    {
        public                      JsonValue   RawValue    => rawValue;
        
        public    override          string      ToString()  => rawValue.AsString();
        
        [DebuggerBrowsable(Never)]
        private   readonly          JsonValue   rawValue;
        [DebuggerBrowsable(Never)]
        private   readonly          SyncContext syncContext;


        internal Param(in JsonValue value, SyncContext  syncContext) {
            this.rawValue       = value;
            this.syncContext    = syncContext;
        }
        
        public TParam Value { get {
            using (var pooled = syncContext.pool.ObjectMapper.Get()) {
                var reader = pooled.instance.reader;
                return reader.Read<TParam>(rawValue);
            }
        }}

        /// <summary>Return the command <paramref name="value"/></summary> without validation 
        /// <param name="value">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public bool Get(out TParam value, out string error) {
            return Get<TParam>(out value, out error);
        }
        
        /// <summary>Return the command <paramref name="value"/> as the given type <typeparamref name="T"/> without validation</summary>
        /// <param name="value">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public bool Get<T>(out T value, out string error) {
            using (var pooled = syncContext.ObjectMapper.Get()) {
                var reader  = pooled.instance.reader;
                value       = reader.Read<T>(this.rawValue);
                if (reader.Error.ErrSet) {
                    error   = reader.Error.msg.ToString();
                    return false;
                }
                error = null;
                return true;
            }
        }

        /// <summary>Return the validated command <paramref name="value"/></summary>
        /// <param name="value">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public bool GetValidate(out TParam value, out string error) {
            return GetValidate<TParam>(out value, out error);
        }
        
        /// <summary>Return the validated command <paramref name="value"/> as the given type <typeparamref name="T"/></summary>
        /// <param name="value">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public bool GetValidate<T>(out T value, out string error) {
            if (!Validate<T>(out error)) {
                value = default;
                return false;
            }
            return Get(out value, out error);
        }
        
        public bool Validate(out string error) {
            return Validate<TParam>(out error);
        }
        
        public bool Validate<T>(out string error) {
            var paramValidation = syncContext.sharedCache.GetValidationType(typeof(T));
            using (var pooled = syncContext.pool.TypeValidator.Get()) {
                var validator   = pooled.instance;
                if (!validator.Validate(this.rawValue, paramValidation, out error)) {
                    return false;
                }
            }
            return true;
        }
    }
}