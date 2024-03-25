// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

namespace Friflo.Json.Fliox.Mapper.Diff
{
    internal struct Parent
    {
        internal  readonly  object      left;   // not required but very handy for debugging
        internal  readonly  object      right;  // not required but very handy for debugging
        internal            DiffNode    diff;

        public    override  string      ToString() => diff?.ToString();

        internal Parent(object left, object right) {
            this.left   = left  ?? throw new ArgumentNullException(nameof(left));
            this.right  = right ?? throw new ArgumentNullException(nameof(right));
            diff        = null;
        }
    }
}