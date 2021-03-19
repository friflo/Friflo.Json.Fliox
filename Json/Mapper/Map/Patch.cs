
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
        public string path;

        public override string ToString() => path;
    }

    public class PatchReplace : Patch
    {
        public object value;
    }
    
    public class PatchAdd : Patch
    {
        public object value;
    }
    
    public class PatchRemove : Patch
    {
    }
    
    public class PatchCopy : Patch
    {
        public string from;
    }
    
    public class PatchMove : Patch
    {
        public string from;
    }
    
    public class PatchTest : Patch
    {
        public object value;
    }


}