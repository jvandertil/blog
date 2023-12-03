using System.Runtime.CompilerServices;
using VerifyTests;

namespace Vandertil.Blog.Pipeline.StronglyTypedBicepGenerator.Tests;

public class Startup
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}
