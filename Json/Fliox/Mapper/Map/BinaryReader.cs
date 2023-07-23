namespace Friflo.Json.Fliox.Mapper.Map
{
    public abstract class BinaryReader
    {
        protected   int             currentOrdinal;
        
        public abstract Var     GetVar          (PropField field);
        public abstract bool    HasObject       (TypeMapper mapper);
    }
}