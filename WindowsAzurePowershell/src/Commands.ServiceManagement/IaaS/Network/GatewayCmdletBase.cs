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

namespace Microsoft.WindowsAzure.Commands.ServiceManagement.IaaS
{
    using System.Diagnostics.CodeAnalysis;
    using System;
    using System.Globalization;
    using System.Management.Automation;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Security.Cryptography.X509Certificates;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Web;
    using System.Threading;
    using System.Xml;
    using Commands.Utilities.Common;
    using WindowsAzure.ServiceManagement;
    using Service.Gateway;
    using Properties;

    public class GatewayCmdletBase : CloudBaseCmdlet<IGatewayServiceManagement>
    {
        protected override IGatewayServiceManagement CreateChannel()
        {
            if (ServiceBinding == null)
            {
                ServiceBinding = ConfigurationConstants.WebHttpBinding();
            }

            if (CurrentSubscription.ServiceEndpoint == null)
            {
                ServiceEndpoint = Profile.CurrentEnvironment.ServiceEndpoint;
            }
            else
            {
                ServiceEndpoint = CurrentSubscription.ServiceEndpoint.ToString();
            }

            return GatewayManagementHelper.CreateGatewayManagementChannel(ServiceBinding, new Uri(ServiceEndpoint), CurrentSubscription.Certificate);
        }

        protected override void WriteErrorDetails(CommunicationException exception)
        {
            ServiceManagementError error;
            GatewayManagementHelper.TryGetExceptionDetails(exception, out error);

            if (error == null)
            {
                WriteError(new ErrorRecord(exception, string.Empty, ErrorCategory.CloseError, null));
            }
            else
            {
                string errorDetails = string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.HttpStatusCodeAndErrorMessage,
                    error.Code,
                    error.Message);

                WriteError(new ErrorRecord(new CommunicationException(errorDetails), string.Empty, ErrorCategory.CloseError, null));
            }
        }
        protected void ExecuteClientActionInOCS<TResult>(object input, string operationDescription, Func<string, TResult> action, Func<string, Operation> waitOperation, Func<Operation, TResult, object> contextFactory) where TResult : class
        {
            IContextChannel contextChannel = Channel as IContextChannel;
            if (contextChannel != null)
            {
                using (new OperationContextScope(contextChannel))
                {
                    try
                    {
                        TResult result = RetryCall(action);
                        Operation operation = waitOperation(operationDescription);
                        if (result != null)
                        {
                            object context = contextFactory(operation, result);
                            WriteObject(context, true);
                        }
                    }
                    catch (CommunicationException ex)
                    {
                        WriteErrorDetails(ex);
                    }
                }
            }
            else
            {
                TResult result = RetryCall(action);
                if (result != null)
                {
                    WriteObject(result, true);
                }
            }
        }

        protected void ExecuteClientActionInOCS(object input, string operationDescription, Action<string> action, Func<string, Operation> waitOperation)
        {
            IContextChannel contextChannel = Channel as IContextChannel;
            if (contextChannel != null)
            {
                using (new OperationContextScope(contextChannel))
                {
                    try
                    {
                        RetryCall(action);
                        Operation operation = waitOperation(operationDescription);
                        var context = new ManagementOperationContext
                        {
                            OperationDescription = operationDescription,
                            OperationId = operation.OperationTrackingId,
                            OperationStatus = operation.Status
                        };

                        WriteObject(context, true);
                    }
                    catch (CommunicationException ex)
                    {
                        WriteErrorDetails(ex);
                    }
                }
            }
            else
            {
                RetryCall(action);
            }
        }

        protected Operation WaitForNewGatewayOperation(string opdesc)
        {
            Operation operation = null;
            String operationId = RetrieveOperationId();
            WindowsAzureSubscription currentSubscription = CurrentSubscription;
            try
            {
                IGatewayServiceManagement channel = (IGatewayServiceManagement)Channel;
                operation = RetryCall(s => channel.GetGatewayOperation(currentSubscription.SubscriptionId, operationId));

                var activityId = new Random().Next(1, 999999);
                var progress = new ProgressRecord(activityId, opdesc, Resources.GatewayOperationStatus + operation.Status);
                while (string.Compare(operation.Status, OperationState.Succeeded, StringComparison.OrdinalIgnoreCase) != 0 &&
                        string.Compare(operation.Status, OperationState.Failed, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    WriteProgress(progress);
                    Thread.Sleep(1 * 1000);
                    operation = RetryCall(s => channel.GetGatewayOperation(currentSubscription.SubscriptionId, operationId));
                }

                if (string.Compare(operation.Status, OperationState.Failed, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var errorMessage = string.Format(CultureInfo.InvariantCulture, "{0}: {1}", operation.Status, operation.Error.Message);
                    var exception = new Exception(errorMessage);
                    WriteError(new ErrorRecord(exception, string.Empty, ErrorCategory.CloseError, null));
                }
            }
            catch (CommunicationException ex)
            {
                WriteErrorDetails(ex);
            }

            return operation;
        }

    }

    public static class GatewayManagementHelper
    {
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposing the factory would also dispose the channel we are returning.")]
        public static IGatewayServiceManagement CreateGatewayManagementChannel(Binding binding, Uri remoteUri, X509Certificate2 cert)
        {
            WebChannelFactory<IGatewayServiceManagement> factory = new WebChannelFactory<IGatewayServiceManagement>(binding, remoteUri);
            factory.Endpoint.Behaviors.Add(new ServiceManagementClientOutputMessageInspector());
            factory.Credentials.ClientCertificate.Certificate = cert;
            return factory.CreateChannel();
        }

        public static bool TryGetExceptionDetails(CommunicationException exception, out ServiceManagementError errorDetails)
        {
            HttpStatusCode httpStatusCode;
            string operationId;
            return TryGetExceptionDetails(exception, out errorDetails, out httpStatusCode, out operationId);
        }

        public static bool TryGetExceptionDetails(CommunicationException exception, out ServiceManagementError errorDetails, out HttpStatusCode httpStatusCode, out string operationId)
        {
            errorDetails = null;
            httpStatusCode = 0;
            operationId = null;

            if (exception == null)
            {
                return false;
            }

            if (exception.Message == "Internal Server Error")
            {
                httpStatusCode = HttpStatusCode.InternalServerError;
                return true;
            }

            var wex = exception.InnerException as WebException;

            if (wex == null)
            {
                return false;
            }

            var response = wex.Response as HttpWebResponse;
            if (response == null)
            {
                return false;
            }

            if (response.Headers != null)
            {
                operationId = response.Headers[WindowsAzure.ServiceManagement.Constants.OperationTrackingIdHeader];
            }

            using (var s = response.GetResponseStream())
            {
                try
                {
                    if (s == null || s.Length == 0)
                    {
                        return false;
                    }

                    var reader = XmlDictionaryReader.CreateTextReader(s, new XmlDictionaryReaderQuotas());
                    var ser = new DataContractSerializer(typeof(WindowsAzure.ServiceManagement.ServiceManagementError));
                    errorDetails = (ServiceManagementError)ser.ReadObject(reader, true);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }
    }

}