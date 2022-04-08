// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Schema.Language;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema
{
    public static class ClusterStoreGen
    {
        // -------------------------------------- input: C# --------------------------------------
        
        /// C# -> Typescript
        [Test]
        public static void CS_Typescript () {
            var options     = new NativeTypeOptions(typeof(ClusterStore));
            var generator   = TypescriptGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Typescript/ClusterStore");
        }
        
        /// C# -> GraphQL
        [Test]
        public static void CS_GraphQL () {
            var typeSchema  = new NativeTypeSchema(typeof(ClusterStore));
            var generator   = new Generator(typeSchema, ".graphql");
            GraphQLGenerator.Generate(generator);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/GraphQL/ClusterStore");
        }
    }
}