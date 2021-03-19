// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map
{
    public class Patcher
    {

        private             Patch       patch;
        
        public Patcher() {
        }
        
        public void Patch<T>(TypeMapper<T> mapper, T value, Patch patch) {
            this.patch = patch;
            mapper.PatchObject(this, value);
        }
    }
}