// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Friflo.Json.Fliox.Mapper.Map.Utils
{
    public static class HubCommandsUtils
    {
        private static readonly Dictionary<Type, HubMessageInfo[]> HubMessageInfoCache = new Dictionary<Type, HubMessageInfo[]>();
        
        private const string HubCommandsType = "Friflo.Json.Fliox.Hub.Client.HubMessages";

        /// <summary> type is FlioxClient or a derived type </summary>
        internal static HubMessageInfo[] GetHubMessageInfos(Type type) {
            var cache = HubMessageInfoCache;
            if (cache.TryGetValue(type, out  HubMessageInfo[] result)) {
                return result;
            }
            var messageInfos = new List<HubMessageInfo>();
            var classType = type;
            while (classType != null && classType != typeof(object)) {
                var flags   = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                FieldInfo[] fields = classType.GetFields(flags);
                foreach (var field in fields) {
                    if (!IsHubCommands(field.FieldType, field.Name, out HubMessageInfo messageInfo))
                        continue;
                    messageInfos.Add(messageInfo);
                }
                PropertyInfo[] properties = classType.GetProperties(flags);
                foreach (var property in properties) {
                    if (!IsHubCommands(property.PropertyType, property.Name, out HubMessageInfo messageInfo))
                        continue;
                    messageInfos.Add(messageInfo);
                }
                classType = classType.BaseType;
            }
            if (messageInfos.Count == 0) {
                cache[type] = null;
                return null;
            }
            var array = messageInfos.ToArray();
            cache[type] = array;
            return array;
        }
        
        private static bool IsHubCommands(Type memberType, string name, out HubMessageInfo messageInfo) {
            messageInfo = new HubMessageInfo();
            var type = memberType;
            while (type != null) {
                if (type.FullName == HubCommandsType) {
                    messageInfo = new HubMessageInfo(name, memberType);
                    return true;
                }
                if (!type.IsClass)
                    return false;
                type = type.BaseType;
            }
            return false;
        }
    }
    
    internal readonly struct HubMessageInfo {
        internal    readonly    string  name;
        internal    readonly    Type    commandsType;

        public      override    string  ToString() => name;

        internal HubMessageInfo (string name, Type commandsType) {
            this.name           = name;
            this.commandsType   = commandsType;
        }
    }
}