// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Friflo.Json.Fliox.Transform
{
    /// <summary>
    /// Implement models of RFC 6902<br/>
    /// <a href="https://tools.ietf.org/html/rfc6902">JavaScript Object Notation (JSON) Patch</a>
    /// </summary>
    [Discriminator("op", "patch type")]
    [PolymorphType(typeof(PatchReplace),    "replace")]
    [PolymorphType(typeof(PatchAdd),        "add")]
    [PolymorphType(typeof(PatchRemove),     "remove")]
    [PolymorphType(typeof(PatchCopy),       "copy")]
    [PolymorphType(typeof(PatchMove),       "move")]
    [PolymorphType(typeof(PatchTest),       "test")]
    public abstract class JsonPatch
    {
        public abstract PatchType PatchType { get; }
    }

    public sealed class PatchReplace : JsonPatch
    {
        [Required]  public  string      path;
        [Required]  public  JsonValue   value;

        public override     PatchType   PatchType   => PatchType.Replace;
        public override     string      ToString()  => path;
    }
    
    public sealed class PatchAdd : JsonPatch
    {
        [Required]  public  string      path;
        [Required]  public  JsonValue   value;

        public override     PatchType   PatchType   => PatchType.Add;
        public override     string      ToString()  => path;
    }
    
    public sealed class PatchRemove : JsonPatch
    {
        [Required]  public  string      path;

        public override     PatchType   PatchType   => PatchType.Remove;
        public override     string      ToString()  => path;
    }
    
    public sealed class PatchCopy : JsonPatch
    {
        [Required]  public  string      path;
                    public  string      from;

        public override     PatchType   PatchType   => PatchType.Copy;
        public override     string      ToString()  => path;
    }
    
    public sealed class PatchMove : JsonPatch
    {
        [Required]  public  string      path;
                    public  string      from;

        public override     PatchType   PatchType   => PatchType.Move;
        public override     string      ToString()  => path;

    }
    
    public sealed class PatchTest : JsonPatch
    {
        [Required]  public  string      path;
                    public  JsonValue   value;

        public override     PatchType   PatchType   => PatchType.Test;
        public override     string      ToString()  => path;
    }
    
    public enum PatchType
    {
        Replace = 1,
        Remove  = 2,
        Add     = 3,
        Copy    = 4,
        Move    = 5,
        Test    = 6,
    }
}
