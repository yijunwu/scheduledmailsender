﻿//
// ImapBodyParsingTests.cs
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
using System.Linq;
using System.Text;
using System.Threading;

using NUnit.Framework;

using MimeKit;
using MimeKit.Utils;

using MailKit.Net.Imap;
using MailKit;

namespace UnitTests.Net.Imap {
	[TestFixture]
	public class ImapUtilsTests
	{
		[Test]
		public void TestFormattingSimpleUidRange ()
		{
			UniqueId[] uids = new UniqueId[] {
				new UniqueId (1), new UniqueId (2), new UniqueId (3),
				new UniqueId (4), new UniqueId (5), new UniqueId (6),
				new UniqueId (7), new UniqueId (8), new UniqueId (9)
			};
			string expect = "1:9";
			string actual;

			actual = ImapUtils.FormatUidSet (uids);
			Assert.AreEqual (expect, actual, "Formatting a simple range of uids failed.");
		}

		[Test]
		public void TestFormattingNonSequentialUids ()
		{
			UniqueId[] uids = new UniqueId[] {
				new UniqueId (1), new UniqueId (3), new UniqueId (5),
				new UniqueId (7), new UniqueId (9)
			};
			string expect = "1,3,5,7,9";
			string actual;

			actual = ImapUtils.FormatUidSet (uids);
			Assert.AreEqual (expect, actual, "Formatting a non-sequential list of uids.");
		}

		[Test]
		public void TestFormattingComplexSetOfUids ()
		{
			UniqueId[] uids = new UniqueId[] {
				new UniqueId (1), new UniqueId (2), new UniqueId (3),
				new UniqueId (5), new UniqueId (6), new UniqueId (9),
				new UniqueId (10), new UniqueId (11), new UniqueId (12),
				new UniqueId (15), new UniqueId (19), new UniqueId (20)
			};
			string expect = "1:3,5:6,9:12,15,19:20";
			string actual;

			actual = ImapUtils.FormatUidSet (uids);
			Assert.AreEqual (expect, actual, "Formatting a complex list of uids.");
		}

		[Test]
		public void TestParseExampleBodyRfc3501 ()
		{
			const string text = "(\"TEXT\" \"PLAIN\" (\"CHARSET\" \"US-ASCII\") NIL NIL \"7BIT\" 3028 92)\r\n";

			using (var memory = new MemoryStream (Encoding.ASCII.GetBytes (text), false)) {
				using (var tokenizer = new ImapStream (memory, new NullProtocolLogger ())) {
					using (var engine = new ImapEngine ()) {
						BodyPartText basic;
						BodyPart body;

						engine.SetStream (tokenizer);

						try {
							body = ImapUtils.ParseBody (engine, string.Empty, CancellationToken.None);
						} catch (Exception ex) {
							Assert.Fail ("Parsing BODY failed: {0}", ex);
							return;
						}

						var token = engine.ReadToken (CancellationToken.None);
						Assert.AreEqual (ImapTokenType.Eoln, token.Type, "Expected new-line, but got: {0}", token);

						Assert.IsInstanceOfType (typeof (BodyPartText), body, "Body types did not match.");
						basic = (BodyPartText) body;

						Assert.IsTrue (body.ContentType.Matches ("text", "plain"), "Content-Type did not match.");
						Assert.AreEqual ("US-ASCII", body.ContentType.Parameters["charset"], "charset param did not match");

						Assert.IsNotNull (basic, "The parsed body is not BodyPartText.");
						Assert.AreEqual ("7BIT", basic.ContentTransferEncoding, "Content-Transfer-Encoding did not match.");
						Assert.AreEqual (3028, basic.Octets, "Octet count did not match.");
						Assert.AreEqual (92, basic.Lines, "Line count did not match.");
					}
				}
			}
		}

