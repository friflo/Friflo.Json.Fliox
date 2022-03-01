// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Mapper;

using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// <see cref="Command{TParam}"/> expose all data relevant for command execution as properties or methods. <br/>
    /// - the command <see cref="Name"/> == method name <br/>
    /// - the unvalidated input parameter <see cref="GetParam"/> of type <typeparamref name="TParam"/><br/>
    /// - the validated input parameter <see cref="ValidateParam"/> of type <typeparamref name="TParam"/><br/>
    /// - the input parameter <see cref="JsonParam"/> as raw JSON <br/>
    /// - the <see cref="DatabaseName"/> <br/>
    /// - the <see cref="Database"/> instance <br/>
    /// - the <see cref="Hub"/> exposing general Hub information <br/>
    /// - a <see cref="Pool"/> mainly providing common utilities to transform JSON <br/>
    /// </summary>
    /// <typeparam name="TParam">Type of the command input parameter</typeparam>
    public class Command<TParam>{
        public              string          Name            { get; }
        public              IPool           Pool            => messageContext.pool;
        public              FlioxHub        Hub             => messageContext.hub;
        public              JsonValue       JsonParam       => param;
        public              string          DatabaseName    => messageContext.DatabaseName;
        public              EntityDatabase  Database        => messageContext.Database;
        public              User            User            => messageContext.User;
        public              JsonKey         ClientId        => messageContext.clientId;
        public              bool            WriteNull       { get; set; }
        
        internal            string          error;

        [DebuggerBrowsable(Never)]  internal            MessageContext  MessageContext  => messageContext;
        [DebuggerBrowsable(Never)]  private   readonly  JsonValue       param;
        [DebuggerBrowsable(Never)]  private   readonly  MessageContext  messageContext;

        public   override   string          ToString()      => $"{Name}(param: {param.AsString()})";
        
        public              UserInfo        UserInfo { get {
            var user = messageContext.User;
            return new UserInfo (user.userId, user.token, messageContext.clientId);
        } }

        internal Command(string name, JsonValue param, MessageContext messageContext) {
            Name                = name;
            this.param          = param;  
            this.messageContext = messageContext;
        }
        
    /*  public TParam Param { get {
            using (var pooled = messageContext.pool.ObjectMapper.Get()) {
                var reader = pooled.instance.reader;
                return reader.Read<TParam>(param);
            }
        }} */
    
        public bool GetParam(out TParam result, out string error) {
            return GetParam<TParam>(out result, out error);
        }
        
        public bool GetParam<T>(out T result, out string error) {
            using (var pooled = messageContext.pool.ObjectMapper.Get()) {
                var reader  = pooled.instance.reader;
                result      = reader.Read<T>(param);
                if (reader.Error.ErrSet) {
                    error   = reader.Error.msg.ToString();
                    return false;
                }
                error = null;
                return true;
            }
        }

        public bool ValidateParam(out TParam result, out string error) {
            return ValidateParam<TParam>(out result, out error);
        }
        
        public bool ValidateParam<T>(out T result, out string error) {
            var paramValidation = messageContext.sharedCache.GetValidationType(typeof(T));
            using (var pooled = messageContext.pool.TypeValidator.Get()) {
                var validator   = pooled.instance;
                if (!validator.ValidateField(param, paramValidation, out error)) {
                    result = default;
                    return false;
                }
            }
            return GetParam(out result, out error);
        }
        
        /// <summary>Set result of <see cref="Command{TParam}"/> execution to an error</summary>
        public void Error(string message) {
            error = message;
        }

        /// <summary>Set result of <see cref="Command{TParam}"/> execution to an error. <br/>
        /// It returns the default value of the given <typeparamref name="TResult"></typeparamref> to simplify
        /// returning from a command handler with a single statement like:
        /// <code>
        /// if (!command.ValidateParam(out var param, out var error))
        ///     return command.Error &lt;int&gt;(error);
        /// </code>  
        /// </summary>
        public TResult Error<TResult>(string message) {
            error = message;
            return default;
        }
    }
}