using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Types;
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
        
                
        public StubType CreateStubType(Type type) {
            if (type != typeof(StringTokens))
                return null;
            return new PrimitiveType (typeof(StringTokens), StringTokenMapper.Interface);
        }
    }
    
    public class StringTokenMapper : TypeMapper
    {
        public static readonly StringTokenMapper Interface = new StringTokenMapper();
        
        public override string DataTypeName() { return "tokens"; }

        
        public override void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            StringTokens value = (StringTokens) slot.Obj;
            WriteUtils.WriteString(writer, string.Join(" ", value.tokens));
        }

        public override bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event != JsonEvent.ValueString)
                return ValueUtils.CheckElse(reader, ref slot, stubType);    
            string value =  reader.parser.value.ToString();
            if (value.Contains(","))
                return ReadUtils.ErrorMsg(reader, "Invalid separator in token value", value);
            slot.Obj = new StringTokens { tokens = value.Split(' ')};
            return true;
        }
    }

    public class CustomTypeMapper
    {
        [Test]
        public void Run() {
            var resolver = new DefaultTypeResolver();
            var mapperCount = resolver.mapperList.Count;
            resolver.AddSpecificTypeMapper(StringTokenMatcher.Instance);
            AreEqual(mapperCount + 1, resolver.mapperList.Count);
            
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
