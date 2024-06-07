using Friflo.Json.Burst;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Schema.Definition;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Mapper
{
    [Json.Fliox.TypeMapper(typeof(EntityLinkMapper))]
    public readonly struct EntityLink
    {
        public readonly int Id;
        
        internal EntityLink(int id) {
            Id = id;
        }
    }

    public class EntityLinkMapper : TypeMapper<EntityLink>
    {
        public EntityLinkMapper() : base (null, typeof(EntityLink), false, true) { }
        
        public override bool    IsNull(ref EntityLink value)  => value.Id == 0;
        
        public override void Write(ref Writer writer, EntityLink value) {
            writer.format.AppendInt(ref writer.bytes, value.Id);
        }

        public override EntityLink Read(ref Reader reader, EntityLink slot, out bool success) {
            if (reader.parser.Event != JsonEvent.ValueNumber) {
                return reader.HandleEvent(this, out success);
            }
            int id = reader.parser.ValueAsInt(out success);
            if (success) {
                return new EntityLink(id);
            }
            return reader.ErrorMsg<EntityLink>("Invalid entity id", reader.parser.value, out success);
        }
    }
    
    public static class TestCustomMapper
    {
        [Test]
        public static void CustomEntityLinkMapper() {
            var typeStore   = new TypeStore();
            var mapper      = new ObjectMapper(typeStore);
            var link        = new UseEntityLink { link = new EntityLink(42) };
            
            var json = mapper.Write(link);
            Assert.AreEqual("{\"link\":42}", json);
            
            var result = mapper.Read<UseEntityLink>(json);
            Assert.AreEqual(42, result.link.Id);
            
            var useLinkMapper   = typeStore.GetTypeMapper<UseEntityLink>();
            var linkMember      = useLinkMapper.GetMember("link");
            Var linkVar         = linkMember.GetVar(link);
            var linkResult      = (EntityLink)linkVar.Object;
            Assert.AreEqual(42, linkResult.Id);
            
            var newValue    = new Var(new EntityLink(43));
            object linkObj  = link; 
            linkMember.SetVar(linkObj, newValue);
            var objResult   = (UseEntityLink)linkObj;
            Assert.AreEqual(43, objResult.link.Id);
        }
    }
    
    public struct UseEntityLink
    {
        public EntityLink link;
    }
}