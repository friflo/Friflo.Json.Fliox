// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// <see cref="Result"/> contain utility methods to return errors or values of methods annotated with <see cref="CommandHandlerAttribute"/>.<br/>
    /// E.g the method <see cref="Result.Error(string)"/> to return an error.<br/>
    /// These methods return their result using a <see cref="Result{T}"/> type.
    /// </summary>
    public static class Result
    {
        // --- error results
        public static ResultError       Error(string message)                           => new ResultError(message);
        public static ResultError       Error(TaskErrorType errorType, string message)  => new ResultError(errorType, message);
        public static ResultError       ValidationError(string message)                 => new ResultError(TaskErrorType.ValidationError, message);

        // --- success results
        public static Result<T>         Value<T>(T value)               => new Result<T>(value);
        public static Task<Result<T>>   TaskValue<T>(T value)           => Task.FromResult(new Result<T>(value));
        public static Task<Result<T>>   TaskError<T>(string message)    => Task.FromResult<Result<T>>(new ResultError(message));
    }

    /// <summary>
    /// Used to return either a result or an error of methods annotated with <see cref="CommandHandlerAttribute"/>.<br/>
    /// To return errors use one of the error methods of <see cref="Result"/> like <see cref="Result.Error(string)"/> 
    /// </summary>
    public readonly struct Result<T>
    {
        internal  readonly  T           value;
        internal  readonly  ResultError error;

        public    override  string      ToString() => error.message != null ? error.ToString() : $"{value}";

        internal            bool        Success => error.message == null;      
        
        internal Result (T value) {
            this.value  = value;
            this.error  = default;
        }
        
        private Result (in ResultError error) {
            this.value  = default;
            this.error  = error;
        }
        
        public static implicit operator Result<T>(T value)              => new Result<T>(value);
        public static implicit operator Result<T>(in ResultError error) => new Result<T>(error);
    }
    
    public readonly struct ResultError
    {
        public  readonly    TaskErrorType   errorType;
        public  readonly    string          message;
        
        public  override    string          ToString() => message != null ? $"{errorType}: {message}" : "Success";
        
        public ResultError (string message) {
            this.message    = message;
            errorType       = TaskErrorType.CommandError;
        }
        
        public ResultError (TaskErrorType errorType, string message) {
            this.message    = message;
            this.errorType  = errorType;
        }
    }
}