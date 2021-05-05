using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    public interface ICommandResult
    {
        [Fri.Property(Name = "error")]
        DatabaseError      Error { get; set;  }
    }
    
    public class DatabaseError {
        
    }
}