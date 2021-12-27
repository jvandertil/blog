using System.Collections.Generic;
using System.ComponentModel;
using Nuke.Common.Tooling;

namespace Vandertil.Blog.Pipeline
{
    [TypeConverter(typeof(TypeConverter<Environment>))]
    public class Environment : Enumeration
    {
        public static readonly Environment Tst = new() { Value = "tst" };
        public static readonly Environment Prd = new() { Value = "prd" };
        
        public static IReadOnlyCollection<Environment> All() => new[] { Tst, Prd };

        public static implicit operator string(Environment environment)
        {
            return environment.Value;
        }
    }
}
