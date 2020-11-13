using System;
using System.Text.Json;

namespace BlogComments.GitHub.Jwt
{
    public class Utf8JsonSerializer
    {
        public static IReadOnlyMemoryOwner<byte> Serialize<T>(T message)
        {
            PooledByteBufferWriter? buffer = new PooledByteBufferWriter(32);

            try
            {
                using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false }))
                {
                    JsonSerializer.Serialize(writer, message);
                }

                var localBuffer = buffer;
                buffer = null;

                return new PooledBufferMemoryOwner(localBuffer);
            }
            finally
            {
                buffer?.Dispose();
            }
        }

        private class PooledBufferMemoryOwner : IReadOnlyMemoryOwner<byte>
        {
            private readonly PooledByteBufferWriter _writer;

            public ReadOnlyMemory<byte> Memory => _writer.WrittenMemory;

            public PooledBufferMemoryOwner(PooledByteBufferWriter writer)
            {
                _writer = writer;
            }

            public void Dispose()
            {
                _writer.Dispose();
            }
        }
    }
}
