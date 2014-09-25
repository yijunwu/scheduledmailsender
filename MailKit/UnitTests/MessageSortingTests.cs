﻿//
// MessageSortingTests.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (www.xamarin.com)
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
using System.Collections.Generic;

using NUnit.Framework;

using MimeKit;

using MailKit.Search;
using MailKit;

namespace UnitTests {
	[TestFixture]
	public class MessageSortingTests
	{
		[Test]
		public void TestSorting ()
		{
			var messages = new List<MessageSummary> ();
			IList<MessageSummary> sorted;
			MessageSummary summary;

			summary = new MessageSummary (0);
			summary.Envelope = new Envelope ();
			summary.Envelope.Date = DateTimeOffset.Now;
			summary.Envelope.Subject = "aaaa";
			summary.Envelope.From = InternetAddressList.Parse ("Jeffrey Stedfast <jeff@xamarin.com>");
			summary.Envelope.To = InternetAddressList.Parse ("Jeffrey Stedfast <jeff@xamarin.com>");
			summary.Envelope.Cc = new InternetAddressList ();
			summary.MessageSize = 520;
			messages.Add (summary);

			summary = new MessageSummary (1);
			summary.Envelope = new Envelope ();
			summary.Envelope.Date = DateTimeOffset.Now;
			summary.Envelope.Subject = "bbbb";
			summary.Envelope.From = InternetAddressList.Parse ("Jeffrey Stedfast <jeff@xamarin.com>");
			summary.Envelope.To = InternetAddressList.Parse ("Jeffrey Stedfast <jeff@xamarin.com>");
			summary.Envelope.Cc = new InternetAddressList ();
			summary.MessageSize = 265;
			messages.Add (summary);

			summary = new MessageSummary (2);
			summary.Envelope = new Envelope ();
			summary.Envelope.Date = DateTimeOffset.Now;
			summary.Envelope.Subject = "cccc";
			summary.Envelope.From = InternetAddressList.Parse ("Jeffrey Stedfast <jeff@xamarin.com>");
			summary.Envelope.To = InternetAddressList.Parse ("Jeffrey Stedfast <jeff@xamarin.com>");
			summary.Envelope.Cc = new InternetAddressList ();
			summary.MessageSize = 520;
			messages.Add (summary);

			sorted = MessageSorter.Sort (messages, new [] { OrderBy.Arrival });
			Assert.AreEqual (0, sorted[0].Index, "Sorting by arrival failed.");
			Assert.AreEqual (1, sorted[1].Index, "Sorting by arrival failed.");
			Assert.AreEqual (2, sorted[2].Index, "Sorting by arrival failed.");

			sorted = MessageSorter.Sort (messages, new [] { OrderBy.ReverseArrival });
			Assert.AreEqual (2, sorted[0].Index, "Sorting by reverse arrival failed.");
			Assert.AreEqual (1, sorted[1].Index, "Sorting by reverse arrival failed.");
			Assert.AreEqual (0, sorted[2].Index, "Sorting by reverse arrival failed.");

			sorted = MessageSorter.Sort (messages, new [] { OrderBy.Subject });
			Assert.AreEqual (0, sorted[0].Index, "Sorting by subject failed.");
			Assert.AreEqual (1, sorted[1].Index, "Sorting by subject failed.");
			Assert.AreEqual (2, sorted[2].Index, "Sorting by subject failed.");

			sorted = MessageSorter.Sort (messages, new [] { OrderBy.ReverseSubject });
			Assert.AreEqual (2, sorted[0].Index, "Sorting by reverse subject failed.");
			Assert.AreEqual (1, sorted[1].Index, "Sorting by reverse subject failed.");
			Assert.AreEqual (0, sorted[2].Index, "Sorting by reverse subject failed.");

			sorted = MessageSorter.Sort (messages, new [] { OrderBy.Size });
			Assert.AreEqual (1, sorted[0].Index, "Sorting by size failed.");

			sorted = MessageSorter.Sort (messages, new [] { OrderBy.Size, OrderBy.Subject });
			Assert.AreEqual (1, sorted[0].Index, "Sorting by size+subject failed.");
			Assert.AreEqual (0, sorted[1].Index, "Sorting by size+subject failed.");
			Assert.AreEqual (2, sorted[2].Index, "Sorting by size+subject failed.");

			sorted = MessageSorter.Sort (messages, new [] { OrderBy.ReverseSize, OrderBy.ReverseSubject });
			Assert.AreEqual (2, sorted[0].Index, "Sorting by size+subject failed.");
			Assert.AreEqual (0, sorted[1].Index, "Sorting by size+subject failed.");
			Assert.AreEqual (1, sorted[2].Index, "Sorting by size+subject failed.");
		}
	}
}
