//
// IFolder.cs
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
using System.Threading;
using System.Collections.Generic;

using MimeKit;
using MailKit.Search;

namespace MailKit {
	/// <summary>
	/// An interface for a mailbox folder as used by <see cref="IMessageStore"/>.
	/// </summary>
	/// <remarks>
	/// Implemented by message stores such as <see cref="MailKit.Net.Imap.ImapClient"/>
	/// </remarks>
	public interface IFolder : IEnumerable<MimeMessage>
	{
		/// <summary>
		/// Gets the parent folder.
		/// </summary>
		/// <remarks>
		/// Root-level folders do not have a parent folder.
		/// </remarks>
		/// <value>The parent folder.</value>
		IFolder ParentFolder { get; }

		/// <summary>
		/// Gets the folder attributes.
		/// </summary>
		/// <value>The folder attributes.</value>
		FolderAttributes Attributes { get; }

		/// <summary>
		/// Gets the permanent flags.
		/// </summary>
		/// <remarks>
		/// The permanent flags are the message flags that will persist between sessions.
		/// </remarks>
		/// <value>The permanent flags.</value>
		MessageFlags PermanentFlags { get; }

		/// <summary>
		/// Gets the accepted flags.
		/// </summary>
		/// <remarks>
		/// The accepted flags are the message flags that will be accepted and persist
		/// for the current session. For the set of flags that will persist between
		/// sessions, see the <see cref="PermanentFlags"/> property.
		/// </remarks>
		/// <value>The accepted flags.</value>
		MessageFlags AcceptedFlags { get; }

		/// <summary>
		/// Gets the directory separator.
		/// </summary>
		/// <value>The directory separator.</value>
		char DirectorySeparator { get; }

		/// <summary>
		/// Gets the read/write access of the folder.
		/// </summary>
		/// <value>The read/write access.</value>
		FolderAccess Access { get; }

		/// <summary>
		/// Gets whether or not the folder is a namespace folder.
		/// </summary>
		/// <value><c>true</c> if the folder is a namespace folder; otherwise, <c>false</c>.</value>
		bool IsNamespace { get; }

		/// <summary>
		/// Gets the full name of the folder.
		/// </summary>
		/// <remarks>
		/// This is the equivalent of the full path of a file on a file system.
		/// </remarks>
		/// <value>The full name of the folder.</value>
		string FullName { get; }

		/// <summary>
		/// Gets the name of the folder.
		/// </summary>
		/// <remarks>
		/// This is the equivalent of the file name of a file on the file system.
		/// </remarks>
		/// <value>The name of the folder.</value>
		string Name { get; }

		/// <summary>
		/// Gets a value indicating whether the folder is subscribed.
		/// </summary>
		/// <value><c>true</c> if the folder is subscribed; otherwise, <c>false</c>.</value>
		bool IsSubscribed { get; }

		/// <summary>
		/// Gets a value indicating whether the folder is currently open.
		/// </summary>
		/// <value><c>true</c> if the folder is currently open; otherwise, <c>false</c>.</value>
		bool IsOpen { get; }

		/// <summary>
		/// Gets a value indicating whether the folder exists.
		/// </summary>
		/// <value><c>true</c> if the folder exists; otherwise, <c>false</c>.</value>
		bool Exists { get; }

		/// <summary>
		/// Gets whether or not the folder supports mod-sequences.
		/// </summary>
		/// <remarks>
		/// If mod-sequences are not supported by the folder, then all of the APIs that take a modseq
		/// argument will throw <see cref="System.NotSupportedException"/> and should not be used.
		/// </remarks>
		/// <value><c>true</c> if supports mod-sequences; otherwise, <c>false</c>.</value>
		bool SupportsModSeq { get; }

		/// <summary>
		/// Gets the highest mod-sequence value of all messages in the mailbox.
		/// </summary>
		/// <remarks>
		/// This property is only available if the IMAP server supports the CONDSTORE extension.
		/// </remarks>
		/// <value>The highest mod-sequence value.</value>
		ulong HighestModSeq { get; }

