using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

#if !UNITY_5_3_OR_NEWER  // no clean up of native containers for Unity/JSON_BURST

namespace Friflo.Json.Tests.Common.Examples.Mapper
{
    // Custom type as an example to split tokens in a JSON value like "Hello World" into a string[]
    public class StringTokens {
        public string[] tokens;
    }
    
    public class StringTokenMatcher : ITypeMatcher {
        public static readonly StringTokenMatcher Instance = new StringTokenMatcher();
        
                
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (type != typeof(StringTokens))
                return null;
            return new StringTokenMapper (type);
        }
    }
    
    public class StringTokenMapper : TypeMapper<StringTokens>
    {
        public override string DataTypeName() { return "tokens"; }
        
        public StringTokenMapper(Type type) : base (type, true, false) { }

        public override void Write(JsonWriter writer, StringTokens value) {
            WriteUtils.WriteString(writer, string.Join(" ", value.tokens));
        }

        public override StringTokens Read(JsonReader reader, StringTokens slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueString)
                return ValueUtils.CheckElse(reader, this, out success);    
            string value =  reader.parser.value.ToString();
            if (value.Contains(","))
                return ReadUtils.ErrorMsg<StringTokens>(reader, "Invalid separator in token value", value, out success);
            success = true;
            return new StringTokens { tokens = value.Split(' ')};
        }
    }

    public class CustomTypeMapper
    {
        [Test]
        public void Run() {
            var resolver = new DefaultTypeResolver();
            var mapperCount = resolver.matcherList.Count;
            resolver.AddSpecificTypeMapper(StringTokenMatcher.Instance);
            AreEqual(mapperCount + 1, resolver.matcherList.Count);
            
            var typeStore = new TypeStore(resolver);
            string json = "\"Hello World 🌎\"";  // valid JSON :) - but unusual to use only a single value
            
            JsonReader reader = new JsonReader(typeStore);
            StringTokens result = reader.Read<StringTokens>(new Bytes(json));
            AreEqual(new [] {"Hello", "World", "🌎"}, result.tokens);
            
            JsonWriter writer = new JsonWriter(typeStore);
            writer.Write(result);
            AreEqual(json, writer.bytes.ToString());
        }
    }
}

#endif
