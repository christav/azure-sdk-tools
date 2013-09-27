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
    using System.Globalization;
    using System.Linq;
    using System.Management.Automation;
    using Commands.Utilities.Common;
    using Microsoft.WindowsAzure.Commands.Utilities.Properties;

    /// <summary>
    /// Removes a previously imported subscription.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "AzureSubscription", SupportsShouldProcess = true), OutputType(typeof(bool))]
    public class RemoveAzureSubscriptionCommand : CmdletWithSubscriptionBase
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Name of the subscription.")]
        [ValidateNotNullOrEmpty]
        public string SubscriptionName { get; set; }

        [Parameter(Position = 2, HelpMessage = "Do not confirm deletion of subscription")]
        public SwitchParameter Force { get; set; }

        [Parameter(Position = 3, Mandatory = false)]
        public SwitchParameter PassThru { get; set; }

        public void RemoveSubscriptionProcess()
        {
            var subscription = Profile.Subscriptions.FirstOrDefault(s => s.Name == SubscriptionName);
            if (subscription != null)
            {
                // Warn the user if the removed subscription is the default one.
                if (subscription.IsDefault)
                {
                    WriteWarning(Resources.RemoveDefaultSubscription);
                }

                // Warn the user if the removed subscription is the current one.
                if (subscription == Profile.CurrentSubscription)
                {
                    WriteWarning(Resources.RemoveCurrentSubscription);
                }

                Profile.RemoveSubscription(subscription);
                if (PassThru.IsPresent)
                {
                    WriteObject(true);
                }
            }
            else
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidSubscription, SubscriptionName));
            }
        }

        public override void ExecuteCmdlet()
        {
            ConfirmAction(
                Force.IsPresent,
                string.Format(Resources.RemoveSubscriptionConfirmation, SubscriptionName),
                Resources.RemoveSubscriptionMessage,
                SubscriptionName,
                RemoveSubscriptionProcess);
        }
    }
}