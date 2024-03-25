// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    // ---------------------------------- entity models ----------------------------------
    public class GuidEntity {
        public Guid id;
    }

    public class IntEntity {
        public int  id;
    }
    
    public class AutoIntEntity {
        [AutoIncrement]
        public int  id;
    }
    
    [NamingPolicy(NamingPolicyType.Default)]
    public class LongEntity {
        [Key]
        public long Id { get; set; }
    }
    
    public class ShortEntity {
        public short id;
    }
    
    public class ByteEntity {
        public byte id;
    }
    
    public class CustomIdEntity {
        [Key]
        [Required]  public string customId;
    }
    
    public class EntityRefs {
        [Required]
                                                            public  string      id;
        [Relation(nameof(EntityIdStore.guidEntities))]      public  Guid        guidEntity;
        [Relation(nameof(EntityIdStore.guidEntities))]      public  Guid?       guidNullEntity;
        [Relation(nameof(EntityIdStore.intEntities))]       public  int         intEntity;
        [Relation(nameof(EntityIdStore.intEntities))]       public  int?        intNullEntity;
        [Relation(nameof(EntityIdStore.intEntities))]       public  int?        intNullEntity2;
        [Relation(nameof(EntityIdStore.longEntities))]      public  long        longEntity;
        [Relation(nameof(EntityIdStore.longEntities))]      public  long?       longNullEntity;
        [Relation(nameof(EntityIdStore.shortEntities))]     public  short       shortEntity;
        [Relation(nameof(EntityIdStore.shortEntities))]     public  short?      shortNullEntity;
        [Relation(nameof(EntityIdStore.byteEntities))]      public  byte        byteEntity;
        [Relation(nameof(EntityIdStore.byteEntities))]      public  byte?       byteNullEntity;
        [Relation(nameof(EntityIdStore.customIdEntities))]  public  string      customIdEntity;
        [Relation(nameof(EntityIdStore.intEntities))]       public  List<int>   intEntities;
        // arrays with nullable references are supported, but bot recommended. It forces the application
        // for null checks, which can simply omitted by not using an array with nullable references.
        [Relation(nameof(EntityIdStore.intEntities))]       public  List<int?>  intNullEntities;
    }

    public class CustomIdEntity2 {
#if UNITY_5_3_OR_NEWER
        [Key] [Required]
#else
        // Apply [Key]      alternatively by System.ComponentModel.DataAnnotations.KeyAttribute
        // Apply [Required] alternatively by System.ComponentModel.DataAnnotations.RequiredAttribute
        [Key] [Required]
#endif
        public string customId2;
    }
}