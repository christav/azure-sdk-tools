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
    using Utilities.Common;
    using Utilities.Properties;

    [Cmdlet(VerbsCommon.Select, "AzureSubscription", DefaultParameterSetName = "Current")]
    [OutputType(typeof(bool))]
    public class SelectAzureSubscriptionCommand : CmdletWithSubscriptionBase
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Current", HelpMessage = "Name of subscription to select")]
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default", HelpMessage = "Name of subscription to select")]
        [ValidateNotNullOrEmpty]
        public string SubscriptionName { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = "Current", HelpMessage = "Switch to set the chosen subscription as the current one")]
        public SwitchParameter Current { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Default", HelpMessage = "Switch to set the chosen subscription as the default one")]
        public SwitchParameter Default { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "NoCurrent", HelpMessage = "Switch to clear the current subscription")]
        public SwitchParameter NoCurrent { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "NoDefault", HelpMessage = "Switch to clear the default subscription")]
        public SwitchParameter NoDefault { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru { get; set; }

        public override void ExecuteCmdlet()
        {
            switch (ParameterSetName)
            {
                case "Current":
                    SetCurrent();
                    break;

                case "Default":
                    SetDefault();
                    break;

                case "NoCurrent":
                    ClearCurrent();
                    break;

                case "NoDefault":
                    ClearDefault();
                    break;
            }

            if (PassThru.IsPresent)
            {
                WriteObject(true);
            }
        }

        public void SetCurrent()
        {
            Profile.CurrentSubscription = FindNamedSubscription();
        }

        public void SetDefault()
        {
            var newDefault = FindNamedSubscription();
            newDefault.IsDefault = true;
            Profile.UpdateSubscription(newDefault);
        }

        public void ClearCurrent()
        {
            Profile.CurrentSubscription = null;
        }

        public void ClearDefault()
        {
            var defaultSubscription = Profile.DefaultSubscription;
            defaultSubscription.IsDefault = false;
            Profile.UpdateSubscription(defaultSubscription);
        }

        private WindowsAzureSubscription FindNamedSubscription()
        {
            var subscription = Profile.Subscriptions.FirstOrDefault(s => s.Name == SubscriptionName);
            if (subscription == null)
            {
                throw new Exception(string.Format(Resources.InvalidSubscription, SubscriptionName));
            }
            return subscription;
        }
    }
}