using System;
using System.Text;
using Mono.Options;
using System.ServiceModel;
using Microsoft.Win32;

namespace TCX.CallTriggerCmd
{
    public class Program
    {
        class ServiceCallback : IClientCallback
        {

            public void CurrentProfileChanged(int profileid)
            {
                Console.WriteLine("Profile changed");
            }

            public void ProfileExtendedStatusChanged(int profileid, string status)
            {
                Console.WriteLine("Extended status changed");
            }

            public void CallStatusChanged()
            {
                Console.WriteLine("Call status changed");
            }

            public void MyPhoneStatusChanged()
            {
                Console.WriteLine("MyPhone status changed");
            }
        };

        static int Main(string[] args)
        {
            try
            {
                bool show_help = false;
                var listen = false;

                var binding = new NetNamedPipeBinding();
                var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\3CX");
                var uri = key.GetValue("CallTriggerCmdUri");
                if (uri == null)
                    throw new Exception("User specific 3CXPhone CallTrigger uri is not found");

                var address = new EndpointAddress(uri.ToString());
                var channelFactory = new DuplexChannelFactory<ICallTriggerService>(
                    new ServiceCallback(), binding, address);
                var service = channelFactory.CreateChannel();

                var p = new OptionSet() {
                    { "cmd=:", "Deprecated make call command. Please specify destination as second parameter. Example '-cmd makecall:123'",
                        (string command, string destination) => Console.WriteLine( service.MakeCall(DestinationHelper.Normalize(destination))) },

                    { "c|call=", "Make a call to specified destination",
                        (string destination) => Console.WriteLine( service.MakeCall(DestinationHelper.Normalize(destination))) },

                    { "v|videocall=", "Make a video call to specified destination",
                        (string destination) => Console.WriteLine( service.MakeCallEx(DestinationHelper.Normalize(destination), MakeCallOptions.WithVideo)) },

                    { "dial=", "Copy a number to dialer",
                        (string destination) => Console.WriteLine( service.MakeCallEx(DestinationHelper.Normalize(destination), MakeCallOptions.DialOnly)) },

                    { "d|drop=", "Drop a call with specified call id",
                        (string id) => service.DropCall(id) },

                    { "show=", "Show a specific view",
                        (string id) => {
                            Views result;
                            if (Enum.TryParse<Views>(id, out result))
                                service.Show(result, ShowOptions.None);
                        } },

                    { "hold=", "Hold a call with specified call id",
                        (string id) => service.Hold(id, true) },

                    { "resume=", "Resume a call with specified call id",
                        (string id) => service.Hold(id, false) },

                    { "mute=", "Mute/unmute a call with specified call id",
                        (string id) => service.Mute(id) },

                    { "queue=", "Login/Logout from queue",
                        (int status) => service.SetQueueLoginStatus(status > 0) },

                    { "dtmf=/", "Send DTMF codes to specific call id",
                        (string selectedCallId, string dtmf) => service.SendDTMF(selectedCallId, dtmf) },

                    { "listen", "Listen to events after finishing jobs",
                        (l) => {
                            service.Subscribe();
                            listen = true;
                        } },

                    { "activate=", "Activate a call with specified call id",
                        (string id) => service.Activate(id) },

                    { "vactivate=", "Activate a call with specified call id with video",
                        (string id) => service.ActivateEx(id, ActivateOptions.WithVideo) },

                    { "h|help", "Show this help",
                        v => show_help = v != null},

                    { "b|blind=/", "Blind transfer selected call to destination",
                        (string selectedCallId, string destination) => service.BlindTransfer(selectedCallId, DestinationHelper.Normalize(destination))},

                    { "a|attended=/", "Begin attended transfer selected call to destination",
                        (string selectedCallId, string destination) => Console.WriteLine( service.BeginTransfer(selectedCallId, DestinationHelper.Normalize(destination)) )},

                    { "cancel=", "Stop attended transfer for call id",
                        (string id) => service.CancelTransfer(id)},

                    { "t|transfer=", "Finalize attended transfer for call id",
                        (string id) => service.CompleteTransfer(id)},

                    { "p|profiles", "Show list of all available user profiles",
                        x =>
                            {
                                foreach (var profile in service.Profiles)
                                    Console.WriteLine(String.Format("{0}\t{1}\t{2}\t{3}",
                                        profile.ProfileId, profile.Name, profile.CustomName, profile.ExtendedStatus));
                            }},

                    { "set-active-profile=", "Set active profile",
                        (int profileId) =>
                            {
                                service.SetActiveProfile(profileId);
                            }},

                    { "set-profile-status=/", "Set profile status",
                        (int profileId, string status) =>
                            {
                                service.SetProfileExtendedStatus(profileId, status);
                            }},

                    { "l|list", "Show list of active calls", l =>
                        {
                            foreach( var id in service.ActiveCalls )
                                Console.WriteLine("{0} {1}", id.CallID, id.State);
                        }
                    },
                };
                p.Parse(args);
                if ( show_help )
                    p.WriteOptionDescriptions(Console.Out);
                if (listen)
                {
                    Console.ReadLine();
                    service.Unsubscribe();
                }
                channelFactory.Close();
                return 0;
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception.ToString());
                return -1;
            }
        }
    }
}
