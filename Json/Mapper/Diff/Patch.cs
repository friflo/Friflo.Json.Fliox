// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Mapper.Map.Val;

namespace Friflo.Json.Mapper.Diff
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
    public abstract class Patch
    {
    }

    public class PatchReplace : Patch
    {
        public string       path;
        public JsonValue    value;

        public override string ToString() => path;
    }
    
    public class PatchAdd : Patch
    {
        public string       path;
        public JsonValue    value;

        public override string ToString() => path;
    }
    
    public class PatchRemove : Patch
    {
        public string       path;

        public override string ToString() => path;
    }
    
    public class PatchCopy : Patch
    {
        public string       path;
        public string       from;

        public override string ToString() => path;
    }
    
    public class PatchMove : Patch
    {
        public string       path;
        public string       from;

        public override string ToString() => path;
    }
    
    public class PatchTest : Patch
    {
        public string       path;
        public JsonValue    value;

        public override string ToString() => path;
    }
}
