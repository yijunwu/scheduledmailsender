using System;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.TeleTrust;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Math.EC.Multiplier;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;

namespace Org.BouncyCastle.Crypto.Generators
{
    public class ECKeyPairGenerator
        : IAsymmetricCipherKeyPairGenerator
    {
        private readonly string algorithm;

        private ECDomainParameters parameters;
        private DerObjectIdentifier publicKeyParamSet;
        private SecureRandom random;

        public ECKeyPairGenerator()
            : this("EC")
        {
        }

        public ECKeyPairGenerator(
            string algorithm)
        {
            if (algorithm == null)
                throw new ArgumentNullException("algorithm");

            this.algorithm = ECKeyParameters.VerifyAlgorithmName(algorithm);
        }

        public void Init(
            KeyGenerationParameters parameters)
        {
            if (parameters is ECKeyGenerationParameters)
            {
                ECKeyGenerationParameters ecP = (ECKeyGenerationParameters) parameters;

                this.publicKeyParamSet = ecP.PublicKeyParamSet;
                this.parameters = ecP.DomainParameters;
            }
            else
            {
                DerObjectIdentifier oid;
                switch (parameters.Strength)
                {
                    case 192:
                        oid = X9ObjectIdentifiers.Prime192v1;
                        break;
                    case 224:
                        oid = SecObjectIdentifiers.SecP224r1;
                        break;
                    case 239:
                        oid = X9ObjectIdentifiers.Prime239v1;
                        break;
                    case 256:
                        oid = X9ObjectIdentifiers.Prime256v1;
                        break;
                    case 384:
                        oid = SecObjectIdentifiers.SecP384r1;
                        break;
                    case 521:
                        oid = SecObjectIdentifiers.SecP521r1;
                        break;
                    default:
                        throw new InvalidParameterException("unknown key size.");
                }

                X9ECParameters ecps = FindECCurveByOid(oid);

                this.parameters = new ECDomainParameters(
                    ecps.Curve, ecps.G, ecps.N, ecps.H, ecps.GetSeed());
            }

            this.random = parameters.Random;

            if (this.random == null)
            {
                this.random = new SecureRandom();
            }
        }

        /**
         * Given the domain parameters this routine generates an EC key
         * pair in accordance with X9.62 section 5.2.1 pages 26, 27.
         */
        public AsymmetricCipherKeyPair GenerateKeyPair()
        {
            BigInteger n = parameters.N;
            BigInteger d;
            int minWeight = n.BitLength >> 2;

            for (;;)
            {
                d = new BigInteger(n.BitLength, random);

                if (d.CompareTo(BigInteger.Two) < 0 || d.CompareTo(n) >= 0)
                    continue;

                /*
                 * Require a minimum weight of the NAF representation, since low-weight primes may be
                 * weak against a version of the number-field-sieve for the discrete-logarithm-problem.
                 * 
                 * See "The number field sieve for integers of low weight", Oliver Schirokauer.
                 */
                if (WNafUtilities.GetNafWeight(d) < minWeight)
                    continue;

                break;
            }

            ECPoint q = CreateBasePointMultiplier().Multiply(parameters.G, d);

            if (publicKeyParamSet != null)
            {
                return new AsymmetricCipherKeyPair(
                    new ECPublicKeyParameters(algorithm, q, publicKeyParamSet),
                    new ECPrivateKeyParameters(algorithm, d, publicKeyParamSet));
            }

            return new AsymmetricCipherKeyPair(
                new ECPublicKeyParameters(algorithm, q, parameters),
                new ECPrivateKeyParameters(algorithm, d, parameters));
        }

        protected virtual ECMultiplier CreateBasePointMultiplier()
        {
            return new FixedPointCombMultiplier();
        }

        internal static X9ECParameters FindECCurveByOid(DerObjectIdentifier oid)
        {
            // TODO ECGost3410NamedCurves support (returns ECDomainParameters though)

            X9ECParameters ecP = CustomNamedCurves.GetByOid(oid);
            if (ecP == null)
            {
                ecP = ECNamedCurveTable.GetByOid(oid);
            }
            return ecP;
        }

        internal static ECPublicKeyParameters GetCorrespondingPublicKey(
            ECPrivateKeyParameters privKey)
        {
            ECDomainParameters ec = privKey.Parameters;
            ECPoint q = new FixedPointCombMultiplier().Multiply(ec.G, privKey.D);

            if (privKey.PublicKeyParamSet != null)
            {
                return new ECPublicKeyParameters(privKey.AlgorithmName, q, privKey.PublicKeyParamSet);
            }

            return new ECPublicKeyParameters(privKey.AlgorithmName, q, ec);
        }
    }
}
