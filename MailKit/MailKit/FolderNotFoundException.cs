﻿//
// FolderNotFoundException.cs
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
#if !NETFX_CORE
using System.Runtime.Serialization;
#endif

namespace MailKit {
	/// <summary>
	/// The exception that is thrown when a folder could not be found.
	/// </summary>
	/// <remarks>
	/// This exception is thrown by <see cref="IFolder.GetSubfolder(string,System.Threading.CancellationToken)"/>.
	/// </remarks>
#if !NETFX_CORE
	[Serializable]
#endif
	public class FolderNotFoundException : Exception
	{
#if !NETFX_CORE
		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.FolderNotFoundException"/> class.
		/// </summary>
		/// <param name="info">The serialization info.</param>
		/// <param name="context">The streaming context.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="info"/> is <c>null</c>.
		/// </exception>
		protected FolderNotFoundException (SerializationInfo info, StreamingContext context) : base (info, context)
		{
			if (info == null)
				throw new ArgumentNullException ("info");

			FolderName = info.GetString ("FolderName");
		}
#endif

		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.FolderNotFoundException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="folderName">The name of the folder.</param>
		/// <param name="innerException">The inner exception.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="folderName"/> is <c>null</c>.
		/// </exception>
		public FolderNotFoundException (string message, string folderName, Exception innerException) : base (message, innerException)
		{
			if (folderName == null)
				throw new ArgumentNullException ("folderName");

			FolderName = folderName;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.FolderNotFoundException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="folderName">The name of the folder.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="folderName"/> is <c>null</c>.
		/// </exception>
		public FolderNotFoundException (string message, string folderName) : base (message)
		{
			if (folderName == null)
				throw new ArgumentNullException ("folderName");

			FolderName = folderName;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.FolderNotFoundException"/> class.
		/// </summary>
		/// <param name="folderName">The name of the folder.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="folderName"/> is <c>null</c>.
		/// </exception>
		public FolderNotFoundException (string folderName) : base ("The requested folder could not be found.")
		{
			if (folderName == null)
				throw new ArgumentNullException ("folderName");

			FolderName = folderName;
		}

		/// <summary>
		/// Gets the name of the folder that could not be found.
		/// </summary>
		/// <value>The name of the folder.</value>
		public string FolderName {
			get; private set;
		}
	}
}
