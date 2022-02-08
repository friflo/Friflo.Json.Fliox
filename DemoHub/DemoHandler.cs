using System;
using System.Threading.Tasks;
using Bogus;
using Friflo.Json.Fliox.Hub.Host;

// ReSharper disable UnusedMember.Local
namespace Friflo.Json.Fliox.DemoHub
{
    /// <summary>
    /// Implementation of custom database commands declared by <see cref="DemoStore"/>. <br/>
    /// By calling <see cref="TaskHandler.AddCommandHandlers"/> every method with a <see cref="Command{TParam}"/>
    /// parameter is added as a command handler. <br/>
    /// Their method names need to match the commands declared in <see cref="DemoStore"/>.
    /// </summary> 
    public class DemoHandler : TaskHandler
    {
        private static readonly FakeUtils FakeUtils = new FakeUtils();
        
        internal DemoHandler() {
            AddCommandHandlers();
        }
        
        /// synchronous command handler - preferred if possible
        private static double DemoAdd(Command<Operands> command) {
            var param = command.Param;
            return param.left + param.right;
        }
        
        /// asynchronous command handler
        private static Task<double> DemoMul(Command<Operands> command) {
            var param = command.Param;
            var result = param.left * param.right;
            return Task.FromResult(result);
        }

        /* intentionally not implemented to demonstrate response behavior
        private static double DemoSub_NotImpl(Command<Operands> command) {
            var param = command.Param;
            return param.left - param.right;
        } */
        
        private static async Task<FakeResult> DemoFake(Command<Fake> command) {
            var demoStore       = new DemoStore(command.Hub);
            var user            = command.User;
            demoStore.UserId    = user.userId.ToString(); // todo simplify setting user/token
            demoStore.Token     = user.token;
            demoStore.ClientId  = "DemoFake";
            // [bchavez/Bogus: A simple fake data generator for C#] https://github.com/bchavez/Bogus
            
            var result = FakeUtils.CreateFakes(command.Param);
            
            if (result.orders       != null)    demoStore.orders    .UpsertRange(result.orders);
            if (result.customers    != null)    demoStore.customers .UpsertRange(result.customers);
            if (result.articles     != null)    demoStore.articles  .UpsertRange(result.articles);
            if (result.producers    != null)    demoStore.producers .UpsertRange(result.producers);
            if (result.employees    != null)    demoStore.employees .UpsertRange(result.employees);
            
            await demoStore.SyncTasks();
            
            return result;
        }
    }

    internal class FakeUtils
    {
        private             int             articleCounter;
        private readonly    Faker<Article>  articleFaker;
        
        internal FakeUtils()
        {
            Randomizer.Seed = new Random(1337);
            
            articleFaker = new Faker<Article>()
                .RuleFor(a => a.id,     f => $"article-{articleCounter++}")
                .RuleFor(a => a.name,   f => f.Commerce.Product());
        }
        
        internal FakeResult CreateFakes(Fake fake) {
            var result = new FakeResult();
            
            if (fake.articles.HasValue) {
                result.articles = new Article[fake.articles.Value];
                for (int n = 0; n < fake.articles; n++) {
                    result.articles[n] = articleFaker.Generate();
                }
            }
            return result;
        }
    }
}