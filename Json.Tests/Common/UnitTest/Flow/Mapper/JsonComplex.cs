using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Mapper
{
    public class JsonComplex
    {
        public IDictionary  <String, JsonSimple>    map  = null;
        public IDictionary  <String, JsonSimple>    map2 = new Dictionary <String, JsonSimple>();
        public Dictionary   <String, JsonSimple>    map3 = null;
        public Dictionary   <String, JsonSimple>    map4 = new Dictionary <String, JsonSimple>();
        public Dictionary   <String, JsonStruct>    mapStruct = new Dictionary <String, JsonStruct>();
        public DerivedMap                           mapDerived = new DerivedMap();
        public DerivedMap                           mapDerivedNull = null;
        public IDictionary  <String, String>        map5;

        public long             i64;
        public long             i64Neg;
        public int              i32;
        public short            i16;
        public byte             i8;
        public double           dbl;
        public float            flt;
        public float            fltSkip;
        public String           str;
        public String           strNull = "notNull";
        public String           escChars;
        public Object           n = "x";
        public ISub             subType = null;
        public bool             t;
        public bool             f;
        public Sub              sub;
        public JsonStruct       structValue = new JsonStruct (43);    // struct's are a 'value type'
        public IList <Sub>      list = null;
        public IList <Sub>      list2 =         new List <Sub>();
        public List <Sub>       list3 =         null;
        public List <Sub>       list4 =         new List <Sub>();
        public DerivedList      listDerived =   new DerivedList();
        public DerivedList      listDerivedNull = null;
        public List <String>    listStr =       new List <String>();
        public List <ISub>      listObj =       new List <ISub>();
        public List <JsonStruct>listStruct =    new List <JsonStruct>();
        public Sub[]            arr;
        public int[]            i64Arr;
        public bool[]           boolArr;

        public static int       notSerialized;
    }
     
    //
    public class JsonSimple
    {
        public long val;
        
        public JsonSimple() {}
        public JsonSimple(long val)
        {
            this.val = val;
        }
    }
    
    public struct JsonStruct   // todo support readonly struct?
    {
        public long val; // todo support readonly field?

        public JsonStruct(long val) {
            this.val = val;
        }
    }

    [Fri.Instance(typeof(Sub))]
    public interface ISub {
    }

    public class Sub : ISub
    {
        public long     i64;
        public Sub() {}
        public Sub(long i64)
        {
            this.i64 = i64;
        }
    }
    
    public class DerivedList : List <Sub>
    {       
    }

    public class DerivedMap : Dictionary <String, JsonSimple>
    {       
    }
}