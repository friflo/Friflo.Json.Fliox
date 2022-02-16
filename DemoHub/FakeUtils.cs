#if !UNITY_2020_1_OR_NEWER

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
        private int fakeCounter = 1;

        
        internal FakeUtils() {
            Randomizer.Seed = new Random(1337);
        }
        
        internal FakeResult CreateFakes(Fake fake) {
            int employeeCounter = 1;
            int producerCounter = 1;
            int articleCounter  = 1;
            int customerCounter = 1;
            int orderCounter    = 1;
            fakeCounter++;
        
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
                    .RuleFor(e => e.id,         f => NewId(fakeCounter, employeeCounter++, 3))
                    .RuleFor(e => e.firstName,  f => f.Name.FirstName())
                    .RuleFor(e => e.lastName,   f => f.Name.LastName());
                
                result.employees = new Employee[employees];
                for (int n = 0; n < employees; n++) {
                    result.employees[n] = faker.Generate();
                }
            }
            if (producers > 0) {
                var faker = new Faker<Producer>()
                    .RuleFor(p => p.id,             f => NewId(fakeCounter, producerCounter++, 5))
                    .RuleFor(p => p.name,           f => f.Company.CompanyName())
                    .RuleFor(p => p.employeeList,   f => {
                        if (employees == 0)
                            return null;
                        return new List<Ref<Guid, Employee>> { f.PickRandom(result.employees) };
                    });
                
                result.producers = new Producer[producers];
                for (int n = 0; n < producers; n++) {
                    result.producers[n] = faker.Generate();
                }
            }
            if (articles > 0) {
                var faker = new Faker<Article>()
                    .RuleFor(a => a.id,         f => NewId(fakeCounter, articleCounter++, 1))
                    .RuleFor(a => a.name,       f => f.Commerce.ProductName())
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
                    .RuleFor(c => c.id,         f => NewId(fakeCounter, customerCounter++, 2))
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
                    .RuleFor(o => o.id,         f => NewId(fakeCounter, orderCounter++, 4))
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
            var fakePrefix  = fakeCounter.ToString("x8");
            result.info     = $"use container filter: o.id.StartsWith('{fakePrefix}-')";
            result.added    = added;
            return result;
        }

        private static bool UseRealGuids = false;

        private static Guid NewId(int fakeCounter, long localCounter, short type) {
            if (UseRealGuids)
                return Guid.NewGuid();
                
            byte[] bytes = BitConverter.GetBytes(localCounter);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            short b = (short)(type << 12);
            return new Guid(fakeCounter, b, 0, bytes);
        }
    }
}

#endif