using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global
namespace Fliox.DemoHub
{
    [Fri.MessagePrefix("demo.")]
    public partial class DemoStore {
        // --- commands
        /// <summary> generate random entities (records) in the containers listed in the <see cref="DemoHub.Fake"/> param </summary>
        public CommandTask<Records>     FakeRecords (Fake param)    => SendCommand<Fake, Records>   ("demo.FakeRecords", param);

        /// <summary> count records added to containers within the last param seconds. default 60</summary>
        public CommandTask<Counts>      CountLatest (int? param)    => SendCommand<int?, Counts>    ("demo.CountLatest", param);
        
        /// <summary> return records added to containers within the last param seconds. default 60</summary>
        public CommandTask<Records>     LatestRecords(int? param)   => SendCommand<int?, Records>   ("demo.LatestRecords", param);

        /// <summary> simple command adding two numbers - no database access. </summary>
        public CommandTask<double>      Add  (Operands  param)      => SendCommand<Operands, double>("demo.Add", param);
    }
    
    // ------------------------------ command models ------------------------------
    public class Operands {
        public  double      left;
        public  double      right;
    }
    
    public class Fake {
        /// <summary>if false generated entities are nor added to the <see cref="Records"/> result</summary>
        public  bool?       addResults;
        public  int?        articles;
        public  int?        customers;
        public  int?        employees;
        public  int?        orders;
        public  int?        producers;
    }
    
    public class Counts {
        public  int         articles;
        public  int         customers;
        public  int         employees;
        public  int         orders;
        public  int         producers;
    }
    
    public class Records {
        /// <summary>contains a filter that can be used to filter the generated entities in a container</summary>
        public  string      info;
        /// <summary>number of entities generated in each container</summary>
        public  Counts      counts;
        public  Article[]   articles;
        public  Customer[]  customers;
        public  Employee[]  employees;
        public  Order[]     orders;
        public  Producer[]  producers;
    }
}