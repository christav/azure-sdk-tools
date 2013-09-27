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

namespace Microsoft.WindowsAzure.Commands.Subscription
{
    using System;
    using System.Linq;
    using System.Management.Automation;
    using System.Security.Cryptography.X509Certificates;
    using Utilities.Common;

    /// <summary>
    /// Sets an azure subscription.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "AzureSubscription", DefaultParameterSetName = "CommonSettings"), OutputType(typeof(bool))]
    public class SetAzureSubscriptionCommand : CmdletWithSubscriptionBase
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Name of the subscription.", ParameterSetName = "CommonSettings")]
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Name of the subscription.", ParameterSetName = "ResetCurrentStorageAccount")]
        [ValidateNotNullOrEmpty]
        public string SubscriptionName { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Account subscription ID.", ParameterSetName = "CommonSettings")]
        public string SubscriptionId { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Account certificate.", ParameterSetName = "CommonSettings")]
        public X509Certificate2 Certificate { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Service endpoint.", ParameterSetName = "CommonSettings")]
        public string ServiceEndpoint { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Current storage account name.", ParameterSetName = "CommonSettings")]
        [ValidateNotNullOrEmpty]
        public string CurrentStorageAccount { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru { get; set; }

        protected virtual void WriteMessage(string message)
        {
            WriteVerbose(message);
        }

        /// <summary>
        /// Executes the set subscription cmdlet operation.
        /// </summary>
        internal void SetSubscriptionProcess()
        {
            WindowsAzureSubscription subscription = Profile.Subscriptions.FirstOrDefault(s => s.Name == SubscriptionName);
            if (subscription == null)
            {
                CreateNewSubscription();
            }
            else
            {
                UpdateExistingSubscription(subscription);
            }
        }

        private void CreateNewSubscription()
        {
            var subscription = new WindowsAzureSubscription
            {
                Name = SubscriptionName,
                SubscriptionId = SubscriptionId,
                Certificate = Certificate,
                CurrentStorageAccountName = CurrentStorageAccount
            };

            if (string.IsNullOrEmpty(ServiceEndpoint))
            {
                subscription.ServiceEndpoint = new Uri(Profile.CurrentEnvironment.ServiceEndpoint);
            }
            else
            {
                subscription.ServiceEndpoint = new Uri(ServiceEndpoint);
            }

            Profile.AddSubscription(subscription);
        }

        private void UpdateExistingSubscription(WindowsAzureSubscription subscription)
        {
            if (!string.IsNullOrEmpty(SubscriptionId))
            {
                subscription.SubscriptionId = SubscriptionId;
            }

            if (Certificate != null)
            {
                subscription.Certificate = Certificate;
            }

            if (ServiceEndpoint != null)
            {
                subscription.ServiceEndpoint = new Uri(ServiceEndpoint);
            }

            if (CurrentStorageAccount != null)
            {
                subscription.CurrentStorageAccountName = CurrentStorageAccount;
            }

            Profile.UpdateSubscription(subscription);
        }

        public override void ExecuteCmdlet()
        {
            try
            {
                SetSubscriptionProcess();
                if (PassThru.IsPresent)
                {
                    WriteObject(true);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, string.Empty, ErrorCategory.InvalidData, null));
            }
        }
    }
}