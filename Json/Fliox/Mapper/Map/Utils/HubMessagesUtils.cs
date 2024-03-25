// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Fliox.Mapper.Utils;

namespace Friflo.Json.Fliox.Mapper.Map.Utils
{
    public static class HubMessagesUtils
    {
        private static readonly Dictionary<Type, MessageInfo[]> MessageInfoCache    = new Dictionary<Type, MessageInfo[]>();
        private static readonly Dictionary<Type, string>        PrefixCache         = new Dictionary<Type, string>();

        private const string SchemaType  = "Friflo.Json.Fliox.Hub.Client.FlioxClient";
        private const string MessageType = "Friflo.Json.Fliox.Hub.Client.MessageTask";
        private const string CommandType = "Friflo.Json.Fliox.Hub.Client.CommandTask`1";
        
        internal static bool IsSchemaType(Type schemaType) {
            var type = schemaType;
            while (type != null) {
                if (type.FullName == SchemaType) {
                    return true;
                }
                type = type.BaseType;
            }
            return false;
        }
        
        public static MessageInfo[] GetMessageInfos (Type type, TypeStore typeStore) {
            var docs            = typeStore.assemblyDocs;
            var messageInfos    = new List<MessageInfo>();
            var messagePrefix   = GetMessagePrefix(type.CustomAttributes);
            var messages        = GetMessageInfos(type, messagePrefix, docs);
            if (messages != null) {
                messageInfos.AddRange(messages);
            }
            var hubCommands     = HubCommandsUtils.GetHubMessageInfos(type);
            if (hubCommands != null) {
                foreach (var hubCommand in hubCommands) {
                    var prefix          = hubCommand.name + ".";
                    var clientCommands  = GetMessageInfos(hubCommand.commandsType, prefix, docs);
                    if (clientCommands == null)
                        continue;
                    messageInfos.AddRange(clientCommands);
                }
            }
            return messageInfos.ToArray();
        }

        private static MessageInfo[] GetMessageInfos(Type type, string prefix, AssemblyDocs docs) {
            if (MessageInfoCache.TryGetValue(type, out  MessageInfo[] result)) {
                return result;
            }
            var messageInfos    = new List<MessageInfo>();
            var flags           = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            MethodInfo[] methods = type.GetMethods(flags);
            for (int n = 0; n < methods.Length; n++) {
                var  method         = methods[n];
                if (!IsCommand(method, prefix, docs, out MessageInfo messageInfo))
                    continue;
                messageInfos.Add(messageInfo);
            }
            if (messageInfos.Count == 0) {
                MessageInfoCache[type] = null;
                return null;
            }
            var array = messageInfos.ToArray();
            MessageInfoCache[type] = array;
            return array;
        }
        
        private static bool IsCommand(MethodInfo methodInfo, string prefix, AssemblyDocs docs, out MessageInfo messageInfo) {
            messageInfo = new MessageInfo();
            var returnType = methodInfo.ReturnType;
            Type resultType;
            if (returnType.IsGenericType) {
                if (returnType.GetGenericTypeDefinition().FullName != CommandType)
                    return false;
                var returnTypeArgs = returnType.GenericTypeArguments;
                if (returnTypeArgs.Length != 1)
                    return false;
                resultType = returnTypeArgs[0];
                if (resultType.IsGenericParameter)
                    return false;
            } else {
                if (returnType.FullName != MessageType)
                    return false;
                resultType = null;
            }
            var name = AttributeUtils.CommandName(methodInfo.CustomAttributes);
            if (name == null) {
                name = methodInfo.Name;
                if (name == "SendMessage")
                    return false;
            }
            var parameters  = methodInfo.GetParameters();
            var paramLen    = parameters.Length; 
            if (paramLen > 1)
                return false;
            Type paramType = null;
            if (paramLen == 1) {
                paramType = parameters[0].ParameterType;
            }
            var qualifiedName   = prefix == "" ? name : prefix + name;
            var doc             = GetDocs(docs, methodInfo, paramType, paramLen);
            
            messageInfo = new MessageInfo(qualifiedName, paramType, resultType, doc);
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
                var underlyingType  = Nullable.GetUnderlyingType(paramType);  
                var paramTypeName   = underlyingType != null ? $"System.Nullable{{{underlyingType.FullName}}}" : paramType.FullName;
                signature   = $"M:{declaringType.FullName}.{methodInfo.Name}({paramTypeName})";
            }
            var doc         = docs.GetDocs(assembly, signature);
            return doc;
        }
        
        public static string GetMessagePrefix(Type type) {
            string  prefix;
            lock (PrefixCache) {
                if (PrefixCache.TryGetValue(type, out prefix)) {
                    return prefix;
                }
            }
            var attributes  = type.CustomAttributes;
            prefix          = GetMessagePrefix(attributes);
            lock (PrefixCache) {
                PrefixCache.TryAdd(type, prefix);    
            }
            return prefix;
        }
        
        public static string GetMessagePrefix(IEnumerable<CustomAttributeData> attributes) {
            foreach (var attr in attributes) {
                if (attr.AttributeType == typeof(MessagePrefixAttribute)) {
                    var arg     = attr.ConstructorArguments;
                    var value   = (string)arg[0].Value;
                    return value ?? "";
                }
            }
            return "";
        }
    }
    
    public readonly struct MessageInfo {
        public  readonly    string  name;
        /// <summary>null: missing param    <br/>not null: message/command param: Type</summary>
        public  readonly    Type    paramType;
        /// <summary>null: is message       <br/>not null: is command</summary>
        public  readonly    Type    resultType;
        public  readonly    string  doc;

        public  override    string  ToString() => name;

        internal MessageInfo (
            string  name,
            Type    paramType,
            Type    resultType,
            string  doc)
        {
            this.name       = name;
            this.paramType  = paramType;
            this.resultType = resultType;
            this.doc        = doc;
        }
    }
}