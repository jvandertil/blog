using System.Collections.Generic;

namespace Vandertil.Blog.Pipeline.StronglyTypedBicepGenerator.Parser
{
    internal class BicepFile : IHasBicepOutputs
    {
        public string Name { get; }

        public IList<BicepParameter> Parameters { get; }

        public IList<BicepModule> Modules { get; }

        public IList<BicepOutput> Outputs { get; }

        public BicepFile(string name)
        {
            Name = name;

            Parameters = new List<BicepParameter>();
            Modules = new List<BicepModule>();
            Outputs = new List<BicepOutput>();
        }
    }
}
