//
// SaslMechanismLogin.cs
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

namespace MailKit.Security {
	/// <summary>
	/// The LOGIN SASL mechanism.
	/// </summary>
	/// <remarks>
	/// The LOGIN SASL mechanism provides little protection over the use
	/// of plain-text passwords by obscuring the user name and password within
	/// individual base64-encoded blobs and should be avoided unless used in
	/// combination with an SSL or TLS connection.
	/// </remarks>
	public class SaslMechanismLogin : SaslMechanism
	{
		enum LoginState {
			UserName,
			Password
		}

		LoginState state;

		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.Security.SaslMechanismLogin"/> class.
		/// </summary>
		/// <param name="uri">The URI of the service.</param>
		/// <param name="credentials">The user's credentials.</param>
		public SaslMechanismLogin (Uri uri, ICredentials credentials) : base (uri, credentials)
		{
		}

		/// <summary>
		/// Gets the name of the mechanism.
		/// </summary>
		/// <value>The name of the mechanism.</value>
		public override string MechanismName {
			get { return "LOGIN"; }
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
			var cred = Credentials.GetCredential (Uri, MechanismName);
			byte[] challenge;

			switch (state) {
			case LoginState.UserName:
				challenge = Encoding.UTF8.GetBytes (cred.UserName);
				state = LoginState.Password;
				break;
			case LoginState.Password:
				challenge = Encoding.UTF8.GetBytes (cred.Password);
				IsAuthenticated = true;
				break;
			default:
				throw new InvalidOperationException ();
			}

			return challenge;
		}

		/// <summary>
		/// Resets the state of the SASL mechanism.
		/// </summary>
		public override void Reset ()
		{
			state = LoginState.UserName;
			base.Reset ();
		}
	}
}
