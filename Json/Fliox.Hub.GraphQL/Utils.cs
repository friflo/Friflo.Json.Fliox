// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class Utils
    {
        internal static JsonValue CreateSchemaResponse(Pool pool, string schemaJson) {
            var response = new GqlResponse {
                data = new GqlData {
                    schema = new JsonValue(schemaJson)
                }
            };
            using (var pooled = pool.ObjectMapper.Get()) {
                var writer              = pooled.instance.writer;
                writer.Pretty           = true;
                writer.WriteNullMembers = false;
                return new JsonValue(writer.WriteAsArray(response));
            }
        }
    }
}