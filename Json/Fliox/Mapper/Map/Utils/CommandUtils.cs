// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Utils
{
    public static class CommandUtils
    {
        private static readonly Dictionary<Type, CommandInfo[]> CommandInfoCache = new Dictionary<Type, CommandInfo[]>();

        private const string CommandType = "Friflo.Json.Fliox.Hub.Client.CommandTask`1";
        
        public static CommandInfo[] GetCommandInfos (Type type) {
            var docs            = new AssemblyDocs();
            var commandInfos    = new List<CommandInfo>();
            var commandPrefix   = GetCommandPrefix(type.CustomAttributes);
            var commands        = GetCommandTypes(type, commandPrefix, docs);
            if (commands != null) {
                commandInfos.AddRange(commands);
            }
            var hubCommands     = HubCommandsUtils.GetHubCommandsTypes(type);
            if (hubCommands != null) {
                foreach (var hubCommand in hubCommands) {
                    var prefix          = hubCommand.name + ".";
                    var clientCommands  = GetCommandTypes(hubCommand.commandsType, prefix, docs);
                    if (clientCommands == null)
                        continue;
                    commandInfos.AddRange(clientCommands);
                }
            }
            return commandInfos.ToArray();
        }

        private static CommandInfo[] GetCommandTypes(Type type, string prefix, AssemblyDocs docs) {
            if (CommandInfoCache.TryGetValue(type, out  CommandInfo[] result)) {
                return result;
            }
            var commands = new List<CommandInfo>();
            var flags   = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            MethodInfo[] methods = type.GetMethods(flags);
            for (int n = 0; n < methods.Length; n++) {
                var  method         = methods[n];
                if (!IsCommand(method, prefix, docs, out CommandInfo commandInfo))
                    continue;
                commands.Add(commandInfo);
            }
            if (commands.Count == 0) {
                CommandInfoCache[type] = null;
                return null;
            }
            var array = commands.ToArray();
            CommandInfoCache[type] = array;
            return array;
        }
        
        private static bool IsCommand(MethodInfo methodInfo, string prefix, AssemblyDocs docs, out CommandInfo commandInfo) {
            commandInfo = new CommandInfo();
            var returnType = methodInfo.ReturnType;
            if (!returnType.IsGenericType)
                return false;
            if (returnType.GetGenericTypeDefinition().FullName != CommandType)
                return false;
            var returnTypeArgs = returnType.GenericTypeArguments;
            if (returnTypeArgs.Length != 1)
                return false;
            var resultType = returnTypeArgs[0];
            if (resultType.IsGenericParameter)
                return false;
            var name = AttributeUtils.CommandName(methodInfo.CustomAttributes);
            if (name == null)
                name = methodInfo.Name;
            var parameters = methodInfo.GetParameters();
            if (parameters.Length > 1)
                return false;
            Type valueType = typeof(JsonValue);
            if (parameters.Length == 1) {
                valueType = parameters[0].ParameterType;
            }
            var qualifiedName   =  prefix == "" ? name : prefix + name;
            var assembly        = methodInfo.DeclaringType?.Assembly;
            var doc             = docs.GetDocs(assembly, "XXX");
            
            commandInfo = new CommandInfo(qualifiedName, valueType, resultType, doc);
            return true;
        }
        
        private static string GetCommandPrefix(IEnumerable<CustomAttributeData> attributes) {
            foreach (var attr in attributes) {
                if (attr.AttributeType == typeof(Fri.CommandPrefixAttribute)) {
                    var arg     = attr.ConstructorArguments;
                    var value   = (string)arg[0].Value;
                    return value ?? "";
                }
            }
            return "";
        }
    }
    
    public readonly struct CommandInfo {
        public  readonly    string  name;
        public  readonly    Type    valueType;
        public  readonly    Type    resultType;
        public  readonly    string  docs;

        public  override    string  ToString() => name;

        internal CommandInfo (
            string  name,
            Type    valueType,
            Type    resultType,
            string  docs)
        {
            this.name       = name;
            this.valueType  = valueType;
            this.resultType = resultType;
            this.docs       = docs;
        }
    }
}