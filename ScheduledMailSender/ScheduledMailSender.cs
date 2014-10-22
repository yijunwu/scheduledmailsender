using System;
using System.Collections.Generic;
using System;
using System.Net;
using System.Threading;

using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit;
using MimeKit;
using System.IO;

namespace ScheduledMailSender
{
    class ScheduledMailSender
    {
        public bool CheckAndSend(string user, string pwd, string imapUri, string smtpUri)
        {
            using (var client = new ImapClient())
            {
                var credentials = new NetworkCredential(user, pwd);
                var uriObj = new Uri(imapUri);

                using (var cancel = new CancellationTokenSource())
                {
                    client.Connect(uriObj, cancel.Token);

                    // Note: since we don't have an OAuth2 token, disable
                    // the XOAUTH2 authentication mechanism.
                    client.AuthenticationMechanisms.Remove("XOAUTH");

                    client.Authenticate(credentials, cancel.Token);

                    // The Inbox folder is always available on all IMAP servers...
                    var draftBox = client.GetFolder(SpecialFolder.Drafts);
                    if (draftBox == null)
                        draftBox = client.GetFolder("Drafts");
                    draftBox.Open(FolderAccess.ReadWrite, cancel.Token);

                    Console.WriteLine("Total messages: {0}", draftBox.Count);
                    Console.WriteLine("Recent messages: {0}", draftBox.Recent);

                    SearchQuery query = SearchQuery.All;
                    //query = SearchQuery.ToContains("liuyunming@skyrungrp.com");

                    UniqueId[] uids = draftBox.Search(query);

                    int count = 0;

                    for (int i = uids.Length - 1; i >= 0; i--)
                    {

                        UniqueId uid = uids[i];
                        var message = draftBox.GetMessage(uid, cancel.Token);
                        DateTimeOffset dto1 = DateTimeOffset.UtcNow;
                        if (dto1.AddHours(-12) >= message.Date)
                            break;
                        Console.WriteLine("Subject: {0}", message.Subject);
                        Console.WriteLine("Date: {0}", message.Date);

                        if (message.Bcc.Count > 0)
                        {
                            foreach (InternetAddress addr in message.Bcc)
                            {
                                string addrString = addr.Name;
                                if (string.IsNullOrEmpty(addrString))
                                {
                                    addrString = addr.ToString();
                                    if (addrString.Contains("<"))
                                        addrString = addrString.Substring(addrString.LastIndexOf("<") + 1, addrString.LastIndexOf(">") - addrString.LastIndexOf("<") -1);
                                }
                                //if (addr.Name.Contains("send@"))
                                if (addrString.Contains("send@"))
                                {
                                    //string time = addr.Name.Replace("send@", "");
                                    string time = addrString.Replace("send@", "");
                                    Console.WriteLine("Scheduled mail is found. Scheduled time: {0}", time);
                                    count++;

                                    string[] parts = time.Split(new char[] { '.' });
                                    if (parts.Length != 2)
                                        continue;
                                    int hour = 0;
                                    int minute = 0;
                                    DateTimeOffset dt2 = message.Date;
                                    DateTimeOffset offset2;
                                    if (int.TryParse(parts[0], out hour))
                                    {
                                        if (int.TryParse(parts[1], out minute))
                                        {
                                            dt2 = dt2.AddHours(hour - dt2.Hour);
                                            dt2 = dt2.AddMinutes(minute - dt2.Minute);
                                            dt2 = dt2.AddSeconds(0 - dt2.Second);


                                            if (dt2 <= DateTimeOffset.UtcNow && dt2 >= DateTimeOffset.UtcNow.AddHours(-1))
                                            {
                                                message.Bcc.Remove(addr);
                                                this.Send(message, user, pwd, smtpUri);                                                

                                                for (int retry = 1, maxRetry = 10; retry <= maxRetry; i++)
                                                {
                                                    Thread.Sleep(20 * 1000);
                                                    try
                                                    {
                                                        Console.WriteLine("{0} in {1} trys to delete draft", retry, maxRetry);
                                                        draftBox.SetFlags(new UniqueId[] { uid }, MessageFlags.Deleted, true);
                                                        draftBox.Expunge();
                                                    }
                                                    catch (IOException e)
                                                    {
                                                        retry++;
                                                        continue;
                                                    }
                                                    break;
                                                }

                                                Console.WriteLine("Scheduled mail is sent. Subject: {0}", message.Subject);
                                                break;

                                            }
                                            else
                                                Console.WriteLine("Scheduled time is not satisfied. Mail is not sent. Subject: {0}", message.Subject);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    Console.WriteLine("Total count for scheduled mails: {0}", count);

                    client.Disconnect(true, cancel.Token);
                }
            }
            return false;
        }

        public void Send(MimeMessage message, string user, string pwd, string uri)
        {
            using (var client = new SmtpClient())
            {
                var credentials = new NetworkCredential(user, pwd);

                // Note: if the server requires SSL-on-connect, use the "smtps" protocol instead
                var uriObj = new Uri(uri);

                using (var cancel = new CancellationTokenSource())
                {
                    client.Connect(uriObj, cancel.Token);

                    // Note: since we don't have an OAuth2 token, disable
                    // the XOAUTH2 authentication mechanism.
                    client.AuthenticationMechanisms.Remove("XOAUTH2");

                    // Note: only needed if the SMTP server requires authentication
                    client.Authenticate(credentials, cancel.Token);

                    client.Send(message, cancel.Token);
                    client.Disconnect(true, cancel.Token);
                }
            }
        }
    }
}
