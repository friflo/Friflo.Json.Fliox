// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable InconsistentNaming
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class EntityIdStore : FlioxClient {
        // --- containers
        public  EntitySet <Guid,    GuidEntity>      guidEntities       { get; private set; }
        public  EntitySet <int,     IntEntity>       intEntities        { get; private set; }
        public  EntitySet <int,     AutoIntEntity>   intEntitiesAuto    { get; private set; }
        public  EntitySet <long,    LongEntity>      longEntities       { get; private set; }
        public  EntitySet <short,   ShortEntity>     shortEntities      { get; private set; }
        public  EntitySet <byte,    ByteEntity>      byteEntities       { get; private set; }
        public  EntitySet <string,  CustomIdEntity>  customIdEntities   { get; private set; }
        public  EntitySet <string,  EntityRefs>      entityRefs         { get; private set; }
        public  EntitySet <string,  CustomIdEntity2> customIdEntities2  { get; private set; }

        public EntityIdStore(FlioxHub hub) : base(hub) { }
    }
}