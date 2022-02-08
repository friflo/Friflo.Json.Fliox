using System;
using Bogus;

namespace Friflo.Json.Fliox.DemoHub
{
    /// [bchavez/Bogus: A simple fake data generator for C#] https://github.com/bchavez/Bogus
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