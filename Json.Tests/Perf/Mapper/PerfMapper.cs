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
        [Test]
        public void TestBookShelf() {
            Fail("check");

            BookShelf bookShelf = new BookShelf();
            bookShelf.Books = new List<Book>();
            for (int n = 0; n < 1_00_000; n++) {
                var book = new Book();
                book.Id = n;
                book.Title = $"Book {n}";
                book.BookData = new byte[0];
                bookShelf.Books.Add(book);
            }

            using (var typeStore = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
            using (var writer = new JsonWriter(typeStore))
            {
                writer.Write(bookShelf);
                for (int n = 0; n < 10; n++) {
                    int start = TimeUtil.GetMs();
                    writer.Write(bookShelf);
                    /* using (var reader = new JsonReader(typeStore))
                    using (var parser = new JsonParser()) {
                        // parser.InitParser(writer.bytes);
                        // while (parser.NextEvent() != JsonEvent.EOF) { }

                        // reader.Read<BookShelf>(writer.bytes);
                    } */
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
