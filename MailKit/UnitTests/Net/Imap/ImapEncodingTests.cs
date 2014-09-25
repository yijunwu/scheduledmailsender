﻿//
// ImapEncodingTests.cs
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
using System.Text;

using NUnit.Framework;

using MailKit.Net.Imap;

namespace UnitTests.Net.Imap {
	[TestFixture]
	public class ImapEncodingTests
	{
		[Test]
		public void TestArabicExample ()
		{
			const string arabic = "هل تتكلم اللغة الإنجليزية /العربية؟";

			var encoded = ImapEncoding.Encode (arabic);
			Assert.AreEqual ("&BkcGRA- &BioGKgZDBkQGRQ- &BicGRAZEBjoGKQ- &BicGRAYlBkYGLAZEBkoGMgZKBik- /&BicGRAY5BjEGKAZKBikGHw-", encoded, "UTF-7 encoded text does not match the expected value: {0}", encoded);

			var decoded = ImapEncoding.Decode (encoded);
			Assert.AreEqual (arabic, decoded, "UTF-7 decoded text does not match the original text: {0}", decoded);
		}

		[Test]
		public void TestJapaneseExample ()
		{
			const string japanese = "狂ったこの世で狂うなら気は確かだ。";

			var encoded = ImapEncoding.Encode (japanese);
			Assert.AreEqual ("&csIwYzBfMFMwbk4WMGdywjBGMGowiWwXMG94ujBLMGAwAg-", encoded, "UTF-7 encoded text does not match the expected value: {0}", encoded);

			var decoded = ImapEncoding.Decode (encoded);
			Assert.AreEqual (japanese, decoded, "UTF-7 decoded text does not match the original text: {0}", decoded);
		}
	}
}
