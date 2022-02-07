// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Hub.Host.Utils
{
    internal static class TaskHandlerUtils
    {
        private static readonly Dictionary<Type, HandlerInfo[]> HandlerInfoCache = new Dictionary<Type, HandlerInfo[]>();

        internal static HandlerInfo[] GetHandlers(Type type) {
            if (HandlerInfoCache.TryGetValue(type, out  HandlerInfo[] result)) {
                return result;
            }
            var handlers = new List<HandlerInfo>();
            var flags   = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
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
            
            var parameters = methodInfo.GetParameters();
            if (parameters.Length != 1)
                return false;
            var commandParam = parameters[0];
            var genericParamType = commandParam.ParameterType;
            if (!genericParamType.IsGenericType)
                return false;
            if (genericParamType.GetGenericTypeDefinition() != typeof(Command<>))
                return false;
            var genericArgs = genericParamType.GenericTypeArguments;
            if (genericArgs.Length != 1)
                return false;
            var paramType = genericArgs[0];
            var resultType = methodInfo.ReturnType;

            var name = AttributeUtils.CommandName(methodInfo.CustomAttributes);
            if (name == null)
                name = methodInfo.Name;

            handlerInfo = new HandlerInfo(name, paramType, resultType);
            return true;
        }
    }
    
    internal readonly struct HandlerInfo {
        public  readonly    string  name;
        public  readonly    Type    valueType;
        public  readonly    Type    resultType;

        public  override    string  ToString() => name;

        internal HandlerInfo (
            string         name,
            Type           valueType,
            Type           resultType)
        {
            this.name       = name;
            this.valueType  = valueType;
            this.resultType = resultType;
        }
    }
}
