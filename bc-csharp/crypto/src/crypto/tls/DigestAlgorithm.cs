using System;

namespace Org.BouncyCastle.Crypto.Tls
{
    /// <summary>RFC 2246</summary>
    /// <remarks>
    /// Note that the values here are implementation-specific and arbitrary. It is recommended not to
    /// depend on the particular values (e.g. serialization).
    /// </remarks>
    [Obsolete("Use MacAlgorithm constants instead")]
    public enum DigestAlgorithm
    {
        NULL,
        MD5,
        SHA,

        /*
         * RFC 5246
         */
        SHA256,
        SHA384,
        SHA512,
    }
}
