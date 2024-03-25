// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
namespace Friflo.Json.Fliox.Mapper
{
    internal interface IJsonNaming
    {
        string PropertyName(string name);
    }
    
    internal sealed class DefaultNaming : IJsonNaming
    {
        internal static readonly IJsonNaming Instance = new DefaultNaming();
        
        public string PropertyName(string name) {
            return name;
        }
    }
    
    internal sealed class CamelCaseNaming : IJsonNaming
    {
        internal static readonly IJsonNaming Instance = new CamelCaseNaming();
        
        public string PropertyName(string name) {
            return char.ToLower(name[0]) + name.Substring(1);
        }
    }
    
    internal sealed class PascalCaseNaming : IJsonNaming
    {
        internal static readonly IJsonNaming Instance = new PascalCaseNaming();
        
        public string PropertyName(string name) {
            return char.ToUpper(name[0]) + name.Substring(1);
        }
    }
}
