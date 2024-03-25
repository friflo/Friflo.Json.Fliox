// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.DB.UserAuth;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event.Collector;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Schema.Language;
using Friflo.Json.Fliox.Schema.Native;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Collection;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema
{
    // [TestFixture, Ignore("WIP")]
    public static class CSharpOptimize
    {
        private static void Generate_CS_Optimize (Type type, string folder) {
            var typeSchema  = NativeTypeSchema.Create(type);
            var generator   = new Generator(typeSchema, ".cs");
            CSharpOptimizeGenerator.Generate(generator);
            generator.WriteFiles(folder, false);
        }
        
        /// C# -> Optimize - Assembly: Friflo.Json.Tests
        [Test]
        public static void CS_Optimize_JsonTests () {
            var folder = CommonUtils.GetBasePath() + "Gen";
            Generate_CS_Optimize(typeof(ListOneMember), folder);
            Generate_CS_Optimize(typeof(SimpleStore), folder);
            Generate_CS_Optimize(typeof(PocStore), folder);
        }
        
        private static void Generate_CS_Optimize_Library (Type type) {
            var folder = Path.GetFullPath(CommonUtils.GetBasePath() + "../Json/") + "Fliox.Hub/Gen";
            Generate_CS_Optimize(type, folder);
        }
        
        /// C# -> Optimize - Assembly: Friflo.Fliox.Hub
        [Test]
        public static void CS_Optimize_FlioxHub_Protocol () {
            Generate_CS_Optimize_Library(typeof(ProtocolMessage));
            Generate_CS_Optimize_Library(typeof(RemoteEventMessage));
            Generate_CS_Optimize_Library(typeof(RemoteSyncEvent));
            Generate_CS_Optimize_Library(typeof(RemoteMessageTask));
            Generate_CS_Optimize_Library(typeof(WriteTaskModel));
            Generate_CS_Optimize_Library(typeof(DeleteTaskModel));
            Generate_CS_Optimize_Library(typeof(RawEventMessage));
        }
        
        [Test]
        public static void CS_Optimize_FlioxHub_ClusterStore () {
            Generate_CS_Optimize_Library(typeof(ClusterStore));
        }
        
        [Test]
        public static void CS_Optimize_FlioxHub_MonitorStore () {
            Generate_CS_Optimize_Library(typeof(MonitorStore));
        }
        
        [Test]
        public static void CS_Optimize_FlioxHub_UserStore () {
            Generate_CS_Optimize_Library(typeof(UserStore));
        }
    }
}