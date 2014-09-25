﻿//
// StatusItems.cs
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

namespace MailKit {
	/// <summary>
	/// Status items.
	/// </summary>
	/// <remarks>
	/// Used with <see cref="IFolder.Status(StatusItems,System.Threading.CancellationToken)"/>
	/// </remarks>
	[Flags]
	public enum StatusItems {
		/// <summary>
		/// Updates <see cref="IFolder.Count"/>.
		/// </summary>
		Count          = 1 << 0,

		/// <summary>
		/// Updates <see cref="IFolder.Recent"/>.
		/// </summary>
		Recent         = 1 << 1,

		/// <summary>
		/// Updates <see cref="IFolder.UidNext"/>.
		/// </summary>
		UidNext        = 1 << 2,

		/// <summary>
		/// Updates <see cref="IFolder.UidValidity"/>.
		/// </summary>
		UidValidity    = 1 << 3,

		/// <summary>
		/// Updates <see cref="IFolder.FirstUnread"/>.
		/// </summary>
		FirstUnread    = 1 << 4,

		/// <summary>
		/// Updates <see cref="IFolder.HighestModSeq"/>.
		/// </summary>
		HighestModSeq  = 1 << 5,
	}
}
