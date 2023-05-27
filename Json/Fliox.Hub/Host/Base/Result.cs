// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
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

    public readonly struct Result<T>
    {
        internal  readonly  T           value;
        internal  readonly  ResultError error;

        public    override  string      ToString() => error.message != null ? error.ToString() : $"{value}";

        internal            bool        Success => error.message == null;      
        
        public Result (T value) {
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