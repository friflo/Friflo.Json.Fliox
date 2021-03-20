// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Mapper.Map.Val;

namespace Friflo.Json.Mapper.Map
{
    [Fri.Discriminator("op")]
    [Fri.Polymorph(typeof(PatchReplace),    Discriminant = "replace")]
    [Fri.Polymorph(typeof(PatchAdd),        Discriminant = "add")]
    [Fri.Polymorph(typeof(PatchRemove),     Discriminant = "remove")]
    [Fri.Polymorph(typeof(PatchCopy),       Discriminant = "copy")]
    [Fri.Polymorph(typeof(PatchMove),       Discriminant = "move")]
    [Fri.Polymorph(typeof(PatchTest),       Discriminant = "test")]
    public abstract class Patch
    {
        [Fri.Ignore]
        public abstract string Path { get;  }

        public override string ToString() => Path;
    }

    public class PatchReplace : Patch
    {
        [Fri.Ignore]
        public override string Path => path;
        
        public string       path;
        public JsonValue    value;
    }
    
    public class PatchAdd : Patch
    {
        [Fri.Ignore]
        public override string Path => path;
        
        public string       path;
        public JsonValue    value;
    }
    
    public class PatchRemove : Patch
    {
        [Fri.Ignore]
        public override string Path => path;
        
        public string       path;
    }
    
    public class PatchCopy : Patch
    {
        [Fri.Ignore]
        public override string Path => path;

        public string       path;
        public string       from;
    }
    
    public class PatchMove : Patch
    {
        [Fri.Ignore]
        public override string Path => path;

        public string       path;
        public string       from;
    }
    
    public class PatchTest : Patch
    {
        [Fri.Ignore]
        public override string Path => path;

        public string       path;
        public JsonValue    value;
    }
}
