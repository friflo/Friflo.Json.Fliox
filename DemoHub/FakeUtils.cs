using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Friflo.Json.Fliox.Hub.Client;

namespace Friflo.Json.Fliox.DemoHub
{
    /// <summary>
    /// <see cref="FakeUtils"/> is used to create random records in the <see cref="DemoStore"/> containers. <br/>
    /// The records are generated with random data by using: <br/>
    /// [bchavez/Bogus: A simple fake data generator for C#] https://github.com/bchavez/Bogus
    /// </summary>
    internal class FakeUtils
    {
        private int     employeeCounter = 10;
        private int     producerCounter = 10;
        private int     articleCounter  = 10;
        private int     customerCounter = 10;
        private int     orderCounter    = 10;
        
        internal FakeUtils() {
            Randomizer.Seed = new Random(1337);
        }
        
        internal FakeResult CreateFakes(Fake fake) {
            var result = new FakeResult();
            var orders      = fake.orders       ?? 0; 
            var articles    = fake.articles     ?? 0;
            var producers   = fake.producers    ?? 0;
            var customers   = fake.customers    ?? 0;
            var employees   = fake.employees    ?? 0;
            
            // set default counts if all counts == 0
            if (orders == 0 && articles == 0 && producers == 0 && customers == 0 && employees == 0) {
                orders      = 1;
                articles    = 2;
                producers   = 1;
                customers   = 1;
                employees   = 1;
            }
            
            if (employees > 0) {
                var faker = new Faker<Employee>()
                    .RuleFor(e => e.id,         f => $"employee-{employeeCounter++}")
                    .RuleFor(e => e.firstName,  f => f.Name.FirstName())
                    .RuleFor(e => e.lastName,   f => f.Name.LastName());
                
                result.employees = new Employee[employees];
                for (int n = 0; n < employees; n++) {
                    result.employees[n] = faker.Generate();
                }
            }
            if (producers > 0) {
                var faker = new Faker<Producer>()
                    .RuleFor(p => p.id,             f => $"producer-{producerCounter++}")
                    .RuleFor(p => p.name,           f => f.Company.CompanyName())
                    .RuleFor(p => p.employeeList,   f => {
                        if (employees == 0)
                            return null;
                        return new List<Ref<string, Employee>> { f.PickRandom(result.employees) };
                    });
                
                result.producers = new Producer[producers];
                for (int n = 0; n < producers; n++) {
                    result.producers[n] = faker.Generate();
                }
            }
            if (articles > 0) {
                var faker = new Faker<Article>()
                    .RuleFor(a => a.id,         f => $"article-{articleCounter++}")
                    .RuleFor(a => a.name,       f => f.Commerce.Product())
                    .RuleFor(a => a.producer,   f => {
                        if (producers == 0)
                            return default;
                        return f.PickRandom(result.producers);
                    });

                result.articles = new Article[articles];
                for (int n = 0; n < articles; n++) {
                    result.articles[n] = faker.Generate();
                }
            }
            if (customers > 0) {
                var faker = new Faker<Customer>()
                    .RuleFor(c => c.id,         f => $"customer-{customerCounter++}")
                    .RuleFor(c => c.name,       f => f.Company.CompanyName());

                result.customers = new Customer[customers];
                for (int n = 0; n < customers; n++) {
                    result.customers[n] = faker.Generate();
                }
            }
            if (orders > 0) {
                var itemFaker = new Faker<OrderItem>();
                itemFaker.Rules((f, item) => {
                    var article     = f.PickRandom(result.articles);
                    item.article    = article.id;
                    item.name       = article.name;
                    item.amount     = f.Random.Number(1, 10);
                });
                var faker = new Faker<Order>()
                    .RuleFor(o => o.id,         f => $"order-{orderCounter++}")
                    .RuleFor(o => o.created,    f => f.Date.Past())
                    .RuleFor(o => o.customer,   f => {
                        if (customers == 0)
                            return default;
                        return f.PickRandom(result.customers);
                    })
                    .RuleFor(o => o.items,      articles == 0 ? null : itemFaker.Generate(2).ToList());
                
                result.orders = new Order[orders];
                for (int n = 0; n < orders; n++) {
                    result.orders[n] = faker.Generate();
                }
            }
            
            var added = new Fake {
                orders      = result.orders?    .Length,
                customers   = result.customers? .Length,
                articles    = result.articles?  .Length,
                producers   = result.producers? .Length,
                employees   = result.employees? .Length,
            };
            result.added = added;
            return result;
        }
    }
}