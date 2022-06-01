// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable CheckNamespace
namespace Friflo.Json.Fliox
{
    // ------------------------------- OpenAPI attributes -------------------------------
    /// <summary>
    /// <a href="https://spec.openapis.org/oas/v3.0.0#openapi-object">OpenAPI Object specification</a>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class OpenAPI : Attribute {
        public string           Version        { get; set; }
        public string           TermsOfService { get; set; }
        
        public string           LicenseName    { get; set; }
        public string           LicenseUrl     { get; set; }
        
        public string           ContactName    { get; set; }
        public string           ContactUrl     { get; set; }
        public string           ContactEmail   { get; set; }
    }
    
    /// <summary>
    /// <a href="https://spec.openapis.org/oas/v3.0.0#server-object">OpenAPI Server Object</a>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class OpenAPIServer : Attribute {
        public string           Url             { get; set; }
        public string           Description     { get; set; }
    }
}