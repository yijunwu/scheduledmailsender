using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ScheduledMailSender
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// Command line options: -console user password
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] != null && (args[0].ToLower().Equals("-console") || args[0].ToLower().Equals("/console")))
                {
                    ScheduledMailSender sender = new ScheduledMailSender();
                    string user = args[1];
                    string password = args[2];
                    string imapUri = "imaps://imap-mail.outlook.com";
                    string smtpUri = "smtp://smtp-mail.outlook.com:587";
                    try
                    {
                        RunTimer((int)(4.5*60.0));
                        sender.CheckAndSend(user, password, imapUri, smtpUri);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Exception unhandled: "+e+"\r\n"+e.StackTrace);
                        return -1;
                    }
                    return 0;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
            return 0;
        }


        public static void RunTimer(int timeOutInSec)
        {
            StateObjClass StateObj = new StateObjClass();
            StateObj.MaxValue = (int) timeOutInSec / 1;
            StateObj.TimerCanceled = false;
            StateObj.SomeValue = 1;
            System.Threading.TimerCallback TimerDelegate =
                new System.Threading.TimerCallback(TimerTask);

            // Create a timer that calls a procedure every 2 seconds. 
            // Note: There is no Start method; the timer starts running as soon as  
            // the instance is created.
            System.Threading.Timer TimerItem =
                new System.Threading.Timer(TimerDelegate, StateObj, 1000, 1000);

            // Save a reference for Dispose.
            StateObj.TimerReference = TimerItem;
        }

        private static void TimerTask(object StateObj)
        {
            StateObjClass State = (StateObjClass)StateObj;
            // Use the interlocked class to increment the counter variable.
            System.Threading.Interlocked.Increment(ref State.SomeValue);
            //System.Diagnostics.Debug.WriteLine("Launched new thread  " + DateTime.Now.ToString());
            if (State.TimerCanceled)
            // Dispose Requested.
            {
                State.TimerReference.Dispose();
                //System.Diagnostics.Debug.WriteLine("Done  " + DateTime.Now.ToString());
            }
            if (State.SomeValue >= State.MaxValue)
            {
                State.TimerReference.Dispose();
                Console.WriteLine("Timed out("+State.MaxValue+" seconds). Exiting program now.");
                Environment.Exit(-1);
            }
        }

        private class StateObjClass
        {
            // Used to hold parameters for calls to TimerTask. 
            public int SomeValue;
            public int MaxValue;
            public System.Threading.Timer TimerReference;
            public bool TimerCanceled;
        }
    }
}
