namespace Vandertil.Blog.Pipeline.StronglyTypedBicepGenerator.Parser
{
    public class BicepOutput
    {
        public string Name { get; }

        public BicepDataType DataType { get; }

        public BicepOutput(string name, BicepDataType dataType)
        {
            Name = name;
            DataType = dataType;
        }
    }
}
