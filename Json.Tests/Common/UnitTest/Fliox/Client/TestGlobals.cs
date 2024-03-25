// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Tests.Common.Utils;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public static class TestGlobals {
        
        public const string DB      = "main_db";
        public const string UserDB  = "user_db";
        public const string Monitor = "monitor";
            
        public static readonly string PocStoreFolder = CommonUtils.GetBasePath() + "assets~/DB/main_db";
            
        
        public static SharedEnv Shared { get; private set; }
        
        public static void Init() {
            SharedTypeStore.Init();
            Shared        = new SharedEnv("TestGlobals");
        }
        
        public static void Dispose() {
            Shared.Dispose();
            Shared = null;
            SharedTypeStore.Dispose();
        }
    }
}