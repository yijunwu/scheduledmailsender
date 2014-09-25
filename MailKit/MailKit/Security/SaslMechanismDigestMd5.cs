//
// SaslMechanismDigestMd5.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013-2014 Xamarin Inc. (www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Net;
using System.Text;
using System.Collections.Generic;

#if NETFX_CORE
using Encoding = Portable.Text.Encoding;
using MD5 = MimeKit.Cryptography.MD5;
#else
using MD5 = System.Security.Cryptography.MD5CryptoServiceProvider;
using System.Security.Cryptography;
#endif

namespace MailKit.Security {
	/// <summary>
	/// The DIGEST-MD5 SASL mechanism.
	/// </summary>
	/// <remarks>
	/// Unlike the PLAIN and LOGIN SASL mechanisms, the DIGEST-MD5 mechanism
	/// provides some level of protection and should be relatively safe to
	/// use even with a clear-text connection.
	/// </remarks>
	public class SaslMechanismDigestMd5 : SaslMechanism
	{
		enum LoginState {
			Auth,
			Final
		}

		DigestChallenge challenge;
		DigestResponse response;
		LoginState state;
		string cnonce;

		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.Security.SaslMechanismDigestMd5"/> class.
		/// </summary>
		/// <param name="uri">The URI of the service.</param>
		/// <param name="credentials">The user's credentials.</param>
		/// <param name="entropy">Random characters to act as the cnonce token.</param>
		internal SaslMechanismDigestMd5 (Uri uri, ICredentials credentials, string entropy) : base (uri, credentials)
		{
			cnonce = entropy;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.Security.SaslMechanismDigestMd5"/> class.
		/// </summary>
		/// <param name="uri">The URI of the service.</param>
		/// <param name="credentials">The user's credentials.</param>
		public SaslMechanismDigestMd5 (Uri uri, ICredentials credentials) : base (uri, credentials)
		{
		}

		/// <summary>
		/// Gets the name of the mechanism.
		/// </summary>
		/// <value>The name of the mechanism.</value>
		public override string MechanismName {
			get { return "DIGEST-MD5"; }
		}

		/// <summary>
		/// Parses the server's challenge token and returns the next challenge response.
		/// </summary>
		/// <returns>The next challenge response.</returns>
		/// <param name="token">The server's challenge token.</param>
		/// <param name="startIndex">The index into the token specifying where the server's challenge begins.</param>
		/// <param name="length">The length of the server's challenge.</param>
		/// <exception cref="SaslException">
		/// An error has occurred while parsing the server's challenge token.
		/// </exception>
		protected override byte[] Challenge (byte[] token, int startIndex, int length)
		{
			if (IsAuthenticated)
				throw new InvalidOperationException ();

			if (token == null)
				return null;

			var cred = Credentials.GetCredential (Uri, MechanismName);

			switch (state) {
			case LoginState.Auth:
				if (token.Length > 2048)
					throw new SaslException (MechanismName, SaslErrorCode.ChallengeTooLong, "Server challenge too long.");

				challenge = DigestChallenge.Parse (Encoding.UTF8.GetString (token, startIndex, length));

				if (string.IsNullOrEmpty (cnonce)) {
					var entropy = new byte[15];

					using (var rng = RandomNumberGenerator.Create ())
						rng.GetBytes (entropy);

					cnonce = Convert.ToBase64String (entropy);
				}

				response = new DigestResponse (challenge, Uri.Scheme, Uri.DnsSafeHost, cred.UserName, cred.Password, cnonce);
				state = LoginState.Final;
				return response.Encode ();
			case LoginState.Final:
				if (token.Length == 0)
					throw new SaslException (MechanismName, SaslErrorCode.MissingChallenge, "Server response did not contain any authentication data.");

				var text = Encoding.UTF8.GetString (token, startIndex, length);
				string key, value;
				int index = 0;

				if (!DigestChallenge.TryParseKeyValuePair (text, ref index, out key, out value))
					throw new SaslException (MechanismName, SaslErrorCode.IncompleteChallenge, "Server response contained incomplete authentication data.");

				var expected = response.ComputeHash (cred.Password, false);
				if (value != expected)
					throw new SaslException (MechanismName, SaslErrorCode.IncorrectHash, "Server response did not contain the expected hash.");

				IsAuthenticated = true;
				return new byte[0];
			default:
				throw new IndexOutOfRangeException ("state");
			}
		}

		/// <summary>
		/// Resets the state of the SASL mechanism.
		/// </summary>
		public override void Reset ()
		{
			state = LoginState.Auth;
			challenge = null;
			response = null;
			cnonce = null;
			base.Reset ();
		}
	}

