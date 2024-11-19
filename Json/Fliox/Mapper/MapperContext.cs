using System;
using Friflo.Json.Fliox.Mapper.Map;

namespace Friflo.Json.Fliox.Mapper
{
    /// <summary>
    /// Use to attach user specific data with to an <see cref="ObjectReader"/> and <see cref="ObjectWriter"/> with
    /// <see cref="ObjectReader.SetMapperContext{T}"/> and <see cref="ObjectWriter.SetMapperContext{T}"/>.
    /// <br/>
    /// This data can be accessed in a custom TypeMapper with
    /// <see cref="Reader.GetMapperContext{T}"/> and <see cref="Writer.GetMapperContext{T}"/>. 
    /// </summary>
    public interface IMapperContext { }
    
    internal static class MapperContext
    {
        private static int _contextIndexSeq;
        
        internal static int GetNextIndex() {
            return _contextIndexSeq++;
        }

        internal static T GetMapperContext<T>(object[] contextMap) where T : class, IMapperContext
        {
            if (contextMap != null) {
                var index = MapperContextInfo<T>.Index;
                if (index < contextMap.Length) {
                    var context = contextMap[index];
                    if (context != null) {
                        return (T)context;
                    }
                }
            }
            throw MissingContextException(typeof(T));
        }
        
        internal static void SetMapperContext<T>(ref object[] contextMap, T mapperContext)  where T : class, IMapperContext
        {
            var index = MapperContextInfo<T>.Index;
            var map = contextMap; 
            if (map == null) {
                map = new object[index + 1];
            } else {
                if (map.Length <= index) {
                    map = new object[index + 1];
                    Array.Copy(contextMap, map, contextMap.Length);
                }
            }
            map[index] = mapperContext;
            contextMap = map;
        }
        
        private static InvalidOperationException MissingContextException(Type type) {
            return new InvalidOperationException($"Missing MapperContext: {type.Name}");
        }
    }
    
    public static class MapperContextInfo<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly int Index = MapperContext.GetNextIndex();
    }
}