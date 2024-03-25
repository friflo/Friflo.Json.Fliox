// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable CollectionNeverQueried.Local
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Collection
{
    public static class TestListOne
    {
        [Test]
        public static void TestListOne_Add() {
            var list = new ListOne<int>();
            // --- items: 0
            AreEqual(0, list.Count);
            AreEqual(1, list.Capacity);
            AreEqual("Count: 0", list.ToString());
            var span = list.GetReadOnlySpan();
            AreEqual(0,  span.Length);
            Throws<IndexOutOfRangeException>(() => { var _ = list[0]; });
            Throws<IndexOutOfRangeException>(() => { var _ = list.GetReadOnlySpan()[0]; });

            // --- items: 1
            list.Add(20);
            AreEqual(1, list.Count);
            AreEqual(1, list.Capacity);
            AreEqual(20, list[0]);
            AreEqual("Count: 1", list.ToString());
            span = list.GetReadOnlySpan();
            AreEqual(1,  span.Length);
            AreEqual(20, span[0]);
            Throws<IndexOutOfRangeException>(() => { var _ = list[1]; });
            Throws<IndexOutOfRangeException>(() => { var _ = list.GetReadOnlySpan()[1]; });

            // --- items: 2
            list.Add(21);
            AreEqual(2, list.Count);
            AreEqual(4, list.Capacity);
            AreEqual(20, list[0]);
            AreEqual(21, list[1]);
            span = list.GetReadOnlySpan();
            AreEqual(2,  span.Length);
            AreEqual(20, span[0]);
            AreEqual(21, span[1]);

            // --- items: 2 - changed capacity
            list.Capacity = 10;
            AreEqual(2, list.Count);
            AreEqual(10, list.Capacity);
            AreEqual(20, list[0]);
            AreEqual(21, list[1]);
            span = list.GetReadOnlySpan();
            AreEqual(2,  span.Length);
            AreEqual(20, span[0]);
            AreEqual(21, span[1]);
            Throws<IndexOutOfRangeException>(() => { var _ = list[2]; });
            Throws<IndexOutOfRangeException>(() => { var _ = list.GetReadOnlySpan()[2]; });
        }
        
        [Test]
        public static void TestListOne_IndexSet() {
            var list = new ListOne<int>();
            
            // --- items: 0
            Throws<IndexOutOfRangeException>(() => { list[0] = 42; });
            
            // --- items: 1
            list.Add(10);
            list[0] = 20;
            AreEqual(20, list[0]);
            Throws<IndexOutOfRangeException>(() => { list[1] = 42; });
            
            // --- items: 2
            list.Add(11);
            list[0] = 30;
            list[1] = 31;
            AreEqual(30, list[0]);
            AreEqual(31, list[1]);
            Throws<IndexOutOfRangeException>(() => { list[2] = 42; });
        }
        
        [Test]
        public static void TestListOne_Enumerator() {
            var list = new ListOne<int>();
            
            // --- items: 0
            using var e0 = list.GetEnumerator();
            IsFalse(e0.MoveNext());
            
            // --- items: 1
            list.Add(10);
            using var e1 = list.GetEnumerator();
            IsTrue(e1.MoveNext());
            AreEqual(10, e1.Current);
            IsFalse(e1.MoveNext());
            
            // --- items: 2
            list.Add(11);
            using var e2 = list.GetEnumerator();
            IsTrue(e2.MoveNext());
            AreEqual(10, e2.Current);
            IsTrue(e2.MoveNext());
            AreEqual(11, e2.Current);
            IsFalse(e2.MoveNext());
            
            // --- check memory
            int count = 0;
            var start = Mem.GetAllocatedBytes();
            foreach (var _ in list) {
                count++;
            }
            var diff = Mem.GetAllocationDiff(start);
            AreEqual(2, count);
            Mem.AreEqual(0, diff);
        }
        
        [Test]
        public static void TestListOne_CopyTo() {
            var list = new ListOne<int>();
            
            // --- items: 0
            var array = Array.Empty<int>();
            list.CopyTo(array, 0);

            // --- items: 1
            array = new int[1];
            list.Add(10);
            list.CopyTo(array, 0);
            AreEqual(10, list[0]);

            // --- items: 2
            array = new int[2];
            list.Add(11);
            list.CopyTo(array, 0);
            AreEqual(10, list[0]);
            AreEqual(11, list[1]);
        }
        
        [Test]
        public static void TestListOne_RemoveRange_1() {
            var list = new ListOne<int>();
            list.Add(20);
            
            list.RemoveRange(0, 1);
            AreEqual(0,  list.Count);
            AreEqual(1,  list.Capacity);
            var span = list.GetReadOnlySpan();
            AreEqual(0,  span.Length);
        }

        [Test]
        public static void TestListOne_RemoveRange_2() {
            var list = new ListOne<int>();
            list.Add(20);
            list.Add(21);
            
            list.RemoveRange(0, 1);
            AreEqual(1,  list.Count);
            AreEqual(4,  list.Capacity);
            AreEqual(21, list[0]);
            var span = list.GetReadOnlySpan();
            AreEqual(1,  span.Length);
            AreEqual(21, span[0]);
        }
        
        [Test]
        public static void TestListOne_Mapper()
        {
            var mapper  = new ObjectMapper(new TypeStore());
            var list    = new ListOne<int>();
            
            var json = mapper.writer.WriteAsBytes(list);
            AreEqual("[]", json.AsString());
            
            list.Add(1);
            json = mapper.writer.WriteAsBytes(list);
            AreEqual("[1]", json.AsString());
            
            list.Add(2);
            json = mapper.writer.WriteAsBytes(list);
            AreEqual("[1,2]", json.AsString());
            
            var start = Mem.GetAllocatedBytes();
            mapper.writer.WriteAsBytes(list);
            var diff = Mem.GetAllocationDiff(start);
            Mem.AreEqual(0, diff);
        }
        
        private const string Expected = "{\"ints\":[11]}";
        
        [Test]
        public static void TestListOne_WriteAsMember()
        {
            var mapper  = new ObjectMapper(new TypeStore());
            mapper.WriteNullMembers = false;
            var obj     = new ListOneMember { ints = new ListOne<int>() };
            obj.ints.Add(11);
            
            var json = mapper.writer.WriteAsBytes(obj);
            AreEqual(Expected, json.AsString());
            
            var start = Mem.GetAllocatedBytes();
            mapper.writer.WriteAsBytes(obj);
            var diff = Mem.GetAllocationDiff(start);
            Mem.AreEqual(0, diff);
        }
        
        [Test]
        public static void TestListOne_ReadAsMember() {
            var mapper  = new ObjectMapper(new TypeStore());

            var jsonValue   = new JsonValue(Expected);
            var obj         = new ListOneMember();
            mapper.reader.ReadTo(jsonValue, obj, false);
            AreEqual(1,  obj.ints.Count);
            AreEqual(11, obj.ints[0]);
            //
            var start = Mem.GetAllocatedBytes();
            mapper.reader.ReadTo(jsonValue, obj, false);
            var diff = Mem.GetAllocationDiff(start);
            Mem.AreEqual(0, diff);
        }
        
        [Test]
        public static void TestListOne_Perf()
        {
            var list        = new ListOne<int>();
            const int count = 10; // 100_000_000;
            for (int n = 0; n < count; n++) {
                list.Add(21);
                list.Clear();
            }
            AreEqual(0, list.Count);
        }
    }
    
    internal class ListOneMember
    {
        public ListOne<int> ints;
    }
}