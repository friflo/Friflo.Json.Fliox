using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Mapper;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;

using static NUnit.Framework.Assert;
// using static Friflo.Json.Tests.Common.UnitTest.NoCheck;

namespace Friflo.Json.Tests.Perf.Mapper
{
    public class PerfMapper: LeakTestsFixture
    {
        private BookShelf   bookShelf;
        private Bytes       bookShelfJson;

        [TearDown]
        public new void TearDown() {
            bookShelfJson.Dispose();
        }

        private BookShelf CreateBookShelf() {
            if (bookShelf != null)
                return bookShelf;
            bookShelf = new BookShelf { Books = new List<Book>() };
            for (int n = 0; n < 1_000_000; n++) {
                var book = new Book {Id = n, Title = $"Book {n}", BookData = new byte[0]};
                bookShelf.Books.Add(book);
            }
            return bookShelf;
        }
        
        private Bytes CreateBookShelfJson() {
            if (bookShelfJson.buffer.IsCreated())
                return bookShelfJson;
            var shelf = CreateBookShelf();
            using (var typeStore = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
            using (var writer = new JsonWriter(typeStore)) {
                writer.Write(shelf);
                return bookShelfJson = writer.Output.SwapWithDefault();
            }
        }

        
        [Test]
        public void TestWriteBookShelf() {
            BookShelf shelf = CreateBookShelf();
            using (var typeStore = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
            using (var writer = new JsonWriter(typeStore))
            {
                for (int n = 0; n < 20; n++) {
                    int start = TimeUtil.GetMs();
                    writer.Write(shelf);
                    int end = TimeUtil.GetMs();
                    Console.WriteLine(end - start);
                }
            }
        }
        
        [Test]
        public void TestParseBookShelf() {
            Bytes json = CreateBookShelfJson();
            for (int n = 0; n < 10; n++) {
                int start = TimeUtil.GetMs();
                using (var parser = new JsonParser()) {
                    parser.InitParser(json);
                    while (parser.NextEvent() != JsonEvent.EOF) {
                        if (parser.error.ErrSet)
                            Fail(parser.error.msg.ToString());
                    }
                    IsTrue(parser.ProcessedBytes > 49_000_000);
                }
                int end = TimeUtil.GetMs();
                Console.WriteLine(end - start);
            }
        }
        
        [Test]
        public void TestReadToBookShelf() {
            var shelf = CreateBookShelf();
            Bytes json = CreateBookShelfJson();
            for (int n = 0; n < 4; n++) {
                int start = TimeUtil.GetMs();
                using (var typeStore = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
                using (var reader = new JsonReader(typeStore))
                {
                    reader.ReadTo(json, shelf, out bool success);
                    IsTrue(success);
                }
                int end = TimeUtil.GetMs();
                Console.WriteLine(end - start);
            }
        }
    }
    
    
    public class BookShelf
    {
        public List<Book> Books { get; set; }


        public BookShelf() // Parameterless ctor is needed for every protocol buffer class during deserialization
        { }
    }
    
    public class Book {
        public string   Title;
        public int      Id;
        public byte[]   BookData;
    }
}
