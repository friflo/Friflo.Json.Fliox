using Friflo.Json.Fliox.Hub.Client;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnassignedField.Global
namespace Friflo.Json.Fliox.DemoHub
{
    public partial class DemoStore
    {
        // --- commands
        /// <summary> Generate random entities (records) in the containers listed in the <see cref="DemoHub.Fake"/> param </summary> 
        public CommandTask<FakeResult>Fake (Fake        param)      => SendCommand<Fake, FakeResult>(nameof(Fake), param);
        
        /// <summary> simple command adding two numbers - no database access. </summary>
        public CommandTask<double>    Add  (Operands    param)      => SendCommand<Operands, double>(nameof(Add),  param);
        
        /// <summary> simple command multiplying two numbers - no database access. </summary>
        public CommandTask<double>    Mul  (Operands    param)      => SendCommand<Operands, double>(nameof(Mul),  param);
        
        /// <summary> command handler for <see cref="Sub_NotImpl"/> intentionally not implemented by <see cref="DemoHandler"/>. 
        /// Execution results in:
        /// <code>NotImplemented > no command handler for: 'demo.Sub_NotImpl' </code></summary>
        public CommandTask<double>    Sub_NotImpl (Operands param)  => SendCommand<Operands, double>(nameof(Sub_NotImpl), param);
    }
    
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
    }
    
    public class FakeResult {
        public  string      info;
        public  Fake        added;
        public  Order[]     orders;
        public  Customer[]  customers;
        public  Article[]   articles;
        public  Producer[]  producers;
        public  Employee[]  employees;
    }
}