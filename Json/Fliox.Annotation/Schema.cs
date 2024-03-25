// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Friflo.Json.Fliox
{
    // --- schema generation Attributes
    // used by Friflo.Json.Fliox.Schema
    
    /// <summary>
    /// <a href="https://spec.openapis.org/oas/v3.0.0#openapi-object">OpenAPI Object specification</a>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class OpenAPIAttribute : Attribute {
        public OpenAPIAttribute (
            string version          = null,
            string termsOfService   = null,
            string licenseName      = null,
            string licenseUrl       = null,
            string contactName      = null,
            string contactUrl       = null,
            string contactEmail     = null)
        {
            Version         = version;
            TermsOfService  = termsOfService;
            LicenseName     = licenseName;
            LicenseUrl      = licenseUrl;
            ContactName     = contactName;
            ContactUrl      = contactUrl;
            ContactEmail    = contactEmail;
            
        }
        public  string  Version        { get; }
        public  string  TermsOfService { get; }
        
        public  string  LicenseName    { get; }
        public  string  LicenseUrl     { get; }
        
        public  string  ContactName    { get; }
        public  string  ContactUrl     { get; }
        public  string  ContactEmail   { get; }
    }
    
    /// <summary>
    /// <a href="https://spec.openapis.org/oas/v3.0.0#server-object">OpenAPI Server Object</a>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class OpenAPIServerAttribute : Attribute {
        public OpenAPIServerAttribute (
            string url          = null,
            string description  = null)
        {
            Url         = url;
            Description = description;
        }
        public  string  Url             { get; }
        public  string  Description     { get; }
    }
}