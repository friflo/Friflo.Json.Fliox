using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Friflo.Json.Fliox.Hub.Client;

namespace Friflo.Json.Fliox.DemoHub
{
    /// [bchavez/Bogus: A simple fake data generator for C#] https://github.com/bchavez/Bogus
    internal class FakeUtils
    {
        private int     employeeCounter = 10;
        private int     producerCounter = 10;
        private int     articleCounter  = 10;
        private int     orderCounter  = 10;
        
        internal FakeUtils() {
            Randomizer.Seed = new Random(1337);
        }
        
        internal FakeResult CreateFakes(Fake fake) {
            var result = new FakeResult();
            
            if (fake.employees.HasValue) {
                var faker = new Faker<Employee>()
                    .RuleFor(a => a.id,         f => $"employee-{employeeCounter++}")
                    .RuleFor(a => a.firstName,  f => f.Name.FirstName())
                    .RuleFor(a => a.lastName,   f => f.Name.LastName());
                
                result.employees = new Employee[fake.employees.Value];
                for (int n = 0; n < fake.employees; n++) {
                    result.employees[n] = faker.Generate();
                }
            }
            if (fake.producers.HasValue) {
                var faker = new Faker<Producer>()
                    .RuleFor(a => a.id,             f => $"producer-{producerCounter++}")
                    .RuleFor(a => a.name,           f => f.Company.CompanyName())
                    .RuleFor(a => a.employeeList,   f => new List<Ref<string, Employee>> { f.PickRandom(result.employees)} );
                
                result.producers = new Producer[fake.producers.Value];
                for (int n = 0; n < fake.producers; n++) {
                    result.producers[n] = faker.Generate();
                }
            }
            if (fake.articles.HasValue) {
                var faker = new Faker<Article>()
                    .RuleFor(a => a.id,         f => $"article-{articleCounter++}")
                    .RuleFor(a => a.name,       f => f.Commerce.Product())
                    .RuleFor(a => a.producer,   f => f.PickRandom(result.producers));

                result.articles = new Article[fake.articles.Value];
                for (int n = 0; n < fake.articles; n++) {
                    result.articles[n] = faker.Generate();
                }
            }
            if (fake.orders.HasValue) {
                var itemFaker = new Faker<OrderItem>();
                itemFaker.Rules((f, item) => {
                    var article     = f.PickRandom(result.articles);
                    item.article    = article.id;
                    item.name       = article.name;
                    item.amount     = f.Random.Number(1, 10);
                });
                var faker = new Faker<Order>()
                    .RuleFor(a => a.id,         f => $"order-{orderCounter++}")
                    .RuleFor(a => a.created,    f => f.Date.Past())
                    .RuleFor(a => a.items,      itemFaker.Generate(2).ToList());
                
                result.orders = new Order[fake.orders.Value];
                for (int n = 0; n < fake.orders; n++) {
                    result.orders[n] = faker.Generate();
                }
            }
            return result;
        }
    }
}