// Copyright (c) Ullrich Praetz. All rights reserved.
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
        private static readonly Dictionary<Type, HandlerInfo[]> HandlerInfoCache = new Dictionary<Type, HandlerInfo[]>();

        internal static HandlerInfo[] GetHandlers(Type type) {
            if (HandlerInfoCache.TryGetValue(type, out  HandlerInfo[] result)) {
                return result;
            }
            var handlers = new List<HandlerInfo>();
            var flags   = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static; // | BindingFlags.FlattenHierarchy;
            MethodInfo[] methods = type.GetMethods(flags);
            for (int n = 0; n < methods.Length; n++) {
                var  method         = methods[n];
                if (!IsHandler(method, out HandlerInfo commandInfo))
                    continue;
                handlers.Add(commandInfo);
            }
            if (handlers.Count == 0) {
                HandlerInfoCache[type] = null;
                return null;
            }
            var array = handlers.ToArray();
            HandlerInfoCache[type] = array;
            return array;
        }
        
        private static bool IsHandler(MethodInfo methodInfo, out HandlerInfo handlerInfo) {
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
            var  resultType      = methodInfo.ReturnType;
            Type resultType2;
            bool isAsync = false;
            if (!resultType.IsGenericType) {
                if (resultType == typeof(Task)) {
                    isAsync = true;
                    resultType2 = typeof(void);
                } else {
                    resultType2 = resultType;
                }
            } else {
                resultType2 = GetGenericResultType (resultType, out isAsync);
                if (resultType2 == null) {
                    return false;
                }
            }
            var name = AttributeUtils.CommandName(methodInfo.CustomAttributes);
            if (name == null) {
                name = methodInfo.Name;
            }
            handlerInfo = new HandlerInfo(name, methodInfo, paramType, resultType2, isAsync);
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
    
    internal readonly struct HandlerInfo {
        public  readonly    string      name;
        public  readonly    MethodInfo  method;
        public  readonly    Type        valueType;
        public  readonly    Type        resultType;
        public  readonly    bool        isAsync;

        public  override    string  ToString() => name;

        internal HandlerInfo (
            string      name,
            MethodInfo  method,
            Type        valueType,
            Type        resultType,
            bool        isAsync)
        {
            this.name           = name;
            this.method         = method;
            this.valueType      = valueType;
            this.resultType     = resultType;
            this.isAsync        = isAsync;
        }
    }
}