		[Test]
		public void TestParseExampleEnvelopeRfc3501 ()
		{
			const string text = "(\"Wed, 17 Jul 1996 02:23:25 -0700 (PDT)\" \"IMAP4rev1 WG mtg summary and minutes\" ((\"Terry Gray\" NIL \"gray\" \"cac.washington.edu\")) ((\"Terry Gray\" NIL \"gray\" \"cac.washington.edu\")) ((\"Terry Gray\" NIL \"gray\" \"cac.washington.edu\")) ((NIL NIL \"imap\" \"cac.washington.edu\")) ((NIL NIL \"minutes\" \"CNRI.Reston.VA.US\") (\"John Klensin\" NIL \"KLENSIN\" \"MIT.EDU\")) NIL NIL \"<B27397-0100000@cac.washington.edu>\")\r\n";

			using (var memory = new MemoryStream (Encoding.ASCII.GetBytes (text), false)) {
				using (var tokenizer = new ImapStream (memory, new NullProtocolLogger ())) {
					using (var engine = new ImapEngine ()) {
						Envelope envelope;

						engine.SetStream (tokenizer);

						try {
							envelope = ImapUtils.ParseEnvelope (engine, CancellationToken.None);
						} catch (Exception ex) {
							Assert.Fail ("Parsing ENVELOPE failed: {0}", ex);
							return;
						}

						var token = engine.ReadToken (CancellationToken.None);
						Assert.AreEqual (ImapTokenType.Eoln, token.Type, "Expected new-line, but got: {0}", token);

						Assert.IsTrue (envelope.Date.HasValue, "Parsed ENVELOPE date is null.");
						Assert.AreEqual ("Wed, 17 Jul 1996 02:23:25 -0700", DateUtils.FormatDate (envelope.Date.Value), "Date does not match.");
						Assert.AreEqual ("IMAP4rev1 WG mtg summary and minutes", envelope.Subject, "Subject does not match.");

						Assert.AreEqual (1, envelope.From.Count, "From counts do not match.");
						Assert.AreEqual ("\"Terry Gray\" <gray@cac.washington.edu>", envelope.From.ToString (), "From does not match.");

						Assert.AreEqual (1, envelope.Sender.Count, "Sender counts do not match.");
						Assert.AreEqual ("\"Terry Gray\" <gray@cac.washington.edu>", envelope.Sender.ToString (), "Sender does not match.");

						Assert.AreEqual (1, envelope.ReplyTo.Count, "Reply-To counts do not match.");
						Assert.AreEqual ("\"Terry Gray\" <gray@cac.washington.edu>", envelope.ReplyTo.ToString (), "Reply-To does not match.");

						Assert.AreEqual (1, envelope.To.Count, "To counts do not match.");
						Assert.AreEqual ("imap@cac.washington.edu", envelope.To.ToString (), "To does not match.");

						Assert.AreEqual (2, envelope.Cc.Count, "Cc counts do not match.");
						Assert.AreEqual ("minutes@CNRI.Reston.VA.US, \"John Klensin\" <KLENSIN@MIT.EDU>", envelope.Cc.ToString (), "Cc does not match.");

						Assert.AreEqual (0, envelope.Bcc.Count, "Bcc counts do not match.");

						Assert.IsNull (envelope.InReplyTo, "In-Reply-To is not null.");

						Assert.AreEqual ("B27397-0100000@cac.washington.edu", envelope.MessageId, "Message-Id does not match.");
					}
				}
			}
		}

