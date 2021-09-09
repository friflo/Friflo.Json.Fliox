// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable InconsistentNaming
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Graph
{
    public class EntityIdStore : EntityStore {
        public  EntitySet <Guid,    GuidEntity>      guidEntities       { get; private set; }
        public  EntitySet <int,     IntEntity>       intEntities        { get; private set; }
        public  EntitySet <long,    LongEntity>      longEntities       { get; private set; }
        public  EntitySet <short,   ShortEntity>     shortEntities      { get; private set; }
        public  EntitySet <byte,    ByteEntity>      byteEntities       { get; private set; }
        public  EntitySet <string,  CustomIdEntity>  customIdEntities   { get; private set; }
        public  EntitySet <string,  EntityRefs>      entityRefs         { get; private set; }
        public  EntitySet <string,  CustomIdEntity2> customIdEntities2  { get; private set; }

        public EntityIdStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {}
    }

    public class GuidEntity {
        public Guid id;
    }

    public class IntEntity {
        public int  id;
    }
    
    public class LongEntity {
        public long Id { get; set; }
    }
    
    public class ShortEntity {
        public short id;
    }
    
    public class ByteEntity {
        public byte id;
    }
    
    public class CustomIdEntity {
        [Fri.Key]
        [Fri.Required]  public string customId;
    }
    
    public class EntityRefs {
        [Fri.Required]  public string                            id;
                        public      Ref <Guid,   GuidEntity>     guidEntity;
                        public      Ref <Guid?,  GuidEntity>     guidNullEntity;
                        public      Ref <int,    IntEntity>      intEntity;
                        public      Ref <int?,   IntEntity>      intNullEntity;
                        public      Ref <int?,   IntEntity>      intNullEntity2;
                        public      Ref <long,   LongEntity>     longEntity;
                        public      Ref <short,  ShortEntity>    shortEntity;
                        public      Ref <byte,   ByteEntity>     byteEntity;
                        public      Ref <string, CustomIdEntity> customIdEntity;
                        public List<Ref <int,    IntEntity>>     intEntities;
                        // nullable array elements are supported, but bot recommended.
                        // It forces the application for null checks, which can simply omitted by not using nullable elements.
                        public List<Ref <int?,   IntEntity>>     intNullEntities;
    }

    public class CustomIdEntity2 {
#if UNITY_5_3_OR_NEWER
        [Fri.Key] [Fri.Required]
#else
        // Apply [Key]      alternatively by System.ComponentModel.DataAnnotations.KeyAttribute
        // Apply [Required] alternatively by System.ComponentModel.DataAnnotations.RequiredAttribute
        [Key] [Required]
#endif
        public string customId2;
    }

}