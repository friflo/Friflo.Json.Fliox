// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;

namespace Friflo.Json.Burst.Utils
{
    // JSON_BURST_TAG
    public struct String32
    {
        public String value;
        
        public String32(String src) {
            value = src;
        }

        public override String ToString() { return value; }
    }
}