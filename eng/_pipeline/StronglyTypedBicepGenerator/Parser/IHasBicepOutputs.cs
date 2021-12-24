using System.Collections.Generic;

namespace Vandertil.Blog.Pipeline.StronglyTypedBicepGenerator.Parser
{
    public interface IHasBicepOutputs
    {
        IList<BicepOutput> Outputs { get; }
    }
}
