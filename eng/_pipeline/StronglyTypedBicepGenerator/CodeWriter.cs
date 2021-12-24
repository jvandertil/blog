using System.Text;

namespace Vandertil.Blog.Pipeline.StronglyTypedBicepGenerator
{
    internal class CodeWriter
    {
        private readonly StringBuilder _builder;
        private readonly string _indentToken;

        private int _indentLevel;

        private bool _indentWrittenForLine;

        public CodeWriter()
            : this("    ")
        {
        }

        public CodeWriter(string indentToken)
        {
            _indentToken = indentToken;
            _builder = new StringBuilder();
        }

        public CodeWriter Append(char value)
        {
            EnsureIndentationWritten();

            _builder.Append(value);

            return this;
        }

        public CodeWriter Append(string value)
        {
            EnsureIndentationWritten();

            _builder.Append(value);

            return this;
        }

        public CodeWriter AppendLine()
        {
            _builder.AppendLine();

            _indentWrittenForLine = false;

            return this;
        }

        public CodeWriter AppendLine(string text)
        {
            EnsureIndentationWritten();

            _builder.Append(text);
            AppendLine();

            return this;
        }

        public override string ToString()
        {
            return _builder.ToString();
        }

        public CodeWriter PushIndent()
        {
            _indentLevel++;
            return this;
        }

        public CodeWriter PopIndent()
        {
            if (_indentLevel > 0)
            {
                _indentLevel--;
            }

            return this;
        }

        private void EnsureIndentationWritten()
        {
            if (!_indentWrittenForLine)
            {
                for (int i = 0; i < _indentLevel; ++i)
                {
                    _builder.Append(_indentToken);
                }

                _indentWrittenForLine = true;
            }
        }
    }
}
