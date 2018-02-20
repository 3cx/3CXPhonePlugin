using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;

namespace TCX.CallTriggerCmd
{
    public sealed class ErrorHandler : IErrorHandler
    {
        public bool HandleError(Exception error)
        {
            return false;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            //If it's a FaultException already, then we have nothing to do
            if (error is FaultException)
                return;

            var newException = new FaultException(error.Message);
            var messageFault = newException.CreateMessageFault();
            fault = Message.CreateMessage(version, messageFault, newException.Action);
        }

    }
}
