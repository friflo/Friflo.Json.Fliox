// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Mapper.Patch
{
    /// <summary>
    /// Implement models of RFC 6902 - "JavaScript Object Notation (JSON) Patch"
    /// See: https://tools.ietf.org/html/rfc6902
    /// </summary>
    [Fri.Discriminator("op")]
    [Fri.Polymorph(typeof(PatchReplace),    Discriminant = "replace")]
    [Fri.Polymorph(typeof(PatchAdd),        Discriminant = "add")]
    [Fri.Polymorph(typeof(PatchRemove),     Discriminant = "remove")]
    [Fri.Polymorph(typeof(PatchCopy),       Discriminant = "copy")]
    [Fri.Polymorph(typeof(PatchMove),       Discriminant = "move")]
    [Fri.Polymorph(typeof(PatchTest),       Discriminant = "test")]
    public abstract class JsonPatch
    {
        public abstract PatchType PatchType { get; }
    }

    public class PatchReplace : JsonPatch
    {
        public string       path;
        public JsonValue    value;

        public override PatchType   PatchType   => PatchType.Replace;
        public override string      ToString()  => path;
    }
    
    public class PatchAdd : JsonPatch
    {
        public string       path;
        public JsonValue    value;

        public override PatchType   PatchType   => PatchType.Add;
        public override string      ToString()  => path;
    }
    
    public class PatchRemove : JsonPatch
    {
        public string       path;

        public override PatchType   PatchType   => PatchType.Remove;
        public override string      ToString()  => path;
    }
    
    public class PatchCopy : JsonPatch
    {
        public string       path;
        public string       from;

        public override PatchType   PatchType   => PatchType.Copy;
        public override string      ToString()  => path;
    }
    
    public class PatchMove : JsonPatch
    {
        public string       path;
        public string       from;

        public override PatchType   PatchType   => PatchType.Move;
        public override string      ToString()  => path;

    }
    
    public class PatchTest : JsonPatch
    {
        public string       path;
        public JsonValue    value;

        public override PatchType   PatchType   => PatchType.Test;
        public override string      ToString()  => path;
    }
    
    public enum PatchType
    {
        Replace,
        Remove,
        Add,
        Copy,
        Move,
        Test,
    }
}