		/// <summary>
		/// Gets the Unique ID validity.
		/// </summary>
		/// <remarks>
		/// <para>UIDs are only valid so long as the UID validity value remains unchanged. If and when
		/// the folder's <see cref="UidValidity"/> is changed, a client MUST discard its cache of UIDs
		/// along with any summary information that it may have and re-query the folder.</para>
		/// <para>The <see cref="UidValidity"/> will only be set after the folder has been opened.</para>
		/// </remarks>
		/// <value>The UID validity.</value>
		UniqueId? UidValidity { get; }

		/// <summary>
		/// Gets the UID that the next message that is added to the folder will be assigned.
		/// </summary>
		/// <remarks>
		/// This value will only be set after the folder has been opened.
		/// </remarks>
		/// <value>The next UID.</value>
		UniqueId? UidNext { get; }

		/// <summary>
		/// Gets the index of the first unread message in the folder.
		/// </summary>
		/// <remarks>
		/// This value will only be set after the folder has been opened.
		/// </remarks>
		/// <value>The index of the first unread message.</value>
		int FirstUnread { get; }

		/// <summary>
		/// Gets the number of recently added messages.
		/// </summary>
		/// <value>The number of recently added messages.</value>
		int Recent { get; }

		/// <summary>
		/// Gets the total number of messages in the folder.
		/// </summary>
		/// <value>The total number of messages.</value>
		int Count { get; }

