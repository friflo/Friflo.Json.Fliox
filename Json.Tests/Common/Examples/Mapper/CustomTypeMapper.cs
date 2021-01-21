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
    
    public class StringTokenMapper : IJsonMapper
    {
        public static readonly StringTokenMapper Interface = new StringTokenMapper();
        
        public string DataTypeName() { return "tokens"; }
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(StringTokens))
                return null;
            return new PrimitiveType (typeof(StringTokens), Interface);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            StringTokens value = (StringTokens) slot.Obj;
            writer.bytes.AppendChar('\"');
            writer.bytes.AppendString(string.Join(" ", value.tokens));
            writer.bytes.AppendChar('\"');
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            if (reader.parser.Event == JsonEvent.ValueString) {
                string value =  reader.parser.value.ToString();
                if (value.Contains(","))
                    return ReadUtils.ErrorMsg(reader, "Invalid separator in token value", value);
                slot.Obj = new StringTokens { tokens = value.Split(' ')};
                return true;
            }
            return ValueUtils.CheckElse(reader, ref slot, stubType);
        }
    }
    

    public class CustomTypeMapper
    {
        [Test]
        public void Run() {
            var resolver = new DefaultTypeResolver();
            var mapperCount = resolver.mapperList.Count;
            resolver.AddSpecificTypeMapper(StringTokenMapper.Interface);
            AreEqual(mapperCount + 1, resolver.mapperList.Count);
            
            var typeStore = new TypeStore(resolver);
            string json = "\"Hello World\"";  // valid JSON :) - but unusual to use only a single value
            
            JsonReader reader = new JsonReader(typeStore);
            StringTokens result = reader.Read<StringTokens>(new Bytes(json));
            AreEqual(new [] {"Hello", "World"}, result.tokens);
            
            JsonWriter writer = new JsonWriter(typeStore);
            writer.Write(result);
            AreEqual(json, writer.bytes.ToString());
        }
    }
}

#endif
