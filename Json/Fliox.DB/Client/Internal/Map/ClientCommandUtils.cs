// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Friflo.Json.Fliox.DB.Client.Internal.Map
{
    internal static class ClientCommandUtils
    {
        private static readonly Dictionary<Type, CommandInfo[]> CommandInfoCache = new Dictionary<Type, CommandInfo[]>();
        
        internal static CommandInfo[] GetCommandTypes(Type type) {
            if (CommandInfoCache.TryGetValue(type, out  CommandInfo[] result)) {
                return result;
            }
            var commands = new List<CommandInfo>();
            var flags   = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            MethodInfo[] methods = type.GetMethods(flags);
            for (int n = 0; n < methods.Length; n++) {
                var  method         = methods[n];
                if (!IsCommand(method, out CommandInfo commandInfo))
                    continue;
                commands.Add(commandInfo);
            }
            var array = commands.ToArray();
            CommandInfoCache[type] = array;
            return array;
        }
        
        private static bool IsCommand(MethodInfo methodInfo, out CommandInfo commandInfo) {
            commandInfo = new CommandInfo();
            var returnType = methodInfo.ReturnType;
            if (!returnType.IsGenericType)
                return false;
            if (!(returnType.GetGenericTypeDefinition() == typeof(CommandTask<>)))
                return false;
            var parameters = methodInfo.GetParameters();
            Type valueType = null;
            if (parameters.Length > 0) {
                var param = parameters[0];
                valueType = param.ParameterType;
            }
            var resultType = returnType.GenericTypeArguments[0];
            commandInfo = new CommandInfo(methodInfo.Name, valueType, resultType);
            return true;
        }
    }
    
    internal readonly struct CommandInfo {
        internal readonly   string         name;
        internal readonly   Type           valueType;
        internal readonly   Type           resultType;
        
        internal CommandInfo (
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