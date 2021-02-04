using System;
using System.Collections.Generic;
using Friflo.Json.Mapper;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

using static NUnit.Framework.Assert;
// using static Friflo.Json.Tests.Common.UnitTest.NoCheck;

namespace Friflo.Json.Tests.Perf.Mapper
{
    public class PerfMapper
    {
        private BookShelf createBookShelf(int count) {
            BookShelf bookShelf = new BookShelf();
            bookShelf.Books = new List<Book>();
            for (int n = 0; n < count; n++) {
                var book = new Book {Id = n, Title = $"Book {n}", BookData = new byte[0]};
                bookShelf.Books.Add(book);
            }
            return bookShelf;
        }

        
        [Test]
        public void TestWriteBookShelf() {
            BookShelf bookShelf = createBookShelf(1_000_000);

            using (var typeStore = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
            using (var writer = new JsonWriter(typeStore))
            {
                for (int n = 0; n < 20; n++) {
                    int start = TimeUtil.GetMs();
                    writer.Write(bookShelf);
                    int end = TimeUtil.GetMs();
                    Console.WriteLine(end - start);
                }
            }
        }
        
        [Test]
        public void TestReadBookShelf() {
            BookShelf bookShelf = createBookShelf(1_000_000);

            using (var typeStore = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
            using (var writer = new JsonWriter(typeStore))
            {
                writer.Write(bookShelf);
                for (int n = 0; n < 5; n++) {
                    int start = TimeUtil.GetMs();
                    writer.Write(bookShelf);
                    using (var reader = new JsonReader(typeStore))
                    // using (var parser = new JsonParser())
                    {
                        // parser.InitParser(writer.bytes);
                        // while (parser.NextEvent() != JsonEvent.EOF) { }
                        reader.Read<BookShelf>(writer.bytes);
                    }
                    int end = TimeUtil.GetMs();
                    Console.WriteLine(end - start);
                }
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
