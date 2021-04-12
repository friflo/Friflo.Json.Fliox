// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
namespace Friflo.Json.Flow.Mapper
{
    public interface IJsonNaming
    {
        string PropertyName(string name);
    }
    
    public class DefaultNaming : IJsonNaming
    {
        public string PropertyName(string name) {
            return name;
        }
    }
    
    public class CamelCaseNaming : IJsonNaming
    {
        public string PropertyName(string name) {
            return char.ToLower(name[0]) + name.Substring(1);
        }
    }
    
    public class PascalCaseNaming : IJsonNaming
    {
        public string PropertyName(string name) {
            return char.ToUpper(name[0]) + name.Substring(1);
        }
    }
}
