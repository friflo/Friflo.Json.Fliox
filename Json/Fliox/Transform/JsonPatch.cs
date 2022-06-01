// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Req = Friflo.Json.Fliox.RequiredMemberAttribute;

namespace Friflo.Json.Fliox.Transform
{
    /// <summary>
    /// Implement models of RFC 6902<br/>
    /// <a href="https://tools.ietf.org/html/rfc6902">JavaScript Object Notation (JSON) Patch</a>
    /// </summary>
    [Discriminator("op", Description = "patch type")]
    [Polymorph(typeof(PatchReplace),    Discriminant = "replace")]
    [Polymorph(typeof(PatchAdd),        Discriminant = "add")]
    [Polymorph(typeof(PatchRemove),     Discriminant = "remove")]
    [Polymorph(typeof(PatchCopy),       Discriminant = "copy")]
    [Polymorph(typeof(PatchMove),       Discriminant = "move")]
    [Polymorph(typeof(PatchTest),       Discriminant = "test")]
    public abstract class JsonPatch
    {
        public abstract PatchType PatchType { get; }
    }

    public sealed class PatchReplace : JsonPatch
    {
        [Req]  public  string      path;
        [Req]  public  JsonValue   value;

        public override         PatchType   PatchType   => PatchType.Replace;
        public override         string      ToString()  => path;
    }
    
    public sealed class PatchAdd : JsonPatch
    {
        [Req]  public  string      path;
        [Req]  public  JsonValue   value;

        public override         PatchType   PatchType   => PatchType.Add;
        public override         string      ToString()  => path;
    }
    
    public sealed class PatchRemove : JsonPatch
    {
        [Req]  public  string      path;

        public override         PatchType   PatchType   => PatchType.Remove;
        public override         string      ToString()  => path;
    }
    
    public sealed class PatchCopy : JsonPatch
    {
        [Req]           public string       path;
                        public string       from;

        public override PatchType   PatchType   => PatchType.Copy;
        public override string      ToString()  => path;
    }
    
    public sealed class PatchMove : JsonPatch
    {
        [Req]           public  string      path;
                        public  string      from;

        public override         PatchType   PatchType   => PatchType.Move;
        public override         string      ToString()  => path;

    }
    
    public sealed class PatchTest : JsonPatch
    {
        [Req]           public  string      path;
                        public  JsonValue   value;

        public override         PatchType   PatchType   => PatchType.Test;
        public override         string      ToString()  => path;
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
