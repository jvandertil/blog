using System.Collections.Generic;

namespace Vandertil.Blog.Pipeline.StronglyTypedBicepGenerator.Parser
{
    internal class BicepModule : IHasBicepOutputs
    {
        public string Name { get; }

        public string RelativePath { get; }

        public IList<BicepOutput> Outputs { get; }

        public IList<BicepModule> Modules { get; }

        public BicepModule(string name, string relativePath)
        {
            Name = name;
            RelativePath = relativePath;

            Modules = new List<BicepModule>();
            Outputs = new List<BicepOutput>();
        }
    }
}
