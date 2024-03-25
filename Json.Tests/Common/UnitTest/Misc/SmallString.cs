// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Tests.Common.UnitTest.Misc
{
#if UNITY_5_3_OR_NEWER || DEBUG

    /// <summary>
    /// Optimization specific for Unity. Falls back to standard string methods in .NET CLR<br/>
    /// Provide a performant equality test for the encapsulated string with a <see cref="string"/> length less or equal 16 characters<br/>
    /// For longer strings it falls back to string.Equals(string).
    /// </summary>
    /// <remarks>
    /// Performance improvement In Unity:
    /// <see cref="IsEqual"/> 10x compared to string.Equals(string)<br/>
    /// 1.7x  when used for <see cref="Dictionary{TKey,TValue}"/> index operators
    /// </remarks>
    public readonly partial struct SmallString
    {
        /// <summary>
        ///  0   => null string <br/>
        /// -1   => empty string <br/>
        /// -2   => small string Length = 1  <br/>  
        /// -17  => small string Length = 16 <br/>
        /// > 16 => string Length
        /// </summary>
        private readonly    int     lenCode;
        private readonly    int     hashCode;
        
        private readonly    ulong   c00;
        private readonly    ulong   c04;
        private readonly    ulong   c08;
        private readonly    ulong   c12;
        
        public  readonly    string  value;
        
        public              int     Length      => lenCode > 0 ? lenCode : lenCode < 0 ? -lenCode - 1 : throw new NullReferenceException();
        public              bool    IsNull()    => lenCode == 0;
        
        public bool IsEqual(in SmallString str) {
            return lenCode <= 0
                ? !(lenCode  != str.lenCode  ||
                    hashCode != str.hashCode ||
                    c00      != str.c00      ||
                    c04      != str.c04      ||
                    c08      != str.c08      ||
                    c12      != str.c12)
                : lenCode == str.lenCode &&
                  value   == str.value;
        }

        public unsafe  SmallString(string str) {
            value   = str;
            if (str == null) {
                lenCode = 0; c00 = 0; c04 = 0; c08 = 0; c12 = 0;
                hashCode = 1234;
                return;
            }
            var s       = str.AsSpan();
            var strLen  = s.Length;
            if (strLen == 0) {
                lenCode = -1; c00 = 0; c04 = 0; c08 = 0; c12 = 0;
                hashCode = 5678;
                return;
            }
            fixed (char*  srcPtr = &s[0])
            {
                switch (strLen) {
                    case 1:  lenCode = -2;  c00 = s[0];                                             c04 = 0; c08 = 0; c12 = 0;  goto End;
                    case 2:  lenCode = -3;  c00 = s[0] + ((ulong)s[1] << 16);                       c04 = 0; c08 = 0; c12 = 0;  goto End;
                    case 3:  lenCode = -4;  c00 = s[0] + ((ulong)s[1] << 16) + ((ulong)s[2] << 32); c04 = 0; c08 = 0; c12 = 0;  goto End;
                    
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
                             
                    default: lenCode = strLen; c00 = 0; c04 = 0; c08 = 0; c12 = 0;
                             hashCode = 0; // calculate on demand
                             return;
                }
                Pre12:  c12 = *(ulong*)(srcPtr + 12);
                Pre8:   c08 = *(ulong*)(srcPtr +  8);
                Pre4:   c04 = *(ulong*)(srcPtr +  4);
                Pre0:   c00 = *(ulong*)(srcPtr +  0);
                End:
                var hash = (ulong)lenCode ^ c00 ^ c04 ^ c08 ^ c12;
                hashCode = (int)hash ^ (int)(hash >> 32);
            }
        }
        
        private sealed class SmallStringComparer : IEqualityComparer<SmallString>
        {
            public bool Equals(SmallString x, SmallString y) {
                return x.IsEqual(y);
            }

            public int GetHashCode(SmallString small) {
                return small.lenCode <= 0 ? small.hashCode : small.value.GetHashCode();
            }
        }
    }
#else
    public readonly partial struct SmallString
    {
        public  readonly    string  value;
        
        public  SmallString(string str) {
            value   = str;
        }
        
        public          int     Length                      => value.Length;
        public          bool    IsNull()                    => value == null;
        public          bool    IsEqual(in SmallString str) => value == str.value;
    }

    public sealed class SmallStringComparer : IEqualityComparer<SmallString>
    {
        public bool Equals(SmallString x, SmallString y) {
            return x.value == y.value;
        }

        public int GetHashCode(SmallString small) {
            return small.value.GetHashCode();
        }
    }
#endif

    public readonly partial struct SmallString
    {
        public override string  ToString()  => value;
        
        public override bool Equals(object obj) {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use IsEqual() or SmallString.Equality comparer");
        }

        public override int GetHashCode() {
            throw new NotImplementedException("not implemented by intention to avoid boxing. Use SmallString.Equality comparer");
        }

        public static readonly  IEqualityComparer<SmallString> Equality = new SmallStringComparer();    
    }

}