// Copyright (c) Ullrich Praetz. All rights reserved.
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

        internal static HubMessageInfo[] GetHubMessageInfos(Type type) {
            if (HubMessageInfoCache.TryGetValue(type, out  HubMessageInfo[] result)) {
                return result;
            }
            var messageInfos = new List<HubMessageInfo>();
            var flags   = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo[] fields = type.GetFields(flags);
            for (int n = 0; n < fields.Length; n++) {
                var  field         = fields[n];
                if (!IsHubCommands(field, out HubMessageInfo messageInfo))
                    continue;
                messageInfos.Add(messageInfo);
            }
            if (messageInfos.Count == 0) {
                HubMessageInfoCache[type] = null;
                return null;
            }
            var array = messageInfos.ToArray();
            HubMessageInfoCache[type] = array;
            return array;
        }
        
        private static bool IsHubCommands(FieldInfo fieldInfo, out HubMessageInfo messageInfo) {
            messageInfo = new HubMessageInfo();
            var fieldType   = fieldInfo.FieldType;
            var type        = fieldType;
            
            while (type != null) {
                if (type.FullName == HubCommandsType) {
                    messageInfo = new HubMessageInfo(fieldInfo.Name, fieldType);
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