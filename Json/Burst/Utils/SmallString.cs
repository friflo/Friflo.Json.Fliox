using System;

namespace Friflo.Json.Burst.Utils
{
    public readonly struct SmallString
    {
        private readonly    int     len;
        
        private readonly    ulong   c00;
        private readonly    ulong   c04;
        private readonly    ulong   c08;
        private readonly    ulong   c12;
        
        public  readonly    string  value;

        public  override    string  ToString() => value;
        
        public bool IsEqual(in SmallString str) {
            return len <= 16 ?
                !(c00 != str.c00 ||
                  c04 != str.c04 ||
                  c08 != str.c08 ||
                  c12 != str.c12) :
                 value  == str.value;
        }

        public unsafe  SmallString(string str) {
            value   = str;
            var s   = str.AsSpan();
            len     = s.Length;
            if (len == 0) {
                c00 = 0; c04 = 0; c08 = 0; c12 = 0;
                return;
            }
            fixed (char*  srcPtr = &s[0])
            {
                switch (len) {
                    case 1:     c00 = s[0];                                             c04 = 0; c08 = 0; c12 = 0;  return;
                    case 2:     c00 = s[0] + ((ulong)s[1] << 16);                       c04 = 0; c08 = 0; c12 = 0;  return;
                    case 3:     c00 = s[0] + ((ulong)s[1] << 16) + ((ulong)s[2] << 32); c04 = 0; c08 = 0; c12 = 0;  return;
                    
                    case 4:                                                             c04 = 0; c08 = 0; c12 = 0;  goto Pre0;
                    case 5:     c04 = s[4];                                                      c08 = 0; c12 = 0;  goto Pre0;
                    case 6:     c04 = s[4] + ((ulong)s[5] << 16);                                c08 = 0; c12 = 0;  goto Pre0;
                    case 7:     c04 = s[4] + ((ulong)s[5] << 16) + ((ulong)s[6] << 32);          c08 = 0; c12 = 0;  goto Pre0;

                    case 8:                                                                      c08 = 0; c12 = 0;  goto Pre4;
                    case 9:     c08 = s[8];                                                               c12 = 0;  goto Pre4;
                    case 10:    c08 = s[8] + ((ulong)s[9] << 16);                                         c12 = 0;  goto Pre4;
                    case 11:    c08 = s[8] + ((ulong)s[9] << 16) + ((ulong)s[10] << 32);                  c12 = 0;  goto Pre4;
                        
                    case 12:                                                                              c12 = 0;  goto Pre8;
                    case 13:    c12 = s[12];                                                                        goto Pre8;
                    case 14:    c12 = s[12]+ ((ulong)s[13] << 16);                                                  goto Pre8;
                    case 15:    c12 = s[12]+ ((ulong)s[13] << 16)+ ((ulong)s[14] << 32);                            goto Pre8;
                        
                    case 16:                                                                                        goto Pre12;
                    default:    c00 = 0; c04 = 0; c08 = 0; c12 = 0;                                                 return;
                }
                Pre12:  c12 = *(ulong*)(srcPtr + 12);
                Pre8:   c08 = *(ulong*)(srcPtr +  8);
                Pre4:   c04 = *(ulong*)(srcPtr +  4);
                Pre0:   c00 = *(ulong*)(srcPtr +  0);
            }
        }
    }
}