namespace IdleFramework.Save
{
    /// <summary>
    /// Provides a hook for obfuscating save payloads.
    /// </summary>
    public interface ISaveCipher
    {
        /// <summary>
        /// Encodes plaintext bytes.
        /// </summary>
        byte[] Encode(byte[] input);

        /// <summary>
        /// Decodes encoded bytes.
        /// </summary>
        byte[] Decode(byte[] input);
    }
}
