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
        
        private void CreateBookShelfJson(ref Bytes json) {
            if (json.buffer.IsCreated())
                return;
            json = new Bytes(0);
            var shelf = CreateBookShelf();
            using (var typeStore = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
            using (var writer = new JsonWriter(typeStore)) {
                writer.Write(shelf, ref json);
            }
        }

        
        [Test]
        public void TestWrite() {
            BookShelf shelf = CreateBookShelf();
            Stream stream = new MemoryStream();
            using (var      typeStore   = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
            using (var      writer      = new JsonWriter(typeStore))
            {
                for (int n = 0; n < 10; n++) {
                    int start = TimeUtil.GetMs();
                    stream.Position = 0;
                    writer.Write(shelf, stream);
                    int end = TimeUtil.GetMs();
                    Console.WriteLine(end - start);
                }
            }
        }
        
        [Test]
        public void TestNextEvent() {
            CreateBookShelfJson(ref bookShelfJson);
            for (int n = 0; n < 10; n++) {
                int start = TimeUtil.GetMs();
                using (var parser = new JsonParser()) {
                    parser.InitParser(bookShelfJson);
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
        public void TestParser() {
            CreateBookShelfJson(ref bookShelfJson);
            for (int n = 0; n < 10; n++) {
                int start = TimeUtil.GetMs();
                int bookCount = 0;
                using (var parser = new JsonParser()) {
                    parser.InitParser(bookShelfJson);
                    parser.NextEvent();
                    var i = parser.GetObjectIterator();
                    while (parser.NextObjectMember(ref i)) {
                        if (parser.UseMemberArr(ref i, "Books")) {
                            var i2 = parser.GetArrayIterator();
                            while (parser.NextArrayElement(ref i2)) {
                                if (parser.UseElementObj(ref i2)) {
                                    var i3 = parser.GetObjectIterator();
                                    bookCount++;
                                    while (parser.NextObjectMember(ref i3)) {
                                        if      (parser.UseMemberStr(ref i3, "Title")) { }
                                        else if (parser.UseMemberNum(ref i3, "Id")) { }
                                    }
                                }
                            }
                        }
                    }
                    AreEqual(CreateBookShelf().Books.Count, bookCount);
                }
                int end = TimeUtil.GetMs();
                Console.WriteLine(end - start);
            }
        }
        
        [Test]
        public void TestReadTo() {
            var shelf = CreateBookShelf();
            CreateBookShelfJson(ref bookShelfJson);
            for (int n = 0; n < 10; n++) {
                GC.Collect();
                int start = TimeUtil.GetMs();
                using (var typeStore = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
                using (var reader = new JsonReader(typeStore))
                {
                    reader.ReadTo(bookShelfJson, shelf);
                    IsTrue(reader.Success);
                }
                int end = TimeUtil.GetMs();
                Console.WriteLine(end - start);
            }
        }
        
        [Test]
        public void TestRead() {
            CreateBookShelfJson(ref bookShelfJson);
            for (int n = 0; n < 10; n++) {
                GC.Collect();
                int start = TimeUtil.GetMs();
                using (var typeStore = new TypeStore(null, new StoreConfig(TypeAccess.IL)))
                using (var reader = new JsonReader(typeStore))
                {
                    reader.Read<BookShelf>(bookShelfJson);
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
