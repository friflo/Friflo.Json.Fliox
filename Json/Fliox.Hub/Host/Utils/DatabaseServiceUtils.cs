// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Hub.Host.Utils
{
    internal static class DatabaseServiceUtils
    {
        private static readonly Dictionary<Type, ServiceInfo> ServiceInfoCache            = new Dictionary<Type, ServiceInfo>();
        private static readonly Dictionary<Type, ServiceInfo> AttributedServiceInfoCache  = new Dictionary<Type, ServiceInfo>();
        
        internal static ServiceInfo GetAttributedHandlers(Type type) {
            var cache = AttributedServiceInfoCache;
            if (cache.TryGetValue(type, out  var result)) {
                return result;
            }
            var handlers    = new List<HandlerInfo>();
            var types       = new HashSet<Type>();
            if (!AddHandlers(type, handlers, types, out string error)) {
                return new ServiceInfo(null, error);
            }
            if (handlers.Count == 0) {
                cache[type] = null;
                return null;
            }
            var array = handlers.ToArray();
            var infos = new ServiceInfo(array, null);
            cache[type] = infos;
            return infos;
        }
        
        private static bool AddHandlers(Type type, List<HandlerInfo> handlers, HashSet<Type> types, out string error)
        {
            if (!types.Add(type)) {
                error = null;
                return true;
            }
            if (type == null                    ||
                type == typeof(object)          ||
                type == typeof(string)          ||
                type == typeof(DatabaseService) ||
                !type.IsClass)
            {
                error = null;
                return true;
            }
            var methodFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MethodInfo[] methods = type.GetMethods(methodFlags);
            for (int n = 0; n < methods.Length; n++) {
                var method      = methods[n];
                var handlerType = AttributeUtils.GetHandler(method.CustomAttributes, out string commandName);
                if (handlerType == HandlerType.None) {
                    continue;
                }
                var name = commandName ?? method.Name; 
                if (!GetHandler(method, name, out HandlerInfo handler)) {
                    error = $"invalid [{handlerType}] - method: {method.DeclaringType?.Name}.{method.Name}()";
                    return false;
                }
                if (handlerType != handler.type) {
                    error = $"expected [{handlerType}], was [{handler.type}] - method: {method.DeclaringType?.Name}.{method.Name}()";
                    return false;
                }
                handlers.Add(handler);
            }
            if (!AddHandlers(type.BaseType, handlers, types, out error)) {
                return false;
            }
            error = null;
            return true;
        }

        internal static ServiceInfo GetHandlers(Type type) {
            var cache = ServiceInfoCache;
            if (cache.TryGetValue(type, out var result)) {
                return result;
            }
            var handlers = new List<HandlerInfo>();
            var flags   = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static; // | BindingFlags.FlattenHierarchy;
            MethodInfo[] methods = type.GetMethods(flags);
            for (int n = 0; n < methods.Length; n++) {
                var  method         = methods[n];
                if (!GetHandler(method, method.Name, out HandlerInfo commandInfo))
                    continue;
                handlers.Add(commandInfo);
            }
            if (handlers.Count == 0) {
                cache[type] = null;
                return null;
            }
            var array = handlers.ToArray();
            var serviceInfo = new ServiceInfo(array, null);
            cache[type] = serviceInfo;
            return serviceInfo;
        }
        
        private static bool GetHandler(MethodInfo methodInfo, string name, out HandlerInfo handlerInfo) {
            handlerInfo = new HandlerInfo();
            // if (methodInfo.Name == "DbContainers") { var i = 1; }
            
            var parameters = methodInfo.GetParameters();
            if (parameters.Length != 2)
                return false;
            var commandParam    = parameters[0];
            var command         = parameters[1];
            var commandType     = command.ParameterType;
            if (commandType != typeof(MessageContext))
                return false;
            var genericParamType = commandParam.ParameterType;
            if (!genericParamType.IsGenericType)
                return false;
            if (genericParamType.GetGenericTypeDefinition() != typeof(Param<>))
                return false;
            var genericArgs = genericParamType.GenericTypeArguments;
            if (genericArgs.Length != 1)
                return false;
            var paramType       = genericArgs[0];
            
            // --- result type
            Type returnType     = methodInfo.ReturnType;
            Type resultType;
            bool isAsync = false;
            if (returnType.IsGenericType) {
                resultType = GetGenericResultType (returnType, out isAsync);
                if (resultType == null) {
                    return false;
                }
            } else {
                if (returnType == typeof(Task)) {
                    isAsync = true;
                    resultType = typeof(void);
                } else if (returnType == typeof(void)) {
                    resultType = returnType;
                } else {
                    return false;
                }
            }
            handlerInfo = new HandlerInfo(name, methodInfo, paramType, resultType, isAsync);
            return true;
        }
        
        // Is return type of command handler of type:
        //      Task<Result<TResult>>  (==  is async command handler)
        // or        Result<TResult>
        private static Type GetGenericResultType(Type resultType, out bool isAsync) {
            isAsync = false;
            var genericResultArgs = resultType.GenericTypeArguments;
            if (genericResultArgs.Length != 1) {
                return null;
            }
            var genericResultArg    = genericResultArgs[0];
            var genericResultType   = resultType.GetGenericTypeDefinition();
            if (genericResultType == typeof(Task<>)) {
                isAsync = true;
                if (!genericResultArg.IsGenericType) {
                    return null;
                }
                var genResult = genericResultArg.GetGenericTypeDefinition();
                if (genResult != typeof(Result<>)) {
                    return null;
                }
                var genericTaskArgs = genericResultArg.GenericTypeArguments;
                if (genericTaskArgs.Length != 1) {
                    return null;
                }
                return genericTaskArgs[0];
            }
            if (genericResultType != typeof(Result<>)) {
                return null;
            }
            return genericResultArg;
        }
    }
    
    internal sealed class ServiceInfo
    {
        internal readonly HandlerInfo[]  handlers;
        internal readonly string         error;
        
        internal ServiceInfo(HandlerInfo[] handlers, string error) {
            this.handlers   = handlers;
            this.error      = error;
        }
    }
    
    internal readonly struct HandlerInfo {
        public  readonly    string      name;
        public  readonly    HandlerType type;
        public  readonly    MethodInfo  method;
        /// <summary>
        /// The type <c>TParam</c> of the <see cref="Param{TParam}"/> parameter of a method attributed with
        /// <see cref="CommandHandlerAttribute"/> or <see cref="MessageHandlerAttribute"/></summary>
        public  readonly    Type        valueType;
        /// <summary>
        /// For methods attributed with <see cref="CommandHandlerAttribute"/> the type <c>T</c> of a <see cref="Result{T}"/><br/>
        /// For methods attributed with <see cref="MessageHandlerAttribute"/> the type is <see cref="Void"/><br/>
        /// </summary>
        public  readonly    Type        resultType;
        public  readonly    bool        isAsync;
        public  readonly    Type        handlerDelegate;
        public  readonly    Type        messageDelegate;

        public  override    string      ToString() => name;

        internal HandlerInfo (
            string      name,
            MethodInfo  method,
            Type        valueType,
            Type        resultType,
            bool        isAsync)
        {
            this.name       = name;
            this.method     = method;
            this.valueType  = valueType;
            this.resultType = resultType;
            this.isAsync    = isAsync;
            type            = this.resultType == typeof(void) ? HandlerType.MessageHandler : HandlerType.CommandHandler;
            
            if (resultType == typeof(void)) {
                var genericArgs = new Type[1]; // <TParam>
                genericArgs[0]  = valueType;
                if (isAsync) {
                    handlerDelegate = typeof(HostMessageHandlerAsync<>) .MakeGenericType(genericArgs);
                    messageDelegate = typeof(MessageDelegateAsync<>)    .MakeGenericType(genericArgs);
                } else {
                    handlerDelegate = typeof(HostMessageHandler<>)      .MakeGenericType(genericArgs);
                    messageDelegate = typeof(MessageDelegate<>)         .MakeGenericType(genericArgs);
                }
            } else {
                var genericArgs = new Type[2];  // <TParam, TResult>
                genericArgs[0]  = valueType;
                genericArgs[1]  = resultType;
                if (isAsync) {
                    handlerDelegate = typeof(HostCommandHandlerAsync<,>).MakeGenericType(genericArgs);
                    messageDelegate = typeof(CommandDelegateAsync<,>)   .MakeGenericType(genericArgs);
                } else {
                    handlerDelegate = typeof(HostCommandHandler<,>)     .MakeGenericType(genericArgs);
                    messageDelegate = typeof(CommandDelegate<,>)        .MakeGenericType(genericArgs);
                }
            }
        }
    }
}