		[Test]
		public void TestParseExampleMultiLevelDovecotBodyStructure ()
		{
			const string text = "(((\"text\" \"plain\" (\"charset\" \"iso-8859-2\") NIL NIL \"quoted-printable\" 28 2 NIL NIL NIL NIL) (\"text\" \"html\" (\"charset\" \"iso-8859-2\") NIL NIL \"quoted-printable\" 1707 65 NIL NIL NIL NIL) \"alternative\" (\"boundary\" \"----=_NextPart_001_0078_01CBB179.57530990\") NIL NIL NIL) (\"message\" \"rfc822\" NIL NIL NIL \"7bit\" 641 (\"Sat, 8 Jan 2011 14:16:36 +0100\" \"Subj 2\" ((\"Some Name, SOMECOMPANY\" NIL \"recipient\" \"example.com\")) ((\"Some Name, SOMECOMPANY\" NIL \"recipient\" \"example.com\")) ((\"Some Name, SOMECOMPANY\" NIL \"recipient\" \"example.com\")) ((\"Recipient\" NIL \"example\" \"gmail.com\")) NIL NIL NIL NIL) (\"text\" \"plain\" (\"charset\" \"iso-8859-2\") NIL NIL \"quoted-printable\" 185 18 NIL NIL (\"cs\") NIL) 31 NIL (\"attachment\" NIL) NIL NIL) (\"message\" \"rfc822\" NIL NIL NIL \"7bit\" 50592 (\"Sat, 8 Jan 2011 13:58:39 +0100\" \"Subj 1\" ((\"Some Name, SOMECOMPANY\" NIL \"recipient\" \"example.com\")) ((\"Some Name, SOMECOMPANY\" NIL \"recipient\" \"example.com\")) ((\"Some Name, SOMECOMPANY\" NIL \"recipient\" \"example.com\")) ((\"Recipient\" NIL \"example\" \"gmail.com\")) NIL NIL NIL NIL) ( (\"text\" \"plain\" (\"charset\" \"iso-8859-2\") NIL NIL \"quoted-printable\" 4296 345 NIL NIL NIL NIL) (\"text\" \"html\" (\"charset\" \"iso-8859-2\") NIL NIL \"quoted-printable\" 45069 1295 NIL NIL NIL NIL) \"alternative\" (\"boundary\" \"----=_NextPart_000_0073_01CBB179.57530990\") NIL (\"cs\") NIL) 1669 NIL (\"attachment\" NIL) NIL NIL) \"mixed\" (\"boundary\" \"----=_NextPart_000_0077_01CBB179.57530990\") NIL (\"cs\") NIL)\r\n";

			using (var memory = new MemoryStream (Encoding.ASCII.GetBytes (text), false)) {
				using (var tokenizer = new ImapStream (memory, new NullProtocolLogger ())) {
					using (var engine = new ImapEngine ()) {
						BodyPartMultipart multipart;
						BodyPart body;

						engine.SetStream (tokenizer);

						try {
							body = ImapUtils.ParseBody (engine, string.Empty, CancellationToken.None);
						} catch (Exception ex) {
							Assert.Fail ("Parsing BODYSTRUCTURE failed: {0}", ex);
							return;
						}

						var token = engine.ReadToken (CancellationToken.None);
						Assert.AreEqual (ImapTokenType.Eoln, token.Type, "Expected new-line, but got: {0}", token);

						Assert.IsInstanceOfType (typeof (BodyPartMultipart), body, "Body types did not match.");
						multipart = (BodyPartMultipart) body;

						Assert.IsTrue (body.ContentType.Matches ("multipart", "mixed"), "Content-Type did not match.");
						Assert.AreEqual ("----=_NextPart_000_0077_01CBB179.57530990", body.ContentType.Parameters["boundary"], "boundary param did not match");
						Assert.AreEqual (3, multipart.BodyParts.Count, "BodyParts count does not match.");
						Assert.IsInstanceOfType (typeof (BodyPartMultipart), multipart.BodyParts[0], "The type of the first child does not match.");
						Assert.IsInstanceOfType (typeof (BodyPartMessage), multipart.BodyParts[1], "The type of the second child does not match.");
						Assert.IsInstanceOfType (typeof (BodyPartMessage), multipart.BodyParts[2], "The type of the third child does not match.");

						// FIXME: assert more stuff?
					}
				}
			}
		}

		[Test]
		public void TestParseExampleThreads ()
		{
			const string text = "(2)(3 6 (4 23)(44 7 96))\r\n";

			using (var memory = new MemoryStream (Encoding.ASCII.GetBytes (text), false)) {
				using (var tokenizer = new ImapStream (memory, new NullProtocolLogger ())) {
					using (var engine = new ImapEngine ()) {
						MessageThread[] threads;

						engine.SetStream (tokenizer);

						try {
							threads = ImapUtils.ParseThreads (engine, CancellationToken.None);
						} catch (Exception ex) {
							Assert.Fail ("Parsing THREAD response failed: {0}", ex);
							return;
						}

						var token = engine.ReadToken (CancellationToken.None);
						Assert.AreEqual (ImapTokenType.Eoln, token.Type, "Expected new-line, but got: {0}", token);

						Assert.AreEqual (2, threads.Length, "Expected 2 threads.");

						Assert.AreEqual ((uint) 2, threads[0].UniqueId.Value.Id);
						Assert.AreEqual ((uint) 3, threads[1].UniqueId.Value.Id);

						var branches = threads[1].Children.ToArray ();
						Assert.AreEqual (1, branches.Length, "Expected 1 child.");
						Assert.AreEqual ((uint) 6, branches[0].UniqueId.Value.Id);

						branches = branches[0].Children.ToArray ();
						Assert.AreEqual (2, branches.Length, "Expected 2 branches.");

						Assert.AreEqual ((uint) 4, branches[0].UniqueId.Value.Id);
						Assert.AreEqual ((uint) 44, branches[1].UniqueId.Value.Id);

						var children = branches[0].Children.ToArray ();
						Assert.AreEqual (1, children.Length, "Expected 1 child.");
						Assert.AreEqual ((uint) 23, children[0].UniqueId.Value.Id);
						Assert.AreEqual (0, children[0].Children.Count (), "Expected no children.");

						children = branches[1].Children.ToArray ();
						Assert.AreEqual (1, children.Length, "Expected 1 child.");
						Assert.AreEqual ((uint) 7, children[0].UniqueId.Value.Id);

						children = children[0].Children.ToArray ();
						Assert.AreEqual (1, children.Length, "Expected 1 child.");
						Assert.AreEqual ((uint) 96, children[0].UniqueId.Value.Id);
						Assert.AreEqual (0, children[0].Children.Count (), "Expected no children.");
					}
				}
			}
		}
	}
}
