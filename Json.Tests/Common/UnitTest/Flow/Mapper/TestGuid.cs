// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Mapper
{
    public class TestGuid  : LeakTestsFixture
    {
        [Test]
        public void MapGuid() {
            using (var typeStore   = new TypeStore())
            using (var mapper      = new ObjectMapper(typeStore))
            {
                var writer      = mapper.writer;
                writer.Pretty   = true;
                var reader      = mapper.reader;
                const string id = "87db6552-a99d-4d53-9b20-8cc797db2b8f";
                var idJson      = $"\"{id}\"";
                
                var guid = new Guid(id);
                var json = writer.Write(guid);
                AreEqual(idJson, json);
                
                var idResult = reader.Read<Guid>(idJson);
                AreEqual(guid, idResult);
                
                var useGuid = new UseGuid { guid = guid, guidNull = guid };
                json = writer.Write(useGuid);
                AreEqual(@"{
    ""guid"": ""87db6552-a99d-4d53-9b20-8cc797db2b8f"",
    ""guidNull"": ""87db6552-a99d-4d53-9b20-8cc797db2b8f""
}", json);
            }
        }
    }
    
    public class UseGuid {
        public  Guid    guid;
        public  Guid?   guidNull;
    }
}