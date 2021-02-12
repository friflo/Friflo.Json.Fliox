using System;
using System.Collections.Generic;
using System.IO;
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
                var book = new Book {
                    Id = n,
                    Title = $"Book {n}",
                    // Title = null,
                    BookData = new byte[0],
                    // BookData = null,
                };
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
                writer.Write(shelf, ref bookShelfJson);
                return bookShelfJson;
            }
        }

        
        [Test]
        public void TestWrite() {
            BookShelf shelf = CreateBookShelf();
            using (var      typeStore   = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
            using (var      writer      = new JsonWriter(typeStore))
            using (var      dst         = new TestBytes())
            {
                for (int n = 0; n < 10; n++) {
                    int start = TimeUtil.GetMs();
                    writer.Write(shelf, ref dst.bytes);
                    int end = TimeUtil.GetMs();
                    Console.WriteLine(end - start);
                }
            }
        }
        
        [Test]
        public void TestParse() {
            Bytes json = CreateBookShelfJson();
            for (int n = 0; n < 10; n++) {
                int start = TimeUtil.GetMs();
                using (var parser = new JsonParser()) {
                    parser.InitParser(json);
                    // parser.InitParser(new MemoryStream(json.buffer.array, json.start, json.Len));
                    while (parser.NextEvent() != JsonEvent.EOF) {
                        if (parser.error.ErrSet)
                            Fail(parser.error.msg.ToString());
                    }
                    IsTrue(parser.InputPos > 49_000_000);
                }
                int end = TimeUtil.GetMs();
                Console.WriteLine(end - start);
            }
        }
        
        [Test]
        public void TestReadTo() {
            var shelf = CreateBookShelf();
            Bytes json = CreateBookShelfJson();
            for (int n = 0; n < 10; n++) {
                GC.Collect();
                int start = TimeUtil.GetMs();
                using (var typeStore = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
                using (var reader = new JsonReader(typeStore))
                {
                    reader.ReadTo(json, shelf);
                    IsTrue(reader.Success);
                }
                int end = TimeUtil.GetMs();
                Console.WriteLine(end - start);
            }
        }
        
        [Test]
        public void TestRead() {
            Bytes json = CreateBookShelfJson();
            for (int n = 0; n < 10; n++) {
                GC.Collect();
                int start = TimeUtil.GetMs();
                using (var typeStore = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
                using (var reader = new JsonReader(typeStore))
                {
                    reader.Read<BookShelf>(json);
                }
                int end = TimeUtil.GetMs();
                Console.WriteLine(end - start);
            }
        }

#if !UNITY_5_3_OR_NEWER
        
        [Test]
        public void TestCreate() {
            List<string> titles = new List<string>(1000_000);
            for (int n = 0; n < 1_000_000; n++) {
                titles.Add( $"Book {n}");
            }
            for (int i = 0; i < 10; i++) {
                GC.Collect();
                int start = TimeUtil.GetMs();
                bookShelf = new BookShelf { Books = new List<Book>(1000_000) };
                for (int n = 0; n < 1_000_000; n++) {
                    var book = new Book {
                        Id = n,
                        Title = new string(titles[n]),
                        BookData = new byte[0]
                    };
                    bookShelf.Books.Add(book);
                }
                int end = TimeUtil.GetMs();
                Console.WriteLine(end - start);
            }
        }
#endif
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
