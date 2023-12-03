//HintName: BicepFileAttribute.cs
using System;

namespace BicepGenerator
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    internal sealed class BicepFileAttribute : Attribute
    {
        public string FileName { get; }

        public BicepFileAttribute(string fileName)
        {
            FileName = fileName;
        }
    }
}
