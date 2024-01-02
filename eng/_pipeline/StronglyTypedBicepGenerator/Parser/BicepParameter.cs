namespace Vandertil.Blog.Pipeline.StronglyTypedBicepGenerator.Parser
{
    public class BicepParameter
    {
        public string Name { get; }

        public BicepDataType DataType { get; }

        public bool Required { get; }

        public bool IsSecret { get; }

        public BicepParameter(string name, BicepDataType dataType, bool required, bool isSecret)
        {
            Name = name;
            DataType = dataType;
            Required = required;
            IsSecret = isSecret;
        }
    }
}
