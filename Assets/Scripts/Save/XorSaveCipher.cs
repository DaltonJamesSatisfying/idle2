using System;
using System.Text;

namespace IdleFramework.Save
{
    /// <summary>
    /// Minimal XOR cipher used as a placeholder for production ready obfuscation.
    /// </summary>
    public sealed class XorSaveCipher : ISaveCipher
    {
        private readonly byte[] _key;

        /// <summary>
        /// Initializes a new instance of the <see cref="XorSaveCipher"/> class.
        /// </summary>
        public XorSaveCipher(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key must not be empty", nameof(key));
            }

            _key = Encoding.UTF8.GetBytes(key);
        }

        /// <inheritdoc />
        public byte[] Encode(byte[] input)
        {
            return Transform(input);
        }

        /// <inheritdoc />
        public byte[] Decode(byte[] input)
        {
            return Transform(input);
        }

        private byte[] Transform(byte[] input)
        {
            var output = new byte[input.Length];
            for (var i = 0; i < input.Length; i++)
            {
                output[i] = (byte)(input[i] ^ _key[i % _key.Length]);
            }

            return output;
        }
    }
}
