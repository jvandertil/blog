//HintName: BicepFileAttribute.Generated.cs
using System;

namespace Vandertil.BicepGenerator
{
    [System.CodeDom.Compiler.GeneratedCode("StronglyTypedBicepGenerator", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    internal sealed class BicepFileAttribute : Attribute
    {
        public string FileName { get; }

        public BicepFileAttribute(string fileName)
        {
            FileName = fileName;
        }
    }

    [System.CodeDom.Compiler.GeneratedCode("StronglyTypedBicepGenerator", "1.0.0.0")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    internal sealed class SecretAttribute : Attribute
    {
        public SecretAttribute() { }
    }
}
