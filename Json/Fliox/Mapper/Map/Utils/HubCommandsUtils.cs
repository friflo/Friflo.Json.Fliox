// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Friflo.Json.Fliox.Mapper.Map.Utils
{
    public static class HubCommandsUtils
    {
        private static readonly Dictionary<Type, HubCommandsInfo[]> HubCommandsInfoCache = new Dictionary<Type, HubCommandsInfo[]>();
        
        private const string HubCommandsType = "Friflo.Json.Fliox.Hub.Client.HubCommands";

        internal static HubCommandsInfo[] GetHubCommandsTypes(Type type) {
            if (HubCommandsInfoCache.TryGetValue(type, out  HubCommandsInfo[] result)) {
                return result;
            }
            var commands = new List<HubCommandsInfo>();
            var flags   = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo[] fields = type.GetFields(flags);
            for (int n = 0; n < fields.Length; n++) {
                var  field         = fields[n];
                if (!IsHubCommands(field, out HubCommandsInfo commandInfo))
                    continue;
                commands.Add(commandInfo);
            }
            if (commands.Count == 0) {
                HubCommandsInfoCache[type] = null;
                return null;
            }
            var array = commands.ToArray();
            HubCommandsInfoCache[type] = array;
            return array;
        }
        
        private static bool IsHubCommands(FieldInfo fieldInfo, out HubCommandsInfo hubCommandsInfo) {
            hubCommandsInfo = new HubCommandsInfo();
            var fieldType   = fieldInfo.FieldType;
            var type        = fieldType;
            
            while (type != null) {
                if (type.FullName == HubCommandsType) {
                    hubCommandsInfo = new HubCommandsInfo(fieldInfo.Name, fieldType);
                    return true;
                }
                if (!type.IsClass)
                    return false;
                type = type.BaseType;
            }
            return false;
        }
    }
    
    internal readonly struct HubCommandsInfo {
        internal    readonly    string  name;
        internal    readonly    Type    commandsType;

        public      override    string  ToString() => name;

        internal HubCommandsInfo (string name, Type commandsType) {
            this.name           = name;
            this.commandsType   = commandsType;
        }
    }
}