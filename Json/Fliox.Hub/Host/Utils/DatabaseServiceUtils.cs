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
            var resultType      = methodInfo.ReturnType;
            Type resultTaskType = null;
            // is return type of command handler of type: Task<TResult> ?  (==  is async command handler)
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>)) {
                var genericResultArgs   = resultType.GenericTypeArguments; // Length == 1
                resultTaskType          = genericResultArgs[0];
            }
            var name = AttributeUtils.CommandName(methodInfo.CustomAttributes);
            if (name == null)
                name = methodInfo.Name;

            handlerInfo = new HandlerInfo(name, methodInfo, paramType, resultType, resultTaskType);
            return true;
        }
    }
    
    internal readonly struct HandlerInfo {
        public  readonly    string      name;
        public  readonly    MethodInfo  method;
        public  readonly    Type        valueType;
        public  readonly    Type        resultType;
        public  readonly    Type        resultTaskType;

        public  override    string  ToString() => name;

        internal HandlerInfo (
            string      name,
            MethodInfo  method,
            Type        valueType,
            Type        resultType,
            Type        resultTaskType)
        {
            this.name           = name;
            this.method         = method;
            this.valueType      = valueType;
            this.resultType     = resultType;
            this.resultTaskType = resultTaskType;
        }
    }
}
