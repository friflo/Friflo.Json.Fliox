using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Demo;

namespace DemoHub;

/// <summary>
/// This class does not deal with the ORM - the <see cref="DemoClient"/> - at all.
/// It illustrates how to separate the use of domain specific types from database access. <br/>   
/// <br/>   
/// <see cref="FakeUtils"/> is used to create random records in the <see cref="DemoClient"/> containers. <br/>
/// The records are generated with random data by using: <br/>
/// [bchavez/Bogus: A simple fake data generator for C#] https://github.com/bchavez/Bogus
/// </summary>
public class FakeUtils
{
    private long articleCounter  = 10;
    private long customerCounter = 10;
    private long employeeCounter = 10;
    private long orderCounter    = 10;
    private long producerCounter = 10;

    public FakeUtils() {
        Randomizer.Seed = new Random(1337);
    }
    
    public Records CreateFakes(Fake fake)
    {
        var result      = new Records();
        var articles    = fake?.articles    ?? 0;
        var customers   = fake?.customers   ?? 0;
        var employees   = fake?.employees   ?? 0;
        var orders      = fake?.orders      ?? 0; 
        var producers   = fake?.producers   ?? 0;
        
        // set default counts if all counts == 0
        if (articles == 0 && customers == 0 && employees == 0 && orders == 0 && producers == 0) {
            articles    = 2;
            customers   = 1;
            employees   = 1;
            orders      = 1;
            producers   = 1;
        }
        
        var now = DateTime.Now;
        
        if (employees > 0) {
            var faker = new Faker<Employee>()
                .RuleFor(e => e.id,         f => NewId(employeeCounter++, 3))
                .RuleFor(e => e.firstName,  f => f.Name.FirstName())
                .RuleFor(e => e.lastName,   f => f.Name.LastName())
                .RuleFor(e => e.created,    now);
            
            result.employees = new Employee[employees];
            for (int n = 0; n < employees; n++) {
                result.employees[n] = faker.Generate();
            }
        }
        if (producers > 0) {
            var faker = new Faker<Producer>()
                .RuleFor(p => p.id,         f => NewId(producerCounter++, 5))
                .RuleFor(p => p.name,       f => f.Company.CompanyName())
                .RuleFor(p => p.employees,  f => {
                    if (employees == 0)
                        return null;
                    return new List<long> { f.PickRandom(result.employees).id };
                })
                .RuleFor(e => e.created,    now);
            
            result.producers = new Producer[producers];
            for (int n = 0; n < producers; n++) {
                result.producers[n] = faker.Generate();
            }
        }
        if (articles > 0) {
            var faker = new Faker<Article>()
                .RuleFor(a => a.id,         f => NewId(articleCounter++, 1))
                .RuleFor(a => a.name,       f => f.Commerce.ProductName())
                .RuleFor(a => a.producer,   f => {
                    if (producers == 0)
                        return default;
                    return f.PickRandom(result.producers).id;
                })
                .RuleFor(e => e.created,    now);

            result.articles = new Article[articles];
            for (int n = 0; n < articles; n++) {
                result.articles[n] = faker.Generate();
            }
        }
        if (customers > 0) {
            var faker = new Faker<Customer>()
                .RuleFor(c => c.id,         f => NewId(customerCounter++, 2))
                .RuleFor(c => c.name,       f => f.Company.CompanyName())
                .RuleFor(e => e.created,    now);

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
                .RuleFor(o => o.id,         f => NewId(orderCounter++, 4))
                .RuleFor(o => o.created,    f => now)
                .RuleFor(o => o.customer,   f => {
                    if (customers == 0)
                        return default;
                    return f.PickRandom(result.customers).id;
                })
                .RuleFor(o => o.items,      articles == 0 ? null : itemFaker.Generate(2).ToList());
            
            result.orders = new Order[orders];
            for (int n = 0; n < orders; n++) {
                result.orders[n] = faker.Generate();
            }
        }
        
        var counts = new Counts {
            articles    = result.articles?  .Length ?? 0,
            customers   = result.customers? .Length ?? 0,
            employees   = result.employees? .Length ?? 0,
            orders      = result.orders?    .Length ?? 0,
            producers   = result.producers? .Length ?? 0,
        };
        var nowStr      = now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        result.info     = $"use container filter: o.created == '{nowStr}'";
        result.counts   = counts;
        return result;
    }

    private static long NewId(long localCounter, short type) {
        return localCounter * 10 + type;
    }
}
