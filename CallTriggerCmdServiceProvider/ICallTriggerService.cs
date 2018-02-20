using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace TCX.CallTriggerCmd
{
    [DataContract]
    public class UserProfileStatus
    {
        [DataMember]
        public bool IsActive { get; set; }
        
        [DataMember]
        public int ProfileId { get; set; }
        
        [DataMember]
        public string Name { get; set; }
        
        [DataMember]
        public string CustomName { get; set; }

        [DataMember]
        public string ExtendedStatus { get; set; }
    };

    public interface IClientCallback
    {
        [OperationContract]
        void CurrentProfileChanged(int profileid);

        [OperationContract]
        void ProfileExtendedStatusChanged(int profileid, string status);

        [OperationContract]
        void CallStatusChanged();

        [OperationContract]
        void MyPhoneStatusChanged();
    };

    public enum CallState
    {
        /// <summary>
        /// Undefined state. Such calls are not valid
        /// </summary>
        Undefined = 0,
        /// <summary>
        /// Incoming call is ringing on user's phone. Call is not yet answered by the user
        /// </summary>
        Ringing = 1,
        /// <summary>
        /// Outbound call initiated from user's phone. Call is not yet answered by remote party 
        /// </summary>
        Dialing = 2,
        /// <summary>
        /// Call is established. User is talking with other party
        /// </summary>
        Connected = 3,
        /// <summary>
        /// Call is rerouted. User waits for the answer form new party
        /// </summary>
        WaitingForNewParty = 4,
        /// <summary>
        /// User is transferring call to new destination. After successful transfer user will be disconnected from the call.
        /// </summary>
        TryingToTransfer = 5,
        /// <summary>
        /// Call has been disconnected
        /// </summary>
        Ended = 6
    }

    [DataContract]
    public class ActiveCall
    {
        /// <summary>
        /// ID of the local connection.
        /// </summary>
        [DataMember]
        public string CallID { get; set; }
        /// <summary>
        /// true if call is incoming, false - if not
        /// </summary>
        [DataMember]
        public bool Incoming { get; set; }
        /// <summary>
        /// State of the call
        /// </summary>
        [DataMember]
        public CallState State { get; set; }
        /// <summary>
        /// Remote party number.
        /// </summary>
        [DataMember]
        public string OtherPartyNumber { get; set; }
        /// <summary>
        /// Name of the remote party. Usually it is DisplayName of the Callee/Caller provided by PBX
        /// </summary>
        [DataMember]
        public string OtherPartyName { get; set; }
        /// <summary>
        /// Originator - is entity which is distributing(queue/ringgroup) or transfering the call. (incoming only)
        /// </summary>
//        OriginatorType OriginatorType { get; set; }
        /// <summary>
        /// internal id of the originator (DN)
        /// </summary>
        [DataMember]
        public string Originator { get; set; }
        /// <summary>
        /// Name of the originator
        /// </summary>
        [DataMember]
        public string OriginatorName { get; set; }
    }

    [Flags]
    public enum MakeCallOptions
    {
        /// <summary>
        /// No options
        /// </summary>
        None = 0,

        /// <summary>
        ///  Make video call
        /// </summary>
        WithVideo = 1,

        /// <summary>
        /// Copy a number to dialer without making a call
        /// </summary>
        DialOnly = 2,

        /// <summary>
        /// Make call by using Skype for Business contact
        /// </summary>
        SkypeForBusiness = 4
    }

    [Flags]
    public enum ActivateOptions
    {
        /// <summary>
        /// No options
        /// </summary>
        None = 0,
        /// <summary>
        ///  Answer with video
        /// </summary>
        WithVideo = 1
    };

    /// <summary>
    /// Additional options for show
    /// </summary>
    [Flags]
    public enum ShowOptions
    {
        /// <summary>
        /// No options
        /// </summary>
        None = 0
    }

    /// <summary>
    /// View to show
    /// </summary>
    public enum Views
    {
        /// <summary>
        /// Dialpad
        /// </summary>
        DialPad,
        /// <summary>
        /// Presence
        /// </summary>
        Presence,
        /// <summary>
        /// Contacts
        /// </summary>
        Contacts,
        /// <summary>
        /// Call history
        /// </summary>
        CallHistory,
        /// <summary>
        /// Voicemails
        /// </summary>
        Voicemails,
        /// <summary>
        /// Conferences
        /// </summary>
        Conferences,
        /// <summary>
        /// Chat
        /// </summary>
        Chats
    }

    [ServiceContract(CallbackContract = typeof(IClientCallback), SessionMode = SessionMode.Required)]
    public interface ICallTriggerService
    {
        List<ActiveCall> ActiveCalls 
        {
            [OperationContract]
            get; 
        }

        List<UserProfileStatus> Profiles 
        {
            [OperationContract]
            get; 
        }

        [OperationContract]
        void Subscribe();

        [OperationContract]
        void Unsubscribe();

        [OperationContract]
        string MakeCall(string destination);

        [OperationContract]
        string MakeCallEx(string destination, MakeCallOptions options);

        [OperationContract]
        void DropCall(string callId);
        
        [OperationContract]
        void BlindTransfer(string callId, string destination);
        
        [OperationContract]
        string BeginTransfer(string callId, string destination);
        
        [OperationContract]
        void CancelTransfer(string callId);
        
        [OperationContract]
        void CompleteTransfer(string callId);
        
        [OperationContract]
        void Activate(string callId);

        [OperationContract]
        void ActivateEx(string callId, ActivateOptions options);

        [OperationContract]
        void SetActiveProfile(int profileId);

        [OperationContract]
        void SetProfileExtendedStatus(int profileId, string status);

        [OperationContract]
        void Hold(string callId, bool holdOn);

        [OperationContract]
        void Mute(string callId);

        [OperationContract]
        void SendDTMF(string callId, string dtmf);

        [OperationContract]
        void SetQueueLoginStatus(bool loggedIn);

        [OperationContract]
        void Show(Views view, ShowOptions options);
    }
}
