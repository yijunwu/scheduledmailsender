//
// SaslException.cs
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
using System.Runtime.Serialization;

namespace MailKit.Security {
	/// <summary>
	/// An enumeration of the possible error codes that may be reported by a <see cref="SaslException"/>.
	/// </summary>
	public enum SaslErrorCode {
		/// <summary>
		/// The server's challenge was too long.
		/// </summary>
		ChallengeTooLong,

		/// <summary>
		/// The server's response contained an incomplete challenge.
		/// </summary>
		IncompleteChallenge,

		/// <summary>
		/// The server's challenge was invalid.
		/// </summary>
		InvalidChallenge,

		/// <summary>
		/// The server's response did not contain a challenge.
		/// </summary>
		MissingChallenge,

		/// <summary>
		/// The server's challenge contained an incorrect hash.
		/// </summary>
		IncorrectHash
	}

	/// <summary>
	/// A SASL authentication exception.
	/// </summary>
	/// <remarks>
	/// Typically indicates an error while parsing a server's challenge token.
	/// </remarks>
#if !NETFX_CORE
	[Serializable]
#endif
	public class SaslException : AuthenticationException
	{
#if !NETFX_CORE
		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.Security.SaslException"/> class.
		/// </summary>
		/// <param name="info">The serialization info.</param>
		/// <param name="context">The streaming context.</param>
		protected SaslException (SerializationInfo info, StreamingContext context) : base (info, context)
		{
			ErrorCode = (SaslErrorCode) info.GetInt32 ("ErrorCode");
			Mechanism = info.GetString ("Mechanism");
		}
#endif

		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.Security.SaslException"/> class.
		/// </summary>
		/// <param name="mechanism">The SASL mechanism.</param>
		/// <param name="code">The error code.</param>
		/// <param name="message">The error message.</param>
		internal SaslException (string mechanism, SaslErrorCode code, string message) : base (message)
		{
			Mechanism = mechanism;
			ErrorCode = code;
		}

#if !NETFX_CORE
		/// <summary>
		/// When overridden in a derived class, sets the <see cref="System.Runtime.Serialization.SerializationInfo"/>
		/// with information about the exception.
		/// </summary>
		/// <param name="info">The serialization info.</param>
		/// <param name="context">The streaming context.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="info"/> is <c>null</c>.
		/// </exception>
		public override void GetObjectData (SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException ("info");

			info.AddValue ("ErrorCode", (int) ErrorCode);
			info.AddValue ("Mechanism", Mechanism);

			base.GetObjectData (info, context);
		}
#endif

		/// <summary>
		/// Gets the error code.
		/// </summary>
		/// <value>The error code.</value>
		public SaslErrorCode ErrorCode {
			get; private set;
		}

		/// <summary>
		/// Gets the name of the SASL mechanism that had the error.
		/// </summary>
		/// <value>The name of the SASL mechanism.</value>
		public string Mechanism {
			get; private set;
		}
	}
}