		/// <summary>
		/// Opens the folder using the requested folder access.
		/// </summary>
		/// <returns>The <see cref="FolderAccess"/> state of the folder.</returns>
		/// <param name="access">The requested folder access.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		FolderAccess Open (FolderAccess access, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Closes the folder, optionally expunging the messages marked for deletion.
		/// </summary>
		/// <param name="expunge">If set to <c>true</c>, expunge.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		void Close (bool expunge, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Creates a new subfolder with the given name.
		/// </summary>
		/// <returns>The created folder.</returns>
		/// <param name="name">The name of the folder to create.</param>
		/// <param name="isMessageFolder"><c>true</c> if the folder will be used to contain messages; otherwise <c>false</c>.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		IFolder Create (string name, bool isMessageFolder, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Renames the folder to exist with a new name under a new parent folder.
		/// </summary>
		/// <param name="parent">The new parent folder.</param>
		/// <param name="name">The new name of the folder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		void Rename (IFolder parent, string name, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Deletes the folder on the IMAP server.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		void Delete (CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Subscribes the folder.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		void Subscribe (CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Unsubscribes the folder.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		void Unsubscribe (CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Gets the subfolders.
		/// </summary>
		/// <returns>The subfolders.</returns>
		/// <param name="subscribedOnly">If set to <c>true</c>, only subscribed folders will be listed.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		IEnumerable<IFolder> GetSubfolders (bool subscribedOnly, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Gets the specified subfolder.
		/// </summary>
		/// <returns>The subfolder.</returns>
		/// <param name="name">The name of the subfolder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="FolderNotFoundException">
		/// The requested folder could not be found.
		/// </exception>
		IFolder GetSubfolder (string name, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Forces the server to flush its state for the folder.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		void Check (CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Updates the values of the specified items.
		/// </summary>
		/// <param name="items">The items to update.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		void Status (StatusItems items, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Expunges the folder, permanently removing all messages marked for deletion.
		/// </summary>
		/// <remarks>
		/// An <see cref="Expunged"/> event will be emitted for each message that is expunged.
		/// </remarks>
		/// <param name="cancellationToken">The cancellation token.</param>
		void Expunge (CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Expunges the specified uids, permanently removing them from the folder.
		/// </summary>
		/// <remarks>
		/// An <see cref="Expunged"/> event will be emitted for each message that is expunged.
		/// </remarks>
		/// <param name="uids">The message uids.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		void Expunge (UniqueId[] uids, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Appends the specified message to the folder.
		/// </summary>
		/// <returns>The UID of the appended message, if available; otherwise, <c>null</c>.</returns>
		/// <param name="message">The message.</param>
		/// <param name="flags">The message flags.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		UniqueId? Append (MimeMessage message, MessageFlags flags, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Appends the specified message to the folder.
		/// </summary>
		/// <returns>The UID of the appended message, if available; otherwise, <c>null</c>.</returns>
		/// <param name="message">The message.</param>
		/// <param name="flags">The message flags.</param>
		/// <param name="date">The received date of the message.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		UniqueId? Append (MimeMessage message, MessageFlags flags, DateTimeOffset date, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Appends the specified messages to the folder.
		/// </summary>
		/// <returns>The UIDs of the appended messages, if available; otherwise, <c>null</c>.</returns>
		/// <param name="messages">The array of messages to append to the folder.</param>
		/// <param name="flags">The message flags to use for each message.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		UniqueId[] Append (MimeMessage[] messages, MessageFlags[] flags, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Appends the specified messages to the folder.
		/// </summary>
		/// <returns>The UIDs of the appended messages, if available; otherwise, <c>null</c>.</returns>
		/// <param name="messages">The array of messages to append to the folder.</param>
		/// <param name="flags">The message flags to use for each of the messages.</param>
		/// <param name="dates">The received dates to use for each of the messages.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		UniqueId[] Append (MimeMessage[] messages, MessageFlags[] flags, DateTimeOffset[] dates, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Copies the specified messages to the destination folder.
		/// </summary>
		/// <returns>The UIDs of the messages in the destination folder, if available; otherwise, <c>null</c>.</returns>
		/// <param name="uids">The UIDs of the messages to copy.</param>
		/// <param name="destination">The destination folder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		UniqueId[] CopyTo (UniqueId[] uids, IFolder destination, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Moves the specified messages to the destination folder.
		/// </summary>
		/// <returns>The UIDs of the messages in the destination folder, if available; otherwise, <c>null</c>.</returns>
		/// <param name="uids">The UIDs of the messages to copy.</param>
		/// <param name="destination">The destination folder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		UniqueId[] MoveTo (UniqueId[] uids, IFolder destination, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Copies the specified messages to the destination folder.
		/// </summary>
		/// <param name="indexes">The indexes of the messages to copy.</param>
		/// <param name="destination">The destination folder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		void CopyTo (int[] indexes, IFolder destination, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Moves the specified messages to the destination folder.
		/// </summary>
		/// <param name="indexes">The indexes of the messages to copy.</param>
		/// <param name="destination">The destination folder.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		void MoveTo (int[] indexes, IFolder destination, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Fetches the message summaries for the specified message UIDs.
		/// </summary>
		/// <returns>An enumeration of summaries for the requested messages.</returns>
		/// <param name="uids">The UIDs.</param>
		/// <param name="items">The message summary items to fetch.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		IEnumerable<MessageSummary> Fetch (UniqueId[] uids, MessageSummaryItems items, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Fetches the message summaries for the specified message UIDs that have a higher mod-sequence value than the one specified.
		/// </summary>
		/// <returns>An enumeration of summaries for the requested messages.</returns>
		/// <param name="uids">The UIDs.</param>
		/// <param name="modseq">The mod-sequence value.</param>
		/// <param name="items">The message summary items to fetch.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		IEnumerable<MessageSummary> Fetch (UniqueId[] uids, ulong modseq, MessageSummaryItems items, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Fetches the message summaries for the messages between the two UIDs, inclusive.
		/// </summary>
		/// <returns>An enumeration of summaries for the requested messages.</returns>
		/// <param name="min">The minimum UID.</param>
		/// <param name="max">The maximum UID, or <c>null</c> to specify no upper bound.</param>
		/// <param name="items">The message summary items to fetch.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		IEnumerable<MessageSummary> Fetch (UniqueId min, UniqueId? max, MessageSummaryItems items, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Fetches the message summaries for the messages between the two UIDs (inclusive) that have a higher mod-sequence value than the one specified.
		/// </summary>
		/// <returns>An enumeration of summaries for the requested messages.</returns>
		/// <param name="min">The minimum UID.</param>
		/// <param name="max">The maximum UID.</param>
		/// <param name="modseq">The mod-sequence value.</param>
		/// <param name="items">The message summary items to fetch.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		IEnumerable<MessageSummary> Fetch (UniqueId min, UniqueId? max, ulong modseq, MessageSummaryItems items, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Fetches the message summaries for the specified message indexes.
		/// </summary>
		/// <returns>An enumeration of summaries for the requested messages.</returns>
		/// <param name="indexes">The indexes.</param>
		/// <param name="items">The message summary items to fetch.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		IEnumerable<MessageSummary> Fetch (int[] indexes, MessageSummaryItems items, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Fetches the message summaries for the specified message indexes that have a higher mod-sequence value than the one specified.
		/// </summary>
		/// <returns>An enumeration of summaries for the requested messages.</returns>
		/// <param name="indexes">The indexes.</param>
		/// <param name="modseq">The mod-sequence value.</param>
		/// <param name="items">The message summary items to fetch.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		IEnumerable<MessageSummary> Fetch (int[] indexes, ulong modseq, MessageSummaryItems items, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Fetches the message summaries for the messages between the two indexes, inclusive.
		/// </summary>
		/// <returns>An enumeration of summaries for the requested messages.</returns>
		/// <param name="min">The minimum index.</param>
		/// <param name="max">The maximum index, or <c>-1</c> to specify no upper bound.</param>
		/// <param name="items">The message summary items to fetch.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		IEnumerable<MessageSummary> Fetch (int min, int max, MessageSummaryItems items, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Fetches the message summaries for the messages between the two indexes (inclusive) that have a higher mod-sequence value than the one specified.
		/// </summary>
		/// <returns>An enumeration of summaries for the requested messages.</returns>
		/// <param name="min">The minimum index.</param>
		/// <param name="max">The maximum index, or <c>-1</c> to specify no upper bound.</param>
		/// <param name="modseq">The mod-sequence value.</param>
		/// <param name="items">The message summary items to fetch.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		IEnumerable<MessageSummary> Fetch (int min, int max, ulong modseq, MessageSummaryItems items, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Gets the specified message.
		/// </summary>
		/// <returns>The message.</returns>
		/// <param name="uid">The UID of the message.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		MimeMessage GetMessage (UniqueId uid, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Gets the specified message.
		/// </summary>
		/// <returns>The message.</returns>
		/// <param name="index">The index of the message.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		MimeMessage GetMessage (int index, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Gets the specified body part.
		/// </summary>
		/// <returns>The body part.</returns>
		/// <param name="uid">The UID of the message.</param>
		/// <param name="part">The body part.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		MimeEntity GetBodyPart (UniqueId uid, BodyPart part, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Gets the specified body part.
		/// </summary>
		/// <returns>The body part.</returns>
		/// <param name="uid">The UID of the message.</param>
		/// <param name="part">The body part.</param>
		/// <param name="headersOnly"><c>true</c> if only the headers should be downloaded; otherwise, <c>false</c>></param>
		/// <param name="cancellationToken">The cancellation token.</param>
		MimeEntity GetBodyPart (UniqueId uid, BodyPart part, bool headersOnly, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Gets the specified body part.
		/// </summary>
		/// <returns>The body part.</returns>
		/// <param name="index">The index of the message.</param>
		/// <param name="part">The body part.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		MimeEntity GetBodyPart (int index, BodyPart part, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Gets the specified body part.
		/// </summary>
		/// <returns>The body part.</returns>
		/// <param name="index">The index of the message.</param>
		/// <param name="part">The body part.</param>
		/// <param name="headersOnly"><c>true</c> if only the headers should be downloaded; otherwise, <c>false</c>></param>
		/// <param name="cancellationToken">The cancellation token.</param>
		MimeEntity GetBodyPart (int index, BodyPart part, bool headersOnly, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Gets a substream of the specified message.
		/// </summary>
		/// <remarks>
		/// Fetches a substream of the message. If the starting offset is beyond
		/// the end of the message, an empty stream is returned. If the number of
		/// bytes desired extends beyond the end of the message, a truncated stream
		/// will be returned.
		/// </remarks>
		/// <returns>The stream.</returns>
		/// <param name="uid">The UID of the message.</param>
		/// <param name="offset">The starting offset of the first desired byte.</param>
		/// <param name="count">The number of bytes desired.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		Stream GetStream (UniqueId uid, int offset, int count, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Gets a substream of the specified message.
		/// </summary>
		/// <remarks>
		/// Fetches a substream of the message. If the starting offset is beyond
		/// the end of the message, an empty stream is returned. If the number of
		/// bytes desired extends beyond the end of the message, a truncated stream
		/// will be returned.
		/// </remarks>
		/// <returns>The stream.</returns>
		/// <param name="index">The index of the message.</param>
		/// <param name="offset">The starting offset of the first desired byte.</param>
		/// <param name="count">The number of bytes desired.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		Stream GetStream (int index, int offset, int count, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Gets a substream of the specified body part.
		/// </summary>
		/// <remarks>
		/// Fetches a substream of the body part. If the starting offset is beyond
		/// the end of the body part, an empty stream is returned. If the number of
		/// bytes desired extends beyond the end of the body part, a truncated stream
		/// will be returned.
		/// </remarks>
		/// <returns>The stream.</returns>
		/// <param name="uid">The UID of the message.</param>
		/// <param name="part">The desired body part.</param>
		/// <param name="offset">The starting offset of the first desired byte.</param>
		/// <param name="count">The number of bytes desired.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		Stream GetStream (UniqueId uid, BodyPart part, int offset, int count, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Gets a substream of the specified body part.
		/// </summary>
		/// <remarks>
		/// Fetches a substream of the body part. If the starting offset is beyond
		/// the end of the body part, an empty stream is returned. If the number of
		/// bytes desired extends beyond the end of the body part, a truncated stream
		/// will be returned.
		/// </remarks>
		/// <returns>The stream.</returns>
		/// <param name="index">The index of the message.</param>
		/// <param name="part">The desired body part.</param>
		/// <param name="offset">The starting offset of the first desired byte.</param>
		/// <param name="count">The number of bytes desired.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		Stream GetStream (int index, BodyPart part, int offset, int count, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Adds a set of flags to the specified messages.
		/// </summary>
		/// <param name="uids">The UIDs of the messages.</param>
		/// <param name="flags">The message flags to add.</param>
		/// <param name="silent">If set to <c>true</c>, no <see cref="MessageFlagsChanged"/> events will be emitted.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		void AddFlags (UniqueId[] uids, MessageFlags flags, bool silent, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Removes a set of flags from the specified messages.
		/// </summary>
		/// <param name="uids">The UIDs of the messages.</param>
		/// <param name="flags">The message flags to remove.</param>
		/// <param name="silent">If set to <c>true</c>, no <see cref="MessageFlagsChanged"/> events will be emitted.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		void RemoveFlags (UniqueId[] uids, MessageFlags flags, bool silent, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Sets the flags of the specified messages.
		/// </summary>
		/// <param name="uids">The UIDs of the messages.</param>
		/// <param name="flags">The message flags to set.</param>
		/// <param name="silent">If set to <c>true</c>, no <see cref="MessageFlagsChanged"/> events will be emitted.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		void SetFlags (UniqueId[] uids, MessageFlags flags, bool silent, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Adds a set of flags to the specified messages only if their mod-sequence value is less than the specified value.
		/// </summary>
		/// <returns>The unique IDs of the messages that were not updated.</returns>
		/// <param name="uids">The UIDs of the messages.</param>
		/// <param name="modseq">The mod-sequence value.</param>
		/// <param name="flags">The message flags to add.</param>
		/// <param name="silent">If set to <c>true</c>, no <see cref="MessageFlagsChanged"/> events will be emitted.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		UniqueId[] AddFlags (UniqueId[] uids, ulong modseq, MessageFlags flags, bool silent, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Removes a set of flags from the specified messages only if their mod-sequence value is less than the specified value.
		/// </summary>
		/// <returns>The unique IDs of the messages that were not updated.</returns>
		/// <param name="uids">The UIDs of the messages.</param>
		/// <param name="modseq">The mod-sequence value.</param>
		/// <param name="flags">The message flags to remove.</param>
		/// <param name="silent">If set to <c>true</c>, no <see cref="MessageFlagsChanged"/> events will be emitted.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		UniqueId[] RemoveFlags (UniqueId[] uids, ulong modseq, MessageFlags flags, bool silent, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Sets the flags of the specified messages only if their mod-sequence value is less than the specified value.
		/// </summary>
		/// <returns>The unique IDs of the messages that were not updated.</returns>
		/// <param name="uids">The UIDs of the messages.</param>
		/// <param name="modseq">The mod-sequence value.</param>
		/// <param name="flags">The message flags to set.</param>
		/// <param name="silent">If set to <c>true</c>, no <see cref="MessageFlagsChanged"/> events will be emitted.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		UniqueId[] SetFlags (UniqueId[] uids, ulong modseq, MessageFlags flags, bool silent, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Adds a set of flags to the specified messages.
		/// </summary>
		/// <param name="indexes">The indexes of the messages.</param>
		/// <param name="flags">The message flags to add.</param>
		/// <param name="silent">If set to <c>true</c>, no <see cref="MessageFlagsChanged"/> events will be emitted.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		void AddFlags (int[] indexes, MessageFlags flags, bool silent, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Removes a set of flags from the specified messages.
		/// </summary>
		/// <param name="indexes">The indexes of the messages.</param>
		/// <param name="flags">The message flags to remove.</param>
		/// <param name="silent">If set to <c>true</c>, no <see cref="MessageFlagsChanged"/> events will be emitted.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		void RemoveFlags (int[] indexes, MessageFlags flags, bool silent, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Sets the flags of the specified messages.
		/// </summary>
		/// <param name="indexes">The indexes of the messages.</param>
		/// <param name="flags">The message flags to set.</param>
		/// <param name="silent">If set to <c>true</c>, no <see cref="MessageFlagsChanged"/> events will be emitted.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		void SetFlags (int[] indexes, MessageFlags flags, bool silent, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Adds a set of flags to the specified messages only if their mod-sequence value is less than the specified value.
		/// </summary>
		/// <returns>The indexes of the messages that were not updated.</returns>
		/// <param name="indexes">The indexes of the messages.</param>
		/// <param name="modseq">The mod-sequence value.</param>
		/// <param name="flags">The message flags to add.</param>
		/// <param name="silent">If set to <c>true</c>, no <see cref="MessageFlagsChanged"/> events will be emitted.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		int[] AddFlags (int[] indexes, ulong modseq, MessageFlags flags, bool silent, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Removes a set of flags from the specified messages only if their mod-sequence value is less than the specified value.
		/// </summary>
		/// <returns>The indexes of the messages that were not updated.</returns>
		/// <param name="indexes">The indexes of the messages.</param>
		/// <param name="modseq">The mod-sequence value.</param>
		/// <param name="flags">The message flags to remove.</param>
		/// <param name="silent">If set to <c>true</c>, no <see cref="MessageFlagsChanged"/> events will be emitted.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		int[] RemoveFlags (int[] indexes, ulong modseq, MessageFlags flags, bool silent, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Sets the flags of the specified messages only if their mod-sequence value is less than the specified value.
		/// </summary>
		/// <returns>The indexes of the messages that were not updated.</returns>
		/// <param name="indexes">The indexes of the messages.</param>
		/// <param name="modseq">The mod-sequence value.</param>
		/// <param name="flags">The message flags to set.</param>
		/// <param name="silent">If set to <c>true</c>, no <see cref="MessageFlagsChanged"/> events will be emitted.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		int[] SetFlags (int[] indexes, ulong modseq, MessageFlags flags, bool silent, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Searches the folder for messages matching the specified query.
		/// </summary>
		/// <remarks>
		/// The returned array of unique identifiers can be used with <see cref="IFolder.GetMessage(UniqueId,CancellationToken)"/>.
		/// </remarks>
		/// <returns>An array of matching UIDs.</returns>
		/// <param name="query">The search query.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		UniqueId[] Search (SearchQuery query, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Searches the folder for messages matching the specified query,
		/// returning them in the preferred sort order.
		/// </summary>
		/// <remarks>
		/// The returned array of unique identifiers will be sorted in the preferred order and
		/// can be used with <see cref="IFolder.GetMessage(UniqueId,CancellationToken)"/>.
		/// </remarks>
		/// <returns>An array of matching UIDs in the specified sort order.</returns>
		/// <param name="query">The search query.</param>
		/// <param name="orderBy">The sort order.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		UniqueId[] Search (SearchQuery query, OrderBy[] orderBy, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Searches the subset of UIDs in the folder for messages matching the specified query.
		/// </summary>
		/// <remarks>
		/// The returned array of unique identifiers can be used with <see cref="IFolder.GetMessage(UniqueId,CancellationToken)"/>.
		/// </remarks>
		/// <returns>An array of matching UIDs.</returns>
		/// <param name="uids">The subset of UIDs</param>
		/// <param name="query">The search query.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		UniqueId[] Search (UniqueId[] uids, SearchQuery query, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Searches the subset of UIDs in the folder for messages matching the specified query,
		/// returning them in the preferred sort order.
		/// </summary>
		/// <remarks>
		/// The returned array of unique identifiers will be sorted in the preferred order and
		/// can be used with <see cref="IFolder.GetMessage(UniqueId,CancellationToken)"/>.
		/// </remarks>
		/// <returns>An array of matching UIDs in the specified sort order.</returns>
		/// <param name="uids">The subset of UIDs</param>
		/// <param name="query">The search query.</param>
		/// <param name="orderBy">The sort order.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		UniqueId[] Search (UniqueId[] uids, SearchQuery query, OrderBy[] orderBy, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Threads the messages in the folder that match the search query using the specified threading algorithm.
		/// </summary>
		/// <remarks>
		/// The <see cref="MessageThread.UniqueId"/> can be used with <see cref="IFolder.GetMessage(UniqueId,CancellationToken)"/>.
		/// </remarks>
		/// <returns>An array of message threads.</returns>
		/// <param name="algorithm">The threading algorithm to use.</param>
		/// <param name="query">The search query.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		MessageThread[] Thread (ThreadingAlgorithm algorithm, SearchQuery query, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Threads the messages in the folder that match the search query using the specified threading algorithm.
		/// </summary>
		/// <remarks>
		/// The <see cref="MessageThread.UniqueId"/> can be used with <see cref="IFolder.GetMessage(UniqueId,CancellationToken)"/>.
		/// </remarks>
		/// <returns>An array of message threads.</returns>
		/// <param name="uids">The subset of UIDs</param>
		/// <param name="algorithm">The threading algorithm to use.</param>
		/// <param name="query">The search query.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		MessageThread[] Thread (UniqueId[] uids, ThreadingAlgorithm algorithm, SearchQuery query, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Occurs when the folder is deleted.
		/// </summary>
		event EventHandler<EventArgs> Deleted;

		/// <summary>
		/// Occurs when the folder is renamed.
		/// </summary>
		event EventHandler<FolderRenamedEventArgs> Renamed;

		/// <summary>
		/// Occurs when the folder is subscribed.
		/// </summary>
		event EventHandler<EventArgs> Subscribed;

		/// <summary>
		/// Occurs when the folder is unsubscribed.
		/// </summary>
		event EventHandler<EventArgs> Unsubscribed;

		/// <summary>
		/// Occurs when a message is expunged from the folder.
		/// </summary>
		event EventHandler<MessageEventArgs> Expunged;

		/// <summary>
		/// Occurs when flags changed on a message.
		/// </summary>
		event EventHandler<MessageFlagsChangedEventArgs> MessageFlagsChanged;

		/// <summary>
		/// Occurs when the UID validity changes.
		/// </summary>
		event EventHandler<EventArgs> UidValidityChanged;

		/// <summary>
		/// Occurs when the message count changes.
		/// </summary>
		event EventHandler<EventArgs> CountChanged;

		/// <summary>
		/// Occurs when the recent message count changes.
		/// </summary>
		event EventHandler<EventArgs> RecentChanged;
	}
}
