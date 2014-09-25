//
// SmtpClient.cs
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
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using MimeKit;
using MimeKit.IO;

#if NETFX_CORE
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Encoding = Portable.Text.Encoding;
#else
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using SslProtocols = System.Security.Authentication.SslProtocols;
#endif

using MailKit.Security;

namespace MailKit.Net.Smtp {
	/// <summary>
	/// An SMTP client that can be used to send email messages.
	/// </summary>
	/// <remarks>
	/// The <see cref="SmtpClient"/> class supports both the "smtp" and "smtps"
	/// protocols. The "smtp" protocol makes a clear-text connection to the SMTP
	/// server and does not use SSL or TLS unless the SMTP server supports the
	/// STARTTLS extension (as defined by rfc3207). The "smtps" protocol,
	/// however, connects to the SMTP server using an SSL-wrapped connection.
	/// </remarks>
	public class SmtpClient : IMessageTransport
	{
		static readonly byte[] EndData = Encoding.ASCII.GetBytes ("\r\n.\r\n");
		static readonly Encoding Latin1 = Encoding.GetEncoding (28591);

		enum SmtpCommand {
			MailFrom,
			RcptTo,

			// TODO:
			//Data,
		}

		readonly List<SmtpCommand> queued = new List<SmtpCommand> ();
		readonly HashSet<string> authmechs = new HashSet<string> ();
		readonly byte[] input = new byte[4096];
		readonly IProtocolLogger logger;
		SmtpCapabilities capabilities;
		int inputIndex, inputEnd;
		MemoryBlockStream queue;
#if !NETFX_CORE
		EndPoint localEndPoint;
#else
		StreamSocket socket;
#endif
		bool authenticated;
		Stream stream;
		bool disposed;
		string host;

		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.Net.Smtp.SmtpClient"/> class.
		/// </summary>
		/// <remarks>
		/// Before you can send messages with the <see cref="SmtpClient"/>, you must first call
		/// the <see cref="Connect(Uri,CancellationToken)"/> method. Depending on the server,
		/// you may also need to authenticate using the
		/// <see cref="Authenticate(ICredentials,CancellationToken)"/> method.
		/// </remarks>
		public SmtpClient () : this (new NullProtocolLogger ())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.Net.Smtp.SmtpClient"/> class.
		/// </summary>
		/// <remarks>
		/// Before you can send messages with the <see cref="SmtpClient"/>, you must first call
		/// the <see cref="Connect(Uri,CancellationToken)"/> method. Depending on the server,
		/// you may also need to authenticate using the
		/// <see cref="Authenticate(ICredentials,CancellationToken)"/> method.
		/// </remarks>
		/// <param name="protocolLogger">The protocol logger.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="protocolLogger"/> is <c>null</c>.
		/// </exception>
		public SmtpClient (IProtocolLogger protocolLogger)
		{
			if (protocolLogger == null)
				throw new ArgumentNullException ("protocolLogger");

			logger = protocolLogger;
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="MailKit.Net.Smtp.SmtpClient"/> is reclaimed by garbage collection.
		/// </summary>
		~SmtpClient ()
		{
			Dispose (false);
		}

		/// <summary>
		/// Gets the capabilities supported by the SMTP server.
		/// </summary>
		/// <remarks>
		/// The capabilities will not be known until a successful connection has been made via
		/// the <see cref="Connect(Uri,CancellationToken)"/> method and may change as a side-effect
		/// of the <see cref="Authenticate(ICredentials,CancellationToken)"/> method.
		/// </remarks>
		/// <value>The capabilities.</value>
		/// <exception cref="System.ArgumentException">
		/// Capabilities cannot be enabled, they may only be disabled.
		/// </exception>
		public SmtpCapabilities Capabilities {
			get { return capabilities; }
			set {
				if ((capabilities | value) > capabilities)
					throw new ArgumentException ("Capabilities cannot be enabled, they may only be disabled.", "value");

				capabilities = value;
			}
		}

		/// <summary>
		/// Gets the maximum message size supported by the server.
		/// </summary>
		/// <remarks>
		/// <para>The maximum message size will not be known until a successful
		/// connection has been made via the <see cref="Connect(Uri,CancellationToken)"/> method
		/// and may change as a side-effect of the <see cref="Authenticate(ICredentials,CancellationToken)"/>
		/// method.</para>
		/// <para>Note: This value is only relevant if the <see cref="Capabilities"/>
		/// includes the <see cref="SmtpCapabilities.Size"/> flag.</para>
		/// </remarks>
		/// <value>The maximum message size supported by the server.</value>
		public uint MaxSize {
			get; private set;
		}

		void CheckDisposed ()
		{
			if (disposed)
				throw new ObjectDisposedException ("SmtpClient");
		}

		#region IMessageService implementation

#if !NETFX_CORE
		/// <summary>
		/// Gets or sets the client SSL certificates.
		/// </summary>
		/// <remarks>
		/// <para>Some servers may require the client SSL certificates in order
		/// to allow the user to connect.</para>
		/// <para>This property should be set before calling <see cref="Connect(Uri,CancellationToken)"/>.</para>
		/// </remarks>
		/// <value>The client SSL certificates.</value>
		public X509CertificateCollection ClientCertificates {
			get; set;
		}
#endif

		/// <summary>
		/// Gets the authentication mechanisms supported by the SMTP server.
		/// </summary>
		/// <remarks>
		/// The authentication mechanisms are queried durring the <see cref="Connect(Uri,CancellationToken)"/> method.
		/// </remarks>
		/// <value>The authentication mechanisms.</value>
		public HashSet<string> AuthenticationMechanisms {
			get { return authmechs; }
		}

		/// <summary>
		/// Gets whether or not the client is currently connected to an SMTP server.
		/// </summary>
		/// <remarks>
		/// When a <see cref="SmtpProtocolException"/> is caught, the connection state of the
		/// <see cref="SmtpClient"/> should be checked before continuing.
		/// </remarks>
		/// <value><c>true</c> if the client is connected; otherwise, <c>false</c>.</value>
		public bool IsConnected {
			get; private set;
		}

#if !NETFX_CORE
		bool ValidateRemoteCertificate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
		{
			if (ServicePointManager.ServerCertificateValidationCallback != null)
				return ServicePointManager.ServerCertificateValidationCallback (sender, certificate, chain, errors);

			return true;
		}
#endif

		static bool TryParseInt32 (byte[] text, ref int index, int endIndex, out int value)
		{
			int startIndex = index;

			value = 0;

			while (index < endIndex && text[index] >= (byte) '0' && text[index] <= (byte) '9')
				value = (value * 10) + (text[index++] - (byte) '0');

			return index > startIndex;
		}

		SmtpResponse ReadResponse (CancellationToken cancellationToken)
		{
			using (var memory = new MemoryStream ()) {
				bool complete = false;
				bool newLine = true;
				bool more = true;
				int code = 0;
				int nread;

				do {
					if (memory.Length > 0 || inputIndex == inputEnd) {
						// make room for the next read by moving the remaining data to the beginning of the buffer
						int left = inputEnd - inputIndex;

						for (int i = 0; i < left; i++)
							input[i] = input[inputIndex + i];

						inputEnd = left;
						inputIndex = 0;

						cancellationToken.ThrowIfCancellationRequested ();

						if ((nread = stream.Read (input, inputEnd, input.Length - inputEnd)) == 0)
							throw new SmtpProtocolException ("The SMTP server unexpectedly disconnected.");

						logger.LogServer (input, inputEnd, nread);
						inputEnd += nread;
					}

					complete = false;

					do {
						int startIndex = inputIndex;

						if (newLine && inputIndex < inputEnd) {
							int value;

							if (!TryParseInt32 (input, ref inputIndex, inputEnd, out value))
								throw new SmtpProtocolException ("Unable to parse status code returned by the server.");

							if (inputIndex == inputEnd) {
								inputIndex = startIndex;
								break;
							}

							if (code == 0) {
								code = value;
							} else if (value != code) {
								throw new SmtpProtocolException ("The status codes returned by the server did not match.");
							}

							newLine = false;

							if (input[inputIndex] != (byte) '\r' && input[inputIndex] != (byte) '\n')
								more = input[inputIndex++] == (byte) '-';
							else
								more = false;

							startIndex = inputIndex;
						}

						while (inputIndex < inputEnd && input[inputIndex] != (byte) '\r' && input[inputIndex] != (byte) '\n')
							inputIndex++;

						memory.Write (input, startIndex, inputIndex - startIndex);

						if (inputIndex < inputEnd && input[inputIndex] == (byte) '\r')
							inputIndex++;

						if (inputIndex < inputEnd && input[inputIndex] == (byte) '\n') {
							if (more)
								memory.WriteByte (input[inputIndex]);
							complete = true;
							newLine = true;
							inputIndex++;
						}
					} while (more && inputIndex < inputEnd);
				} while (more || !complete);

				string message = null;

				try {
#if !NETFX_CORE
					message = Encoding.UTF8.GetString (memory.GetBuffer (), 0, (int) memory.Length);
#else
					message = Encoding.UTF8.GetString (memory.ToArray (), 0, (int) memory.Length);
#endif
				} catch {
#if !NETFX_CORE
					message = Latin1.GetString (memory.GetBuffer (), 0, (int) memory.Length);
#else
					message = Latin1.GetString (memory.ToArray (), 0, (int) memory.Length);
#endif
				}

				return new SmtpResponse ((SmtpStatusCode) code, message);
			}
		}

		void QueueCommand (SmtpCommand type, string command)
		{
			if (queue == null)
				queue = new MemoryBlockStream ();

			var bytes = Encoding.UTF8.GetBytes (command + "\r\n");
			queue.Write (bytes, 0, bytes.Length);
			queued.Add (type);
		}

		void FlushCommandQueue (MailboxAddress sender, IList<MailboxAddress> recipients, CancellationToken cancellationToken)
		{
			if (queued.Count == 0)
				return;

			try {
				var responses = new List<SmtpResponse> ();
				int rcpt = 0;
				int nread;

				queue.Position = 0;
				while ((nread = queue.Read (input, 0, input.Length)) > 0) {
					cancellationToken.ThrowIfCancellationRequested ();
					stream.Write (input, 0, nread);
					logger.LogClient (input, 0, nread);
				}

				// Note: we need to read all responses from the server before we can process
				// them in case any of them have any errors so that we can RSET the state.
				for (int i = 0; i < queued.Count; i++)
					responses.Add (ReadResponse (cancellationToken));

				for (int i = 0; i < queued.Count; i++) {
					switch (queued [i]) {
					case SmtpCommand.MailFrom:
						ProcessMailFromResponse (responses[i], sender);
						break;
					case SmtpCommand.RcptTo:
						ProcessRcptToResponse (responses[i], recipients[rcpt++]);
						break;
					}
				}
			} finally {
				queue.SetLength (0);
				queued.Clear ();
			}
		}

		SmtpResponse SendCommand (string command, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			var bytes = Encoding.UTF8.GetBytes (command + "\r\n");
			stream.Write (bytes, 0, bytes.Length);
			logger.LogClient (bytes, 0, bytes.Length);

			return ReadResponse (cancellationToken);
		}

		SmtpResponse SendEhlo (bool ehlo, CancellationToken cancellationToken)
		{
			string command = ehlo ? "EHLO " : "HELO ";

#if !NETFX_CORE
			var ip = localEndPoint as IPEndPoint;

			if (ip != null) {
				command += "[";
				if (localEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
					command += "IPv6:";
				command += ip.Address;
				command += "]";
			} else {
				command += ((DnsEndPoint) localEndPoint).Host;
			}
#else
			if (socket.Information.LocalAddress.IPInformation != null)
				command += "[" + socket.Information.LocalAddress.IPInformation + "]";
			else
				command += socket.Information.LocalAddress.CanonicalName;
#endif

			return SendCommand (command, cancellationToken);
		}

		void Ehlo (CancellationToken cancellationToken)
		{
			SmtpResponse response;

			response = SendEhlo (true, cancellationToken);

			// Some SMTP servers do not accept an EHLO after authentication (despite the rfc saying it is required).
			if (response.StatusCode == SmtpStatusCode.BadCommandSequence && capabilities != SmtpCapabilities.None)
				return;

			if (response.StatusCode != SmtpStatusCode.Ok) {
				// Try sending HELO instead...
				response = SendEhlo (false, cancellationToken);
				if (response.StatusCode != SmtpStatusCode.Ok)
					throw new SmtpCommandException (SmtpErrorCode.UnexpectedStatusCode, response.StatusCode, response.Response);
			} else {
				// Clear the extensions
				capabilities = SmtpCapabilities.None;
				authmechs.Clear();
				MaxSize = 0;

				var lines = response.Response.Split ('\n');
				for (int i = 0; i < lines.Length; i++) {
					var capability = lines[i].Trim ();

					if (capability.StartsWith ("AUTH", StringComparison.Ordinal)) {
						int index = 4;

						capabilities |= SmtpCapabilities.Authentication;

						if (index < capability.Length && capability[index] == '=')
							index++;

						var mechanisms = capability.Substring (index);
						foreach (var mechanism in mechanisms.Split (new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
							authmechs.Add (mechanism);
					} else if (capability.StartsWith ("SIZE", StringComparison.Ordinal)) {
						int index = 4;
						uint size;

						capabilities |= SmtpCapabilities.Size;

						while (index < capability.Length && char.IsWhiteSpace (capability[index]))
							index++;

						if (uint.TryParse (capability.Substring (index), out size))
							MaxSize = size;
					} else if (capability == "BINARYMIME") {
						capabilities |= SmtpCapabilities.BinaryMime;
					} else if (capability == "CHUNKING") {
						capabilities |= SmtpCapabilities.Chunking;
					} else if (capability == "ENHANCEDSTATUSCODES") {
						capabilities |= SmtpCapabilities.EnhancedStatusCodes;
					} else if (capability == "8BITMIME") {
						capabilities |= SmtpCapabilities.EightBitMime;
					} else if (capability == "PIPELINING") {
						capabilities |= SmtpCapabilities.Pipelining;
					} else if (capability == "STARTTLS") {
						capabilities |= SmtpCapabilities.StartTLS;
					} else if (capability == "SMTPUTF8") {
						capabilities |= SmtpCapabilities.UTF8;
					}
				}
			}
		}

		/// <summary>
		/// Authenticates using the supplied credentials.
		/// </summary>
		/// <remarks>
		/// <para>If the SMTP server supports authentication, then the SASL mechanisms
		/// that both the client and server support are tried in order of greatest
		/// security to weakest security. Once a SASL authentication mechanism is
		/// found that both client and server support, the credentials are used to
		/// authenticate.</para>
		/// <para>If, on the other hand, authentication is not supported, then
		/// this method simply returns without attempting to authenticate.</para>
		/// </remarks>
		/// <param name="credentials">The user's credentials.</param>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="credentials"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="SmtpClient"/> is not connected or is already authenticated.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The SMTP server does not support authentication.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="MailKit.Security.AuthenticationException">
		/// Authentication using the supplied credentials has failed.
		/// </exception>
		/// <exception cref="MailKit.Security.SaslException">
		/// A SASL authentication error occurred.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="SmtpCommandException">
		/// The SMTP command failed.
		/// </exception>
		/// <exception cref="SmtpProtocolException">
		/// An SMTP protocol error occurred.
		/// </exception>
		public void Authenticate (ICredentials credentials, CancellationToken cancellationToken = default (CancellationToken))
		{
			if (!IsConnected)
				throw new InvalidOperationException ("The SmtpClient must be connected before you can authenticate.");

			if (authenticated)
				throw new InvalidOperationException ("The SmtpClient is already authenticated.");

			if ((capabilities & SmtpCapabilities.Authentication) == 0)
				throw new NotSupportedException ("The SMTP server does not support authentication.");

			if (credentials == null)
				throw new ArgumentNullException ("credentials");

			var uri = new Uri ("smtp://" + host);
			SaslException authException = null;
			SmtpResponse response;
			bool tried = false;
			string challenge;
			string command;

			foreach (var authmech in SaslMechanism.AuthMechanismRank) {
				if (!AuthenticationMechanisms.Contains (authmech))
					continue;

				var sasl = SaslMechanism.Create (authmech, uri, credentials);
				tried = true;

				cancellationToken.ThrowIfCancellationRequested ();

				// send an initial challenge if the mechanism supports it
				if ((challenge = sasl.Challenge (null)) != null)
					command = string.Format ("AUTH {0} {1}", authmech, challenge);
				else
					command = string.Format ("AUTH {0}", authmech);

				response = SendCommand (command, cancellationToken);

				if (response.StatusCode == SmtpStatusCode.AuthenticationMechanismTooWeak)
					continue;

				try {
					while (!sasl.IsAuthenticated) {
						if (response.StatusCode != SmtpStatusCode.AuthenticationChallenge)
							throw new SmtpCommandException (SmtpErrorCode.UnexpectedStatusCode, response.StatusCode, response.Response);

						challenge = sasl.Challenge (response.Response);
						response = SendCommand (challenge, cancellationToken);
					}
				} catch (SaslException ex) {
					// reset the authentication state
					response = SendCommand (string.Empty, cancellationToken);
					authException = ex;
				}

				if (response.StatusCode == SmtpStatusCode.AuthenticationSuccessful) {
					Ehlo (cancellationToken);
					authenticated = true;
					return;
				}
			}

			if (tried)
				throw authException ?? new AuthenticationException ();

			throw new NotSupportedException ("No compatible authentication mechanisms found.");
		}

		internal void ReplayConnect (string hostName, Stream replayStream, CancellationToken cancellationToken)
		{
			CheckDisposed ();

			if (hostName == null)
				throw new ArgumentNullException ("hostName");

			if (replayStream == null)
				throw new ArgumentNullException ("replayStream");

#if !NETFX_CORE
			localEndPoint = new IPEndPoint (IPAddress.Loopback, 25);
#endif
			capabilities = SmtpCapabilities.None;
			stream = replayStream;
			authmechs.Clear ();
			host = hostName;
			MaxSize = 0;

			try {
				// read the greeting
				var response = ReadResponse (cancellationToken);

				if (response.StatusCode != SmtpStatusCode.ServiceReady)
					throw new SmtpCommandException (SmtpErrorCode.UnexpectedStatusCode, response.StatusCode, response.Response);

				// Send EHLO and get a list of supported extensions
				Ehlo (cancellationToken);

				IsConnected = true;
			} catch {
				stream.Dispose ();
				stream = null;
				throw;
			}
		}

		/// <summary>
		/// Establishes a connection to the specified SMTP server.
		/// </summary>
		/// <remarks>
		/// <para>Establishes a connection to an SMTP or SMTP/S server. If the schema
		/// in the uri is "smtp", a clear-text connection is made and defaults to using
		/// port 25 if no port is specified in the URI. However, if the schema in the
		/// uri is "smtps", an SSL connection is made using the
		/// <see cref="ClientCertificates"/> and defaults to port 465 unless a port
		/// is specified in the URI.</para>
		/// <para>It should be noted that when using a clear-text SMTP connection,
		/// if the server advertizes support for the STARTTLS extension, the client
		/// will automatically switch into TLS mode before authenticating unless the
		/// <paramref name="uri"/> contains a query string to disable it.</para>
		/// If a successful connection is made, the <see cref="AuthenticationMechanisms"/>
		/// and <see cref="Capabilities"/> properties will be populated.
		/// </remarks>
		/// <param name="uri">The server URI. The <see cref="System.Uri.Scheme"/> should either
		/// be "smtp" to make a clear-text connection or "smtps" to make an SSL connection.</param>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para>The <paramref name="uri"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// The <paramref name="uri"/> is not an absolute URI.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="SmtpClient"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="SmtpClient"/> is already connected.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="SmtpCommandException">
		/// An SMTP command failed.
		/// </exception>
		/// <exception cref="SmtpProtocolException">
		/// An SMTP protocol error occurred.
		/// </exception>
		public void Connect (Uri uri, CancellationToken cancellationToken = default (CancellationToken))
		{
			CheckDisposed ();

			if (uri == null)
				throw new ArgumentNullException ("uri");

			if (!uri.IsAbsoluteUri)
				throw new ArgumentException ("The uri must be absolute.", "uri");

			if (IsConnected)
				throw new InvalidOperationException ("The SmtpClient is already connected.");

			capabilities = SmtpCapabilities.None;
			authmechs.Clear ();
			MaxSize = 0;

			var smtps = uri.Scheme.ToLowerInvariant () == "smtps";
			var port = uri.Port > 0 ? uri.Port : (smtps ? 465 : 25);
			var query = uri.ParsedQuery ();
			SmtpResponse response = null;
			string value;

			var starttls = !smtps && (!query.TryGetValue ("starttls", out value) || Convert.ToBoolean (value));

#if !NETFX_CORE
			var ipAddresses = Dns.GetHostAddresses (uri.DnsSafeHost);
			Socket socket = null;

			for (int i = 0; i < ipAddresses.Length; i++) {
				socket = new Socket (ipAddresses[i].AddressFamily, SocketType.Stream, ProtocolType.Tcp);

				cancellationToken.ThrowIfCancellationRequested ();

				try {
					socket.Connect (ipAddresses[i], port);
					localEndPoint = socket.LocalEndPoint;
					break;
				} catch {
					if (i + 1 == ipAddresses.Length)
						throw;
				}
			}

			if (smtps) {
				var ssl = new SslStream (new NetworkStream (socket, true), false, ValidateRemoteCertificate);
				ssl.AuthenticateAsClient (uri.Host, ClientCertificates, SslProtocols.Default, true);
				stream = ssl;
			} else {
				stream = new NetworkStream (socket, true);
			}
#else
			socket = new StreamSocket ();

			cancellationToken.ThrowIfCancellationRequested ();
			socket.ConnectAsync (new HostName (uri.DnsSafeHost), port.ToString (), smtps ? SocketProtectionLevel.Tls12 : SocketProtectionLevel.PlainSocket)
				.AsTask (cancellationToken)
				.GetAwaiter ()
				.GetResult ();

			stream = new DuplexStream (socket.InputStream.AsStreamForRead (), socket.OutputStream.AsStreamForWrite ());
#endif

			host = uri.Host;

			logger.LogConnect (uri);

			try {
				// read the greeting
				response = ReadResponse (cancellationToken);

				if (response.StatusCode != SmtpStatusCode.ServiceReady)
					throw new SmtpCommandException (SmtpErrorCode.UnexpectedStatusCode, response.StatusCode, response.Response);

				// Send EHLO and get a list of supported extensions
				Ehlo (cancellationToken);

				if (starttls && (capabilities & SmtpCapabilities.StartTLS) != 0) {
					response = SendCommand ("STARTTLS", cancellationToken);
					if (response.StatusCode != SmtpStatusCode.ServiceReady)
						throw new SmtpCommandException (SmtpErrorCode.UnexpectedStatusCode, response.StatusCode, response.Response);

#if !NETFX_CORE
					var tls = new SslStream (stream, false, ValidateRemoteCertificate);
					tls.AuthenticateAsClient (uri.Host, ClientCertificates, SslProtocols.Tls, true);
					stream = tls;
#else
					socket.UpgradeToSslAsync (SocketProtectionLevel.Tls12, new HostName (uri.DnsSafeHost))
						.AsTask (cancellationToken)
						.GetAwaiter ()
						.GetResult ();
#endif

					// Send EHLO again and get the new list of supported extensions
					Ehlo (cancellationToken);
				}

				IsConnected = true;
			} catch {
				stream.Dispose ();
				stream = null;
				throw;
			}
		}

		/// <summary>
		/// Disconnect the service.
		/// </summary>
		/// <remarks>
		/// If <paramref name="quit"/> is <c>true</c>, a "QUIT" command will be issued in order to disconnect cleanly.
		/// </remarks>
		/// <param name="quit">If set to <c>true</c>, a "QUIT" command will be issued in order to disconnect cleanly.</param>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="SmtpClient"/> has been disposed.
		/// </exception>
		public void Disconnect (bool quit, CancellationToken cancellationToken = default (CancellationToken))
		{
			CheckDisposed ();

			if (!IsConnected)
				return;

			if (quit) {
				try {
					SendCommand ("QUIT", cancellationToken);
				} catch (OperationCanceledException) {
				} catch (SmtpProtocolException) {
				} catch (SmtpCommandException) {
				} catch (IOException) {
				}
			}

			Disconnect ();
		}

		/// <summary>
		/// Pings the SMTP server to keep the connection alive.
		/// </summary>
		/// <remarks>Mail servers, if left idle for too long, will automatically drop the connection.</remarks>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="SmtpClient"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="SmtpClient"/> is not connected.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="SmtpCommandException">
		/// The SMTP command failed.
		/// </exception>
		/// <exception cref="SmtpProtocolException">
		/// An SMTP protocol error occurred.
		/// </exception>
		public void NoOp (CancellationToken cancellationToken = default (CancellationToken))
		{
			CheckDisposed ();

			if (!IsConnected)
				throw new InvalidOperationException ("The SmtpClient is not connected.");

			var response = SendCommand ("NOOP", cancellationToken);

			if (response.StatusCode != SmtpStatusCode.Ok)
				throw new SmtpCommandException (SmtpErrorCode.UnexpectedStatusCode, response.StatusCode, response.Response);
		}

		void Disconnect ()
		{
			authenticated = false;
#if !NETFX_CORE
			localEndPoint = null;
#endif
			IsConnected = false;
			host = null;

			if (stream != null) {
				stream.Dispose ();
				stream = null;
			}

#if NETFX_CORE
			if (socket != null) {
				socket.Dispose ();
				socket = null;
			}
#endif
		}

		#endregion

		#region IMessageTransport implementation

		static MailboxAddress GetMessageSender (MimeMessage message)
		{
			if (message.ResentSender != null)
				return message.ResentSender;

			if (message.ResentFrom.Count > 0)
				return message.ResentFrom.Mailboxes.FirstOrDefault ();

			if (message.Sender != null)
				return message.Sender;

			return message.From.Mailboxes.FirstOrDefault ();
		}

		static IList<MailboxAddress> GetMessageRecipients (MimeMessage message)
		{
			var recipients = new List<MailboxAddress> ();

			if (message.ResentSender != null || message.ResentFrom.Count > 0) {
				recipients.AddRange (message.ResentTo.Mailboxes);
				recipients.AddRange (message.ResentCc.Mailboxes);
				recipients.AddRange (message.ResentBcc.Mailboxes);
			} else {
				recipients.AddRange (message.To.Mailboxes);
				recipients.AddRange (message.Cc.Mailboxes);
				recipients.AddRange (message.Bcc.Mailboxes);
			}

			return recipients;
		}

		[Flags]
		enum SmtpExtension {
			None         = 0,
			EightBitMime = 1 << 0,
			BinaryMime   = 1 << 1
		}

		ContentEncoding GetFinalEncoding (MimePart part)
		{
			if ((capabilities & SmtpCapabilities.BinaryMime) != 0) {
				// no need to re-encode...
				return part.ContentTransferEncoding;
			}

			if ((capabilities & SmtpCapabilities.EightBitMime) != 0) {
				switch (part.ContentTransferEncoding) {
				case ContentEncoding.Default:
				case ContentEncoding.Binary:
					break;
				default:
					// all other Content-Transfer-Encodings are safe to transmit...
					return part.ContentTransferEncoding;
				}
			}

			switch (part.ContentTransferEncoding) {
			case ContentEncoding.EightBit:
			case ContentEncoding.Default:
			case ContentEncoding.Binary:
				break;
			default:
				// all other Content-Transfer-Encodings are safe to transmit...
				return part.ContentTransferEncoding;
			}

			ContentEncoding encoding;

			if ((capabilities & SmtpCapabilities.BinaryMime) != 0)
				encoding = part.GetBestEncoding (EncodingConstraint.None);
			else if ((capabilities & SmtpCapabilities.EightBitMime) != 0)
				encoding = part.GetBestEncoding (EncodingConstraint.EightBit);
			else
				encoding = part.GetBestEncoding (EncodingConstraint.SevenBit);

			if (encoding == ContentEncoding.SevenBit)
				return encoding;

			part.ContentTransferEncoding = encoding;

			return encoding;
		}

		SmtpExtension PrepareMimeEntity (MimeEntity entity)
		{
			if (entity is MessagePart) {
				var message = ((MessagePart) entity).Message;
				return PrepareMimeEntity (message);
			}

			if (entity is Multipart) {
				var extensions = SmtpExtension.None;
				var multipart = (Multipart) entity;

				foreach (var child in multipart)
					extensions |= PrepareMimeEntity (child);

				return extensions;
			}

			switch (GetFinalEncoding ((MimePart) entity)) {
			case ContentEncoding.EightBit:
				// if the server supports the 8BITMIME extension, use it...
				if ((capabilities & SmtpCapabilities.EightBitMime) != 0)
					return SmtpExtension.EightBitMime;

				return SmtpExtension.BinaryMime;
			case ContentEncoding.Binary:
				return SmtpExtension.BinaryMime;
			default:
				return SmtpExtension.None;
			}
		}

		SmtpExtension PrepareMimeEntity (MimeMessage message)
		{
			if (message.Body == null)
				throw new ArgumentException ("Message does not contain a body.");

			return PrepareMimeEntity (message.Body);
		}

		static string GetMailFromCommand (MailboxAddress mailbox, SmtpExtension extensions)
		{
			if ((extensions & SmtpExtension.BinaryMime) != 0)
				return string.Format ("MAIL FROM:<{0}> BODY=BINARYMIME", mailbox.Address);

			if ((extensions & SmtpExtension.EightBitMime) != 0)
				return string.Format ("MAIL FROM:<{0}> BODY=8BITMIME", mailbox.Address);

			return string.Format ("MAIL FROM:<{0}>", mailbox.Address);
		}

		static void ProcessMailFromResponse (SmtpResponse response, MailboxAddress mailbox)
		{
			switch (response.StatusCode) {
			case SmtpStatusCode.Ok:
				break;
			case SmtpStatusCode.MailboxNameNotAllowed:
			case SmtpStatusCode.MailboxUnavailable:
				throw new SmtpCommandException (SmtpErrorCode.SenderNotAccepted, response.StatusCode, mailbox, response.Response);
			case SmtpStatusCode.AuthenticationRequired:
				throw new UnauthorizedAccessException (response.Response);
			default:
				throw new SmtpCommandException (SmtpErrorCode.UnexpectedStatusCode, response.StatusCode, response.Response);
			}
		}

		void MailFrom (MailboxAddress mailbox, SmtpExtension extensions, CancellationToken cancellationToken)
		{
			var command = GetMailFromCommand (mailbox, extensions);

			if ((capabilities & SmtpCapabilities.Pipelining) != 0) {
				QueueCommand (SmtpCommand.MailFrom, command);
				return;
			}

			ProcessMailFromResponse (SendCommand (command, cancellationToken), mailbox);
		}

		static void ProcessRcptToResponse (SmtpResponse response, MailboxAddress mailbox)
		{
			switch (response.StatusCode) {
			case SmtpStatusCode.UserNotLocalWillForward:
			case SmtpStatusCode.Ok:
				break;
			case SmtpStatusCode.UserNotLocalTryAlternatePath:
			case SmtpStatusCode.MailboxNameNotAllowed:
			case SmtpStatusCode.MailboxUnavailable:
			case SmtpStatusCode.MailboxBusy:
				throw new SmtpCommandException (SmtpErrorCode.RecipientNotAccepted, response.StatusCode, mailbox, response.Response);
			case SmtpStatusCode.AuthenticationRequired:
				throw new UnauthorizedAccessException (response.Response);
			default:
				throw new SmtpCommandException (SmtpErrorCode.UnexpectedStatusCode, response.StatusCode, response.Response);
			}
		}

		void RcptTo (MailboxAddress mailbox, CancellationToken cancellationToken)
		{
			var command = string.Format ("RCPT TO:<{0}>", mailbox.Address);

			if ((capabilities & SmtpCapabilities.Pipelining) != 0) {
				QueueCommand (SmtpCommand.RcptTo, command);
				return;
			}

			ProcessRcptToResponse (SendCommand (command, cancellationToken), mailbox);
		}

		void Data (MimeMessage message, CancellationToken cancellationToken)
		{
			var response = SendCommand ("DATA", cancellationToken);

			if (response.StatusCode != SmtpStatusCode.StartMailInput)
				throw new SmtpCommandException (SmtpErrorCode.UnexpectedStatusCode, response.StatusCode, response.Response);

			var options = FormatOptions.Default.Clone ();
			options.NewLineFormat = NewLineFormat.Dos;

			options.HiddenHeaders.Add (HeaderId.ContentLength);
			options.HiddenHeaders.Add (HeaderId.ResentBcc);
			options.HiddenHeaders.Add (HeaderId.Bcc);

			using (var filtered = new FilteredStream (stream)) {
				filtered.Add (new SmtpDataFilter ());
				message.WriteTo (options, filtered, cancellationToken);
				filtered.Flush ();
			}

			stream.Write (EndData, 0, EndData.Length);

			response = ReadResponse (cancellationToken);

			switch (response.StatusCode) {
			default:
				throw new SmtpCommandException (SmtpErrorCode.MessageNotAccepted, response.StatusCode, response.Response);
			case SmtpStatusCode.AuthenticationRequired:
				throw new UnauthorizedAccessException (response.Response);
			case SmtpStatusCode.Ok:
				break;
			}
		}

		void Reset (CancellationToken cancellationToken)
		{
			try {
				var response = SendCommand ("RSET", cancellationToken);
				if (response.StatusCode != SmtpStatusCode.Ok)
					Disconnect (false, cancellationToken);
			} catch (SmtpCommandException) {
				// do not disconnect
			} catch {
				Disconnect ();
			}
		}

		void Send (MimeMessage message, MailboxAddress sender, IList<MailboxAddress> recipients, CancellationToken cancellationToken)
		{
			CheckDisposed ();

			if (!IsConnected)
				throw new InvalidOperationException ("The SmtpClient is not connected.");

			var extensions = PrepareMimeEntity (message);

			try {
				// Note: if PIPELINING is supported, MailFrom() and RcptTo() will
				// queue their commands instead of sending them immediately.
				MailFrom (sender, extensions, cancellationToken);
				foreach (var recipient in recipients)
					RcptTo (recipient, cancellationToken);

				// Note: if PIPELINING is supported, this will flush all outstanding
				// MAIL FROM and RCPT TO commands to the server and then process all
				// of their responses.
				FlushCommandQueue (sender, recipients, cancellationToken);

				Data (message, cancellationToken);
			} catch (UnauthorizedAccessException) {
				// do not disconnect
				throw;
			} catch (SmtpCommandException) {
				Reset (cancellationToken);
				throw;
			} catch {
				Disconnect ();
				throw;
			}
		}

		/// <summary>
		/// Send the specified message.
		/// </summary>
		/// <remarks>
		/// Sends the message by uploading it to an SMTP server.
		/// </remarks>
		/// <param name="message">The message.</param>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="message"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="SmtpClient"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <para>The <see cref="SmtpClient"/> is not connected.</para>
		/// <para>-or-</para>
		/// <para>A sender has not been specified.</para>
		/// <para>-or-</para>
		/// <para>No recipients have been specified.</para>
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation has been canceled.
		/// </exception>
		/// <exception cref="System.UnauthorizedAccessException">
		/// Authentication is required before sending a message.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="SmtpCommandException">
		/// The SMTP command failed.
		/// </exception>
		/// <exception cref="SmtpProtocolException">
		/// An SMTP protocol exception occurred.
		/// </exception>
		public void Send (MimeMessage message, CancellationToken cancellationToken = default (CancellationToken))
		{
			if (message == null)
				throw new ArgumentNullException ("message");

			var recipients = GetMessageRecipients (message);
			var sender = GetMessageSender (message);

			if (sender == null)
				throw new InvalidOperationException ("No sender has been specified.");

			if (recipients.Count == 0)
				throw new InvalidOperationException ("No recipients have been specified.");

			Send (message, sender, recipients, cancellationToken);
		}

		/// <summary>
		/// Send the specified message using the supplied sender and recipients.
		/// </summary>
		/// <remarks>
		/// Sends the message by uploading it to an SMTP server using the supplied sender and recipients.
		/// </remarks>
		/// <param name="message">The message.</param>
		/// <param name="sender">The mailbox address to use for sending the message.</param>
		/// <param name="recipients">The mailbox addresses that should receive the message.</param>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="message"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="sender"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="recipients"/> is <c>null</c>.</para>
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="SmtpClient"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <para>The <see cref="SmtpClient"/> is not connected.</para>
		/// <para>-or-</para>
		/// <para>A sender has not been specified.</para>
		/// <para>-or-</para>
		/// <para>No recipients have been specified.</para>
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation has been canceled.
		/// </exception>
		/// <exception cref="System.UnauthorizedAccessException">
		/// Authentication is required before sending a message.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="SmtpCommandException">
		/// The SMTP command failed.
		/// </exception>
		/// <exception cref="SmtpProtocolException">
		/// An SMTP protocol exception occurred.
		/// </exception>
		public void Send (MimeMessage message, MailboxAddress sender, IEnumerable<MailboxAddress> recipients, CancellationToken cancellationToken = default (CancellationToken))
		{
			if (message == null)
				throw new ArgumentNullException ("message");

			if (sender == null)
				throw new ArgumentNullException ("sender");

			if (recipients == null)
				throw new ArgumentNullException ("recipients");

			var rcpts = recipients.ToList ();

			if (rcpts.Count == 0)
				throw new InvalidOperationException ("No recipients have been specified.");

			Send (message, sender, rcpts, cancellationToken);
		}

		#endregion

		#region IDisposable

		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="SmtpClient"/> and
		/// optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
		/// <c>false</c> to release only the unmanaged resources.</param>
		protected virtual void Dispose (bool disposing)
		{
			if (disposing && !disposed) {
				if (queue != null)
					queue.Dispose ();
				disposed = true;
				Disconnect ();
			}
		}

		/// <summary>
		/// Releases all resource used by the <see cref="MailKit.Net.Smtp.SmtpClient"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose()"/> when you are finished using the <see cref="MailKit.Net.Smtp.SmtpClient"/>. The
		/// <see cref="Dispose()"/> method leaves the <see cref="MailKit.Net.Smtp.SmtpClient"/> in an unusable state. After
		/// calling <see cref="Dispose()"/>, you must release all references to the <see cref="MailKit.Net.Smtp.SmtpClient"/> so
		/// the garbage collector can reclaim the memory that the <see cref="MailKit.Net.Smtp.SmtpClient"/> was occupying.</remarks>
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		#endregion
	}
}
