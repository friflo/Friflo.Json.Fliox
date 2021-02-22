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
    
    public class StringTokenMapper : TypeMapper<StringTokens>
    {
        public StringTokenMapper(StoreConfig config) : base (config, true, false) { }

        public override void Write(ref Writer writer, StringTokens value) {
            writer.WriteString(string.Join(" ", value.tokens));
        }

        public override StringTokens Read(ref Reader reader, StringTokens slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueString)
                return ValueUtils.CheckElse(ref reader, this, out success);    
            string value =  reader.parser.value.ToString();
            if (value.Contains(","))
                return reader.ErrorMsg<StringTokens>("Invalid separator in token value", value, out success);
            success = true;
            return new StringTokens { tokens = value.Split(' ')};
        }
    }

    public class CustomTypeMapper
    {
        [Test]
        public void Run() {
            var typeStore = new TypeStore();
            typeStore.typeResolver.AddConcreteTypeMapper(new StringTokenMapper(typeStore.config));
            
            string json = "\"Hello World 🌎\"";  // valid JSON :) - but unusual to use only a single value
            
            var mapper = new JsonMapper(typeStore);
            StringTokens result = mapper.Read<StringTokens>(new Bytes(json));
            AreEqual(new [] {"Hello", "World", "🌎"}, result.tokens);
            
            var jsonResult = mapper.Write(result);
            AreEqual(json, jsonResult);
        }
    }
}

#endif
