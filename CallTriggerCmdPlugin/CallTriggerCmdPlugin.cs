using System;
using System.Linq;
using MyPhonePlugins;
using System.Collections.Generic;
using System.ServiceModel;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Threading;

namespace TCX.CallTriggerCmd
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, 
                     InstanceContextMode = InstanceContextMode.Single, UseSynchronizationContext=false)]
    [ErrorHandlingBehavior]
    class CallTriggerCmdPlugin : ICallTriggerService, IDisposable
    {
        static readonly Dictionary<Views, MyPhonePlugins.Views> _pluginViewToView = new Dictionary<Views, MyPhonePlugins.Views>() {
            { Views.CallHistory, MyPhonePlugins.Views.CallHistory},
            { Views.Chats, MyPhonePlugins.Views.Chats},
            { Views.Conferences, MyPhonePlugins.Views.Conferences},
            { Views.DialPad, MyPhonePlugins.Views.DialPad},
            { Views.Contacts, MyPhonePlugins.Views.Contacts},
            { Views.Presence, MyPhonePlugins.Views.Presence},
            { Views.Voicemails, MyPhonePlugins.Views.Voicemails},
        };

        private static readonly IReadOnlyDictionary<MakeCallOptions, MyPhonePlugins.MakeCallOptions> OptCorrespondence =
            new Dictionary<MakeCallOptions, MyPhonePlugins.MakeCallOptions>
            {
                {MakeCallOptions.None, MyPhonePlugins.MakeCallOptions.None},
                {MakeCallOptions.WithVideo, MyPhonePlugins.MakeCallOptions.WithVideo},
                {MakeCallOptions.DialOnly, MyPhonePlugins.MakeCallOptions.DialOnly},
                {MakeCallOptions.SkypeForBusiness, MyPhonePlugins.MakeCallOptions.SkypeForBusiness}
            };

        readonly List<IClientCallback> _callbackChannels = new List<IClientCallback>();
        IMyPhoneCallHandler callHandler;
        IExtensionInfo extensionInfo;
        ServiceHost _serviceHost;

        public List<ActiveCall> ActiveCalls 
        { 
            get
            {
                return callHandler.ActiveCalls.Select(x => new ActiveCall()
                {
                    CallID = x.CallID,
                    State = ConvertCallState(x.State),
                    Incoming = x.Incoming,
                    Originator = x.Originator,
                    OriginatorName = x.OriginatorName,
                    OtherPartyName = x.OtherPartyName,
                    OtherPartyNumber = x.OtherPartyNumber
                }).ToList();
            }
        }

        private CallState ConvertCallState(MyPhonePlugins.CallState state)
        {
            switch(state)
            {
                case MyPhonePlugins.CallState.Connected:
                    return CallState.Connected;
                case MyPhonePlugins.CallState.Dialing:
                    return CallState.Dialing;
                case MyPhonePlugins.CallState.Ended:
                    return CallState.Ended;
                case MyPhonePlugins.CallState.Ringing:
                    return CallState.Ringing;
                case MyPhonePlugins.CallState.TryingToTransfer:
                    return CallState.TryingToTransfer;
                case MyPhonePlugins.CallState.Undefined:
                    return CallState.Undefined;
                case MyPhonePlugins.CallState.WaitingForNewParty:
                    return CallState.WaitingForNewParty;
                default:
                    return CallState.Undefined;
            }
        }

        public List<UserProfileStatus> Profiles 
        {
            get
            {
                return callHandler.Profiles.Select(x => new UserProfileStatus() 
                { 
                    IsActive = x.IsActive,
                    ProfileId = x.ProfileId,
                    CustomName = x.CustomName,
                    Name = x.Name,
                    ExtendedStatus = x.ExtendedStatus
                }).ToList();
            }
        }

        public CallTriggerCmdPlugin(MyPhonePlugins.IMyPhoneCallHandler callHandler)
        {
            try
            {
                this.callHandler = callHandler;
                callHandler.OnCallStatusChanged += callHandler_OnCallStatusChanged;
                callHandler.OnMyPhoneStatusChanged += callHandler_OnMyPhoneStatusChanged;
                callHandler.CurrentProfileChanged += callHandler_CurrentProfileChanged;
                callHandler.ProfileExtendedStatusChanged += callHandler_ProfileExtendedStatusChanged;

            }
            catch(Exception exception)
            {
                Dispose();
                throw exception;
            }
        }

        public void StartServiceAsync()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var binding = new NetNamedPipeBinding();
                    var baseAddress = new Uri(GetUserSpecificUri());

                    _serviceHost = new ServiceHost(this, baseAddress);
                    _serviceHost.AddServiceEndpoint(typeof(ICallTriggerService), binding, baseAddress);
                    _serviceHost.Open();
                }
                catch
                {
                    // Exception opening named pipe
                }
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }

        static string GetUserSpecificUri()
        {
            using (var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\3CX"))
            {
                var registryValue = key.GetValue("CallTriggerCmdUri");
                if (registryValue != null)
                    return registryValue.ToString();

                var uri = "net.pipe://localhost/CallTriggerService/" + Guid.NewGuid().ToString();
                key.SetValue("CallTriggerCmdUri", uri, RegistryValueKind.String);
                return uri;
            }
        }

        public void Dispose()
        {
            if (_serviceHost != null)
            {
                _serviceHost.Close();
                _serviceHost = null;
            }
            callHandler.CurrentProfileChanged -= callHandler_CurrentProfileChanged;
            callHandler.ProfileExtendedStatusChanged -= callHandler_ProfileExtendedStatusChanged;
            callHandler.OnCallStatusChanged -= callHandler_OnCallStatusChanged;
            callHandler.OnMyPhoneStatusChanged -= callHandler_OnMyPhoneStatusChanged;
        }

        public void Subscribe()
        {
            lock(_callbackChannels)
            {
                var channel = OperationContext.Current.GetCallbackChannel<IClientCallback>();
                if (!_callbackChannels.Contains(channel)) //if CallbackChannels not contain current one.
                    _callbackChannels.Add(channel);
            }
        }

        public void Unsubscribe()
        {
            lock (_callbackChannels)
            {
                var channel = OperationContext.Current.GetCallbackChannel<IClientCallback>();
                _callbackChannels.Remove(channel);
            }
        }

        public string MakeCallEx(string destination, MakeCallOptions options)
        {
            try
            {
                var status = callHandler.MakeCall(destination, ConvertMakeCallOptions(options));
                if (status != null)
                    return status.CallID;
            }
            catch (Exception exc)
            {
                LogHelper.Log(Environment.SpecialFolder.ApplicationData, "CallTriggerCmd.log",
                    "Error executing callHandler.MakeCall(" + destination + "): " + exc.ToString());
            }
            return String.Empty;
        }


        private static MyPhonePlugins.MakeCallOptions ConvertMakeCallOptions(MakeCallOptions options)
        {
            var myphoneOptions = MyPhonePlugins.MakeCallOptions.None;

            myphoneOptions |=
                OptCorrespondence
                    .Where(s => options.HasFlag(s.Key))
                    .Select(s => s.Value)
                    .Aggregate((f, s) => f | s);

            return myphoneOptions;
        }

        public string MakeCall(string destination)
        {
            return MakeCallEx(destination, MakeCallOptions.None);
        }

        public void DropCall(string callId)
        {
            callHandler.DropCall(callId);
        }

        public void BlindTransfer(string callId, string destination)
        {
            callHandler.BlindTransfer(callId, destination);
        }

        public string BeginTransfer(string callId, string destination)
        {
            var status = callHandler.BeginTransfer(callId, destination);
            return (status != null) ? status.CallID : String.Empty;
        }

        public void CancelTransfer(string callId)
        {
            callHandler.CancelTransfer(callId);
        }

        public void CompleteTransfer(string callId)
        {
            callHandler.CompleteTransfer(callId);
        }

        public void Activate(string callId)
        {
            ActivateEx(callId, ActivateOptions.None);
        }

        public void ActivateEx(string callId, ActivateOptions options)
        {
            callHandler.ActivateEx(callId, ConvertActivateOptions(options));
        }

        private MyPhonePlugins.ActivateOptions ConvertActivateOptions(ActivateOptions options)
        {
            if ((options & ActivateOptions.WithVideo) != 0)
                return MyPhonePlugins.ActivateOptions.WithVideo;
            else
                return MyPhonePlugins.ActivateOptions.None;
        }

        private void callHandler_OnMyPhoneStatusChanged(object sender, MyPhonePlugins.MyPhoneStatus status)
        {
            if (status == MyPhonePlugins.MyPhoneStatus.LoggedIn)
                this.extensionInfo = sender as MyPhonePlugins.IExtensionInfo;

            LogHelper.Log(Environment.SpecialFolder.ApplicationData, "CallTriggerCmd.log", 
                String.Format("MyPhoneStatusChanged - Status='{0}' - Extension='{1}'", status, extensionInfo == null ? String.Empty : extensionInfo.Number));
            Callback(channel => channel.MyPhoneStatusChanged());
        }

        private void callHandler_OnCallStatusChanged(object sender, MyPhonePlugins.CallStatus callInfo)
        {
            LogHelper.Log(Environment.SpecialFolder.ApplicationData, "CallTriggerCmd.log", 
                String.Format("CallStatusChanged - CallID='{0}' - Incoming='{1}' - OtherPartyNumber='{2}' - State='{3}'", callInfo.CallID, callInfo.Incoming, callInfo.OtherPartyNumber, callInfo.State));
            Callback(channel => channel.CallStatusChanged());
        }

        void callHandler_ProfileExtendedStatusChanged(object sender, ProfileExtendedStatusChangedEventArgs e)
        {
            Callback(channel => channel.ProfileExtendedStatusChanged(e.ProfileId, e.Status));
        }

        void callHandler_CurrentProfileChanged(object sender, CurrentProfileChangedEventArgs e)
        {
            Callback(channel => channel.CurrentProfileChanged(e.NewProfileId));
        }

        public void SetActiveProfile(int profileId)
        {
            callHandler.SetActiveProfile(profileId);
        }

        public void SetProfileExtendedStatus(int profileId, string status)
        {
            callHandler.SetProfileExtendedStatus(profileId, status);
        }

        void Callback(Action<IClientCallback> action)
        {
            lock (_callbackChannels)
            {
                foreach (var channel in _callbackChannels.ToList())
                    try
                    {
                        action.Invoke(channel);
                    }
                    catch (CommunicationException)
                    {
                        _callbackChannels.Remove(channel);
                    }
            }
        }

        public void Hold(string callId, bool holdOn)
        {
            callHandler.Hold(callId, holdOn);
        }

        public void Mute(string callId)
        {
            callHandler.Mute(callId);
        }

        public void SendDTMF(string callId, string dtmf)
        {
            callHandler.SendDTMF(callId, dtmf);
        }

        public void SetQueueLoginStatus(bool loggedIn)
        {
            callHandler.SetQueueLoginStatus(loggedIn);
        }

        public void Show(Views view, ShowOptions options)
        {
            callHandler.Show(_pluginViewToView[view], MyPhonePlugins.ShowOptions.None);
        }
    }
}
