using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    public interface IDatabaseResult
    {
        [Fri.Property(Name = "error")]
        DatabaseError      Error { get; set;  }
    }
    
    public class DatabaseError {
        
    }


    public class TaskError {
        
    }
}