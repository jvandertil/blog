using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;

namespace BlogComments.GitHub.Jwt
{
    public class AppClientTokenGenerator
    {
        private const byte FULL_STOP_BYTE = (byte)'.';
        private const byte EQUALS_SIGN_BYTE = (byte)'=';

        private readonly ICryptographicSigner _signer;
        private readonly ISystemClock _clock;

        public AppClientTokenGenerator(ISystemClock clock, ICryptographicSigner signer)
        {
            _signer = signer;
            _clock = clock;
        }

        public string CreateToken(int applicationId, int durationValiditySeconds)
        {
            DateTimeOffset utcNow = _clock.UtcNow;

            var payload = new Dictionary<string, object>
            {
                ["iat"] = utcNow.ToUnixTimeSeconds(),
                ["exp"] = utcNow.AddSeconds(durationValiditySeconds).ToUnixTimeSeconds(),
                ["iss"] = applicationId,
            };

            return SerializeToken(payload);
        }

        private string SerializeToken(Dictionary<string, object> payload)
        {
            var headers = new Dictionary<string, object>
            {
                ["alg"] = _signer.Algorithm,
                ["typ"] = "JWT",
            };

            PooledByteBufferWriter buffer = new PooledByteBufferWriter(128);

            try
            {
                using (var headersSerialized = Utf8JsonSerializer.Serialize(headers))
                {
                    WriteAsUnpaddedBase64(headersSerialized.Memory, buffer);
                }

                WriteSeparator(buffer);

                using (var payloadSerialized = Utf8JsonSerializer.Serialize(payload))
                {
                    WriteAsUnpaddedBase64(payloadSerialized.Memory, buffer);
                }

                var signature = _signer.CalculateSignature(buffer.WrittenMemory.ToArray());

                WriteSeparator(buffer);
                WriteAsUnpaddedBase64(signature, buffer);

                var result = string.Create(buffer.WrittenCount, buffer.WrittenMemory, (dest, source) => Encoding.UTF8.GetChars(source.Span, dest));

                return result;
            }
            finally
            {
                buffer.Dispose();
            }
        }

        private static void WriteAsUnpaddedBase64(ReadOnlyMemory<byte> bytes, IBufferWriter<byte> writer)
        {
            var sizeHint = Base64.GetMaxEncodedToUtf8Length(bytes.Length);
            var buffer = writer.GetSpan(sizeHint);

            Base64.EncodeToUtf8(bytes.Span, buffer, out _, out int bytesWritten);

            int bytesTrimmed = TrimTrailingPadding(buffer.Slice(0, bytesWritten));

            writer.Advance(bytesWritten - bytesTrimmed);
        }

        private static int TrimTrailingPadding(Span<byte> buffer)
        {
            int bytesTrimmed = 0;

            for (int i = buffer.Length - 1; i >= 0; --i)
            {
                if (buffer[i] == EQUALS_SIGN_BYTE)
                {
                    buffer[i] = 0x00;

                    bytesTrimmed++;
                }
                else
                {
                    // No padding, or reached data. Done.
                    break;
                }
            }

            return bytesTrimmed;
        }

        private static void WriteSeparator(IBufferWriter<byte> writer)
        {
            var buffer = writer.GetSpan(1);
            buffer[0] = FULL_STOP_BYTE;

            writer.Advance(1);
        }
    }
}
