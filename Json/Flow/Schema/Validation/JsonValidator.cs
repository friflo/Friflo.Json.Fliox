// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Burst;

namespace Friflo.Json.Flow.Schema.Validation
{
    
    public class JsonValidator
    {
        private ValidationSchema    schema;
        
        public bool Validate (ref JsonParser parser, ValidationSchema schema) {
            this.schema = schema;
            
            return true;
        }
    }
}