	class DigestChallenge
	{
		public string[] Realms { get; private set; }
		public string Nonce { get; private set; }
		public HashSet<string> Qop { get; private set; }
		public bool Stale { get; private set; }
		public int MaxBuf { get; private set; }
		public string Charset { get; private set; }
		public string Algorithm { get; private set; }
		public HashSet<string> Ciphers { get; private set; }

		DigestChallenge ()
		{
			Ciphers = new HashSet<string> ();
			Qop = new HashSet<string> ();
		}

		static bool SkipWhiteSpace (string text, ref int index)
		{
			int startIndex = index;

			while (index < text.Length && char.IsWhiteSpace (text[index]))
				index++;

			return index > startIndex;
		}

		static bool TryParseKey (string text, ref int index, out string key)
		{
			int startIndex = index;

			key = null;

			while (index < text.Length && !char.IsWhiteSpace (text[index]) && text[index] != '=' && text[index] != ',')
				index++;

			if (index == startIndex)
				return false;

			key = text.Substring (startIndex, index - startIndex);

			return true;
		}

		static bool TryParseQuoted (string text, ref int index, out string value)
		{
			var builder = new StringBuilder ();
			bool escaped = false;

			value = null;

			// skip over leading '"'
			index++;

			while (index < text.Length) {
				if (text[index] == '\\') {
					if (escaped)
						builder.Append (text[index]);

					escaped = !escaped;
				} else if (!escaped) {
					if (text[index] == '"')
						break;

					builder.Append (text[index]);
				} else {
					escaped = false;
				}

				index++;
			}

			if (index >= text.Length || text[index] != '"')
				return false;

			index++;

			value = builder.ToString ();

			return true;
		}

		static bool TryParseValue (string text, ref int index, out string value)
		{
			if (text[index] == '"')
				return TryParseQuoted (text, ref index, out value);

			int startIndex = index;

			value = null;

			while (index < text.Length && !char.IsWhiteSpace (text[index]) && text[index] != ',')
				index++;

			if (index == startIndex)
				return false;

			value = text.Substring (startIndex, index - startIndex);

			return true;
		}

		public static bool TryParseKeyValuePair (string text, ref int index, out string key, out string value)
		{
			value = null;
			key = null;

			SkipWhiteSpace (text, ref index);

			if (!TryParseKey (text, ref index, out key))
				return false;

			SkipWhiteSpace (text, ref index);
			if (index >= text.Length || text[index] != '=')
				return false;

			// skip over '='
			index++;

			SkipWhiteSpace (text, ref index);

			return TryParseValue (text, ref index, out value);
		}

