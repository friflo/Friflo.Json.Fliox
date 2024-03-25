// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    public readonly struct Param<TParam>
    {
        /// <summary>
        /// Returns the <see cref="Param{TParam}"/> <see cref="RawValue"/> of a message / command without validation.<br/>
        /// </summary>
                        public              JsonValue   RawValue    => rawValue;
        /// <summary>
        /// Returns the <see cref="Param{TParam}"/> <see cref="Value"/> of a message / command without validation.<br/>
        /// Throws an exception in case deserialization fails.
        /// </summary>
                        public              TParam      Value       => GetValue();
                        public  override    string      ToString()  => rawValue.AsString();
                        
        // --- internal / private fields
        [Browse(Never)] private readonly    JsonValue   rawValue;
        [Browse(Never)] private readonly    SyncContext syncContext;

        internal Param(in JsonValue value, SyncContext  syncContext) {
            this.rawValue       = value;
            this.syncContext    = syncContext;
        }
        
        private TParam GetValue() {
            using (var pooled = syncContext.pool.ObjectMapper.Get()) {
                var reader = pooled.instance.reader;
                var value = reader.Read<TParam>(rawValue);
                if (reader.Error.ErrSet) {
                    throw new InvalidOperationException(reader.Error.msg.ToString());
                }
                return value;
            }
        }

        /// <summary>
        /// Return the command / message <see cref="Param{TParam}"/> <paramref name="value"/> without validation
        /// </summary> 
        /// <param name="value">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public bool Get(out TParam value, out string error) {
            return Get<TParam>(out value, out error);
        }
        
        /// <summary>
        /// Return the command / message <see cref="Param{TParam}"/> <paramref name="value"/>
        /// as the given type <typeparamref name="T"/> without validation
        /// </summary>
        /// <param name="value">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public bool Get<T>(out T value, out string error) {
            using (var pooled = syncContext.ObjectMapper.Get()) {
                var reader  = pooled.instance.reader;
                value       = reader.Read<T>(rawValue);
                if (reader.Error.ErrSet) {
                    error   = reader.Error.msg.ToString();
                    return false;
                }
                error = null;
                return true;
            }
        }

        /// <summary>
        /// Return the validated command / message <see cref="Param{TParam}"/> <paramref name="value"/>
        /// </summary>
        /// <param name="value">the param value if conversion successful</param>
        /// <param name="error">contains the error message if conversion failed</param>
        /// <returns> true if successful; false otherwise </returns>
        public bool GetValidate(out TParam value, out string error) {
            return GetValidate<TParam>(out value, out error);
        }
        
        /// <summary>
        /// Return the validated command / message <see cref="Param{TParam}"/> <paramref name="value"/>
        /// as the given type <typeparamref name="T"/>
        /// </summary>
        /// <param name="value">the param value if deserialization is successful</param>
        /// <param name="error">contains the error message if deserialization fails</param>
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
                if (!validator.Validate(rawValue, paramValidation, out error)) {
                    return false;
                }
            }
            return true;
        }
    }
}