namespace Vandertil.Blog.Pipeline.StronglyTypedBicepGenerator.Parser
{
    public class BicepParameter
    {
        public string Name { get; }

        public BicepDataType DataType { get; }

        public BicepParameter(string name, BicepDataType dataType)
        {
            Name = name;
            DataType = dataType;
        }
    }
}