		public static DigestChallenge Parse (string token)
		{
			var challenge = new DigestChallenge ();
			int index = 0;

			while (index < token.Length) {
				string key, value;

				if (!TryParseKeyValuePair (token, ref index, out key, out value))
					throw new SaslException ("DIGEST-MD5", SaslErrorCode.InvalidChallenge, string.Format ("Invalid SASL challenge from the server: {0}", token));

				switch (key.ToLowerInvariant ()) {
				case "realm":
					challenge.Realms = value.Split (new [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					break;
				case "nonce":
					challenge.Nonce = value;
					break;
				case "qop":
					foreach (var qop in value.Split (new [] { ',' }, StringSplitOptions.RemoveEmptyEntries))
						challenge.Qop.Add (qop.Trim ());
					break;
				case "stale":
					challenge.Stale = value.ToLowerInvariant () == "true";
					break;
				case "maxbuf":
					challenge.MaxBuf = int.Parse (value);
					break;
				case "charset":
					challenge.Charset = value;
					break;
				case "algorithm":
					challenge.Algorithm = value;
					break;
				case "cipher":
					foreach (var cipher in value.Split (new [] { ',' }, StringSplitOptions.RemoveEmptyEntries))
						challenge.Ciphers.Add (cipher.Trim ());
					break;
				}

				SkipWhiteSpace (token, ref index);
				if (index < token.Length && token[index] == ',')
					index++;
			}

			return challenge;
		}
	}

	class DigestResponse
	{
		public string UserName { get; private set; }
		public string Realm { get; private set; }
		public string Nonce { get; private set; }
		public string CNonce { get; private set; }
		public int Nc { get; private set; }
		public string Qop { get; private set; }
		public string DigestUri { get; private set; }
		public string Response { get; private set; }
		public int MaxBuf { get; private set; }
		public string Charset { get; private set; }
		public string Algorithm { get; private set; }
		public string Cipher { get; private set; }
		public string AuthZid { get; private set; }

		public DigestResponse (DigestChallenge challenge, string protocol, string hostName, string userName, string password, string cnonce)
		{
			UserName = userName;

			if (challenge.Realms != null && challenge.Realms.Length > 0)
				Realm = challenge.Realms[0];
			else
				Realm = string.Empty;

			Nonce = challenge.Nonce;
			CNonce = cnonce;
			Nc = 1;

			// FIXME: make sure this is supported
			Qop = "auth";

			DigestUri = string.Format ("{0}/{1}", protocol, hostName);

			if (!string.IsNullOrEmpty (challenge.Charset))
				Charset = challenge.Charset;

			Algorithm = challenge.Algorithm;
			AuthZid = null;
			Cipher = null;

			Response = ComputeHash (password, true);
		}

		static string HexEncode (byte[] digest)
		{
			var hex = new StringBuilder ();

			for (int i = 0; i < digest.Length; i++)
				hex.Append (digest[i].ToString ("x2"));

			return hex.ToString ();
		}

		public string ComputeHash (string password, bool client)
		{
			string text, a1, a2;
			byte[] buf, digest;

			// compute A1
			text = string.Format ("{0}:{1}:{2}", UserName, Realm, password);
			buf = Encoding.UTF8.GetBytes (text);
			using (var md5 = new MD5 ())
				digest = md5.ComputeHash (buf);

			using (var md5 = new MD5 ()) {
				md5.TransformBlock (digest, 0, digest.Length, null, 0);
				text = string.Format (":{0}:{1}", Nonce, CNonce);
				if (!string.IsNullOrEmpty (AuthZid))
					text += ":" + AuthZid;
				buf = Encoding.ASCII.GetBytes (text);
				md5.TransformFinalBlock (buf, 0, buf.Length);
				a1 = HexEncode (md5.Hash);
			}

			// compute A2
			text = client ? "AUTHENTICATE:" : ":";
			text += DigestUri;

			if (Qop == "auth-int" || Qop == "auth-conf")
				text += ":00000000000000000000000000000000";

			buf = Encoding.ASCII.GetBytes (text);
			using (var md5 = new MD5 ())
				digest = md5.ComputeHash (buf);
			a2 = HexEncode (digest);

			// compute KD
			text = string.Format ("{0}:{1}:{2:x8}:{3}:{4}:{5}", a1, Nonce, Nc, CNonce, Qop, a2);
			buf = Encoding.ASCII.GetBytes (text);
			using (var md5 = new MD5 ())
				digest = md5.ComputeHash (buf);

			return HexEncode (digest);
		}

		public byte[] Encode ()
		{
			Encoding encoding;

			if (!string.IsNullOrEmpty (Charset))
				encoding = Encoding.GetEncoding (Charset);
			else
				encoding = Encoding.UTF8;

			var builder = new StringBuilder ();
			builder.AppendFormat ("username=\"{0}\"", UserName);
			builder.AppendFormat (",realm=\"{0}\"", Realm);
			builder.AppendFormat (",nonce=\"{0}\"", Nonce);
			builder.AppendFormat (",cnonce=\"{0}\"", CNonce);
			builder.AppendFormat (",nc={0:x8}", Nc);
			builder.AppendFormat (",qop=\"{0}\"", Qop);
			builder.AppendFormat (",digest-uri=\"{0}\"", DigestUri);
			builder.AppendFormat (",response=\"{0}\"", Response);
			if (MaxBuf > 0)
				builder.AppendFormat (",maxbuf={0}", MaxBuf);
			if (!string.IsNullOrEmpty (Charset))
				builder.AppendFormat (",charset=\"{0}\"", Charset);
			if (!string.IsNullOrEmpty (Algorithm))
				builder.AppendFormat (",algorithm=\"{0}\"", Algorithm);
			if (!string.IsNullOrEmpty (Cipher))
				builder.AppendFormat (",cipher=\"{0}\"", Cipher);
			if (!string.IsNullOrEmpty (AuthZid))
				builder.AppendFormat (",authzid=\"{0}\"", AuthZid);

			return encoding.GetBytes (builder.ToString ());
		}
	}
}
