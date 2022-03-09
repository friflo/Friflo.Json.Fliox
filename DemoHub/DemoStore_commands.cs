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
        public  int?        orders;
        public  int?        customers;
        public  int?        articles;
        public  int?        producers;
        public  int?        employees;
        public  bool?       addResults;
    }
    
    public class Counts {
        public  int         orders;
        public  int         customers;
        public  int         articles;
        public  int         producers;
        public  int         employees;
    }
    
    public class Records {
        public  string      info;
        public  Counts      counts;
        public  Order[]     orders;
        public  Customer[]  customers;
        public  Article[]   articles;
        public  Producer[]  producers;
        public  Employee[]  employees;
    }
}