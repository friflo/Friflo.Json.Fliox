using System;
using System.Collections.Generic;

namespace Friflo.Json.Burst.Utils
{
    /// <summary>
    /// Provide a performant equality test for the encapsulated string with a <see cref="string"/> length less or equal 16 characters<br/>
    /// For longer strings it falls back to string.Equals(string).
    /// </summary>
    /// <remarks>
    /// Performance improvement - CLR: 5x,   Unity: 10x - 20x compared to string.Equals(string)<br/>
    /// Requires initialization - CLR: 1.5x, Unity: 0.45x     compared to string.Equals(string)<br/>
    /// </remarks>
    public readonly struct SmallString
    {
        /// <summary>
        ///  0   => null string <br/>
        /// -1   => empty string <br/>
        /// -2   => small string Length = 1  <br/>  
        /// -17  => small string Length = 16 <br/>
        /// > 16 => string Length
        /// </summary>
        private readonly    int     lenCode;
        
        private readonly    ulong   c00;
        private readonly    ulong   c04;
        private readonly    ulong   c08;
        private readonly    ulong   c12;
        
        public  readonly    string  value;
        
        public              int     Length      => lenCode > 0 ? lenCode : lenCode < 0 ? -lenCode - 1 : throw new NullReferenceException();
        public              bool    IsNull()    => lenCode == 0;
        public  override    string  ToString()  => value;
        
        public override bool Equals(object obj) {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use IsEqual() or SmallString.Equality comparer");
        }

        public override int GetHashCode() {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use SmallString.Equality comparer");
        }
        
        public bool IsEqual(in SmallString str) {
            return lenCode <= 0
                ? !(lenCode != str.lenCode  ||
                    c00     != str.c00      ||
                    c04     != str.c04      ||
                    c08     != str.c08      ||
                    c12     != str.c12)
                : lenCode == str.lenCode &&
                  value   == str.value;
        }

        public unsafe  SmallString(string str) {
            value   = str;
            if (str == null) {
                lenCode = 0; c00 = 0; c04 = 0; c08 = 0; c12 = 0;
                return;
            }
            var s       = str.AsSpan();
            var strLen  = s.Length;
            if (strLen == 0) {
                lenCode = -1; c00 = 0; c04 = 0; c08 = 0; c12 = 0;
                return;
            }
            fixed (char*  srcPtr = &s[0])
            {
                switch (strLen) {
                    case 1:  lenCode = -2;  c00 = s[0];                                             c04 = 0; c08 = 0; c12 = 0;  return;
                    case 2:  lenCode = -3;  c00 = s[0] + ((ulong)s[1] << 16);                       c04 = 0; c08 = 0; c12 = 0;  return;
                    case 3:  lenCode = -4;  c00 = s[0] + ((ulong)s[1] << 16) + ((ulong)s[2] << 32); c04 = 0; c08 = 0; c12 = 0;  return;
                    
                    case 4:  lenCode = -5;                                                          c04 = 0; c08 = 0; c12 = 0;  goto Pre0;
                    case 5:  lenCode = -6;  c04 = s[4];                                                      c08 = 0; c12 = 0;  goto Pre0;
                    case 6:  lenCode = -7;  c04 = s[4] + ((ulong)s[5] << 16);                                c08 = 0; c12 = 0;  goto Pre0;
                    case 7:  lenCode = -8;  c04 = s[4] + ((ulong)s[5] << 16) + ((ulong)s[6] << 32);          c08 = 0; c12 = 0;  goto Pre0;

                    case 8:  lenCode = -9;                                                                   c08 = 0; c12 = 0;  goto Pre4;
                    case 9:  lenCode = -10; c08 = s[8];                                                               c12 = 0;  goto Pre4;
                    case 10: lenCode = -11; c08 = s[8] + ((ulong)s[9] << 16);                                         c12 = 0;  goto Pre4;
                    case 11: lenCode = -12; c08 = s[8] + ((ulong)s[9] << 16) + ((ulong)s[10] << 32);                  c12 = 0;  goto Pre4;
                        
                    case 12: lenCode = -13;                                                                           c12 = 0;  goto Pre8;
                    case 13: lenCode = -14; c12 = s[12];                                                                        goto Pre8;
                    case 14: lenCode = -15; c12 = s[12]+ ((ulong)s[13] << 16);                                                  goto Pre8;
                    case 15: lenCode = -16; c12 = s[12]+ ((ulong)s[13] << 16)+ ((ulong)s[14] << 32);                            goto Pre8;
                        
                    case 16: lenCode = -17;                                                                                     goto Pre12;
                    default: lenCode = strLen; c00 = 0; c04 = 0; c08 = 0; c12 = 0;                                              return;
                }
                Pre12:  c12 = *(ulong*)(srcPtr + 12);
                Pre8:   c08 = *(ulong*)(srcPtr +  8);
                Pre4:   c04 = *(ulong*)(srcPtr +  4);
                Pre0:   c00 = *(ulong*)(srcPtr +  0);
            }
        }
        
        public static readonly  SmallStringComparer Equality = new SmallStringComparer();
        
        public sealed class SmallStringComparer : IEqualityComparer<SmallString>
        {
            public bool Equals(SmallString x, SmallString y) {
                return x.IsEqual(y);
            }

            public int GetHashCode(SmallString small) {
                return small.value.GetHashCode();
            }
        }
    }
    
}