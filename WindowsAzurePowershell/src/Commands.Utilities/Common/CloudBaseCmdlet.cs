// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Commands.Utilities.Common
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Management.Automation;
    using System.Net;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Security;
    using System.ServiceModel.Web;
    using System.Threading;
    using Microsoft.WindowsAzure.Commands.Utilities.Properties;
    using ServiceManagement;

    public abstract class CloudBaseCmdlet<T> : CmdletWithSubscriptionBase
        where T : class
    {
        private Binding _serviceBinding;

        private string _serviceEndpoint;

        public string CurrentServiceEndpoint { get; set; }

        public Binding ServiceBinding
        {
            get
            {
                if (_serviceBinding == null)
                {
                    _serviceBinding = ConfigurationConstants.WebHttpBinding(MaxStringContentLength);
                }

                return _serviceBinding;
            }

            set { _serviceBinding = value; }
        }

        public string ServiceEndpoint
        {
            get
            {
                if (!string.IsNullOrEmpty(CurrentServiceEndpoint))
                {
                    _serviceEndpoint = CurrentServiceEndpoint;
                }
                else if (CurrentSubscription != null && CurrentSubscription.ServiceEndpoint != null)
                {
                    _serviceEndpoint = CurrentSubscription.ServiceEndpoint.ToString();
                }
                else
                {
                    // Use default endpoint
                    _serviceEndpoint = Profile.CurrentEnvironment.ServiceEndpoint;
                }

                return _serviceEndpoint;
            }

            set
            {
                _serviceEndpoint = value;
            }
        }

        public T Channel
        {
            get;
            set;
        }

        protected override void OnCurrentSubscriptionUpdated()
        {
            // Recreate the channel if necessary
            if (!ShareChannel)
            {
                InitChannelCurrentSubscription(true);
            }
        }

        public int MaxStringContentLength
        {
            get;
            set;
        }

        /// <summary>
        /// Sets the current subscription to the passed subscription name. If null, no changes.
        /// </summary>
        /// <param name="subscriptionName">The subscription name</param>
        public void SetCurrentSubscription(string subscriptionName)
        {
            if (!string.IsNullOrEmpty(subscriptionName))
            {
                CurrentSubscription = Profile.Subscriptions.First(s => s.Name == subscriptionName);
            }
        }

        protected void InitChannelCurrentSubscription()
        {
            InitChannelCurrentSubscription(false);
        }

        protected virtual void InitChannelCurrentSubscription(bool force)
        {
            if (CurrentSubscription == null)
            {
                throw new ArgumentException(Resources.InvalidCurrentSubscription);
            }

            if (CurrentSubscription.Certificate == null)
            {
                throw new ArgumentException(Resources.InvalidCurrentSuscriptionCertificate);
            }

            if (string.IsNullOrEmpty(CurrentSubscription.SubscriptionId))
            {
                throw new ArgumentException(Resources.InvalidCurrentSubscriptionId);
            }

            if (Channel == null || force)
            {
                Channel = CreateChannel();
            }
        }

        protected virtual void OnProcessRecord()
        {
            // Intentionally left blank
        }

        protected override void ProcessRecord()
        {
            Validate.ValidateInternetConnection();
            InitChannelCurrentSubscription();
            base.ProcessRecord();
            OnProcessRecord();
        }

        /// <summary>
        /// Gets or sets a flag indicating whether CreateChannel should share
        /// the command's current Channel when asking for a new one.  This is
        /// only used for testing.
        /// </summary>
        public bool ShareChannel { get; set; }

        protected virtual T CreateChannel()
        {
            // If ShareChannel is set by a unit test, use the same channel that
            // was passed into out constructor.  This allows the test to submit
            // a mock that we use for all network calls.
            if (ShareChannel)
            {
                return Channel;
            }
            
            return ChannelHelper.CreateServiceManagementChannel<T>(
                ServiceBinding,
                new Uri(ServiceEndpoint),
                CurrentSubscription.Certificate,
                new HttpRestMessageInspector(WriteDebug));
        }

        protected void RetryCall(Action<string> call)
        {
            RetryCall(CurrentSubscription.SubscriptionId, call);
        }

        protected void RetryCall(string subsId, Action<string> call)
        {
            try
            {
                call(subsId);
            }
            catch (MessageSecurityException ex)
            {
                var webException = ex.InnerException as WebException;

                if (webException == null)
                {
                    throw;
                }

                var webResponse = webException.Response as HttpWebResponse;

                if (webResponse != null && webResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    WriteError(new ErrorRecord(new Exception(Resources.CommunicationCouldNotBeEstablished, ex), string.Empty, ErrorCategory.InvalidData, null));
                }
                else
                {
                    throw;
                }
            }
        }

        protected TResult RetryCall<TResult>(Func<string, TResult> call)
        {
            return RetryCall(CurrentSubscription.SubscriptionId, call);
        }

        protected TResult RetryCall<TResult>(string subsId, Func<string, TResult> call)
        {
            try
            {
                return call(subsId);
            }
            catch (MessageSecurityException ex)
            {
                var webException = ex.InnerException as WebException;

                if (webException == null)
                {
                    throw;
                }

                var webResponse = webException.Response as HttpWebResponse;

                if (webResponse != null && webResponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    WriteError(new ErrorRecord(new Exception(Resources.CommunicationCouldNotBeEstablished, ex), string.Empty, ErrorCategory.InvalidData, null));
                    throw;
                }

                throw;
            }
        }

        protected virtual Operation WaitForOperation(string opdesc)
        {
            return WaitForOperation(opdesc, false);
        }

        protected virtual Operation WaitForOperation(string opdesc, bool silent)
        {
            string operationId = RetrieveOperationId();
            Operation operation = null;

            if (!string.IsNullOrEmpty(operationId))
            {
                try
                {
                    WindowsAzureSubscription currentSubscription = Profile.CurrentSubscription;

                    operation = RetryCall(s => GetOperationStatus(currentSubscription.SubscriptionId, operationId));

                    var activityId = new Random().Next(1, 999999);
                    var progress = new ProgressRecord(activityId, opdesc, "Operation Status: " + operation.Status);

                    while (string.Compare(operation.Status, OperationState.Succeeded, StringComparison.OrdinalIgnoreCase) != 0 &&
                            string.Compare(operation.Status, OperationState.Failed, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        if (silent == false)
                        {
                            WriteProgress(progress);
                        }

                        Thread.Sleep(1 * 1000);
                        operation = RetryCall(s => GetOperationStatus(currentSubscription.SubscriptionId, operationId));
                    }
                    
                    if (string.Compare(operation.Status, OperationState.Failed, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        var errorMessage = string.Format(CultureInfo.InvariantCulture, "{0}: {1}", operation.Status, operation.Error.Message);
                        var exception = new Exception(errorMessage);
                        WriteError(new ErrorRecord(exception, string.Empty, ErrorCategory.CloseError, null));
                    }

                    if (silent == false)
                    {
                        progress = new ProgressRecord(activityId, opdesc, "Operation Status: " + operation.Status);
                        WriteProgress(progress);
                    }
                }
                catch (CommunicationException ex)
                {
                    WriteErrorDetails(ex);
                }
            }
            else
            {
                operation = new Operation
                {
                    OperationTrackingId = string.Empty,
                    Status = OperationState.Failed
                };
            }

            return operation;
        }

        /// <summary>
        /// Invoke the given operation within an OperationContextScope if the
        /// channel supports it.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        protected virtual void InvokeInOperationContext(Action action)
        {
            IContextChannel contextChannel = Channel as IContextChannel;
            if (contextChannel != null)
            {
                using (new OperationContextScope(contextChannel))
                {
                    action();
                }
            }
            else
            {
                action();
            }
        }

        protected virtual Operation GetOperationStatus(string subscriptionId, string operationId)
        {
            var channel = (IServiceManagement)Channel;
            return channel.GetOperationStatus(subscriptionId, operationId);
        }

        protected virtual void WriteErrorDetails(CommunicationException exception)
        {
            ServiceManagementError error;
            ErrorRecord errorRecord = null;

            string operationId;
            if (ErrorHelper.TryGetExceptionDetails(exception, out error, out operationId))
            {
                string errorDetails = string.Format(
                    CultureInfo.InvariantCulture,
                    "HTTP Status Code: {0} - HTTP Error Message: {1}\nOperation ID: {2}",
                    error.Code,
                    error.Message,
                    operationId);

                errorRecord = new ErrorRecord(
                    new CommunicationException(errorDetails),
                    string.Empty,
                    ErrorCategory.CloseError,
                    null);
            }
            else
            {
                errorRecord = new ErrorRecord(exception, string.Empty, ErrorCategory.CloseError, null);
            }

            if (CommandRuntime != null)
            {
                WriteError(errorRecord);
            }
        }

        /// <summary>
        /// Wrap the base Cmdlet's WriteError call so that it will not throw
        /// a NotSupportedException when called without a CommandRuntime (i.e.,
        /// when not called from within Powershell).
        /// </summary>
        /// <param name="errorRecord">The error to write.</param>
        protected void WriteWindowsAzureError(ErrorRecord errorRecord)
        {
            // If the exception is an Azure Service Management error, pull the
            // Azure message out to the front instead of the generic response.
            errorRecord = AzureServiceManagementException.WrapExistingError(errorRecord);
        }

        protected static string RetrieveOperationId()
        {
            var operationId = string.Empty;

            if ((WebOperationContext.Current != null) && (WebOperationContext.Current.IncomingResponse != null))
            {
                operationId = WebOperationContext.Current.IncomingResponse.Headers[ServiceManagement.Constants.OperationTrackingIdHeader];
            }

            return operationId;
        }
    }
}
