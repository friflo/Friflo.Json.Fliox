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
        
        public static CommandInfo[] GetCommandInfos (Type type, TypeStore typeStore) {
            var docs            = typeStore.assemblyDocs;
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
            var parameters  = methodInfo.GetParameters();
            var paramLen    = parameters.Length; 
            if (paramLen > 1)
                return false;
            Type paramType = typeof(JsonValue);
            if (paramLen == 1) {
                paramType = parameters[0].ParameterType;
            }
            var qualifiedName   = prefix == "" ? name : prefix + name;
            var doc             = GetDocs(docs, methodInfo, paramType, paramLen);
            
            commandInfo = new CommandInfo(qualifiedName, paramType, resultType, doc);
            return true;
        }
        
        private static string GetDocs(AssemblyDocs docs, MethodInfo methodInfo, Type paramType, int paramLen) {
            var declaringType   = methodInfo.DeclaringType;
            if (declaringType == null)
                return null;
            var assembly    = declaringType.Assembly;
            string signature;
            if (paramLen == 0) {
                signature   = $"M:{declaringType.FullName}.{methodInfo.Name}";
            } else {
                signature   = $"M:{declaringType.FullName}.{methodInfo.Name}({paramType.FullName})";
            }
            var doc         = docs.GetDocs(assembly, signature);
            return doc;
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
        public  readonly    Type    paramType;
        public  readonly    Type    resultType;
        public  readonly    string  docs;

        public  override    string  ToString() => name;

        internal CommandInfo (
            string  name,
            Type    paramType,
            Type    resultType,
            string  docs)
        {
            this.name       = name;
            this.paramType  = paramType;
            this.resultType = resultType;
            this.docs       = docs;
        }
    }
}