// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox
{
    public sealed class InvalidTypeException : Exception {
        public InvalidTypeException (string msg) : base(msg) { }
    }
}