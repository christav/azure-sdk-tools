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

namespace Microsoft.WindowsAzure.Commands.ServiceBus
{
    using System;
    using System.Management.Automation;
    using System.Text.RegularExpressions;
    using Commands.Utilities.Common;
    using Microsoft.WindowsAzure.Commands.Utilities.Properties;
    using Commands.Utilities.ServiceBus;
    using Commands.Utilities.ServiceBus.Contract;

    /// <summary>
    /// Creates new service bus namespace.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "AzureSBNamespace", SupportsShouldProcess = true), OutputType(typeof(bool))]
    public class RemoveAzureSBNamespaceCommand : CloudBaseCmdlet<IServiceBusManagement>
    {
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "Namespace name")]
        public string Name { get; set; }

        [Parameter(Position = 2, Mandatory = false)]
        public SwitchParameter PassThru { get; set; }

        [Parameter(Position = 3, HelpMessage = "Do not confirm the removal of the namespace")]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Initializes a new instance of the RemoveAzureSBNamespaceCommand class.
        /// </summary>
        public RemoveAzureSBNamespaceCommand()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the RemoveAzureSBNamespaceCommand class.
        /// </summary>
        /// <param name="channel">
        /// Channel used for communication with Azure's service management APIs.
        /// </param>
        public RemoveAzureSBNamespaceCommand(IServiceBusManagement channel)
        {
            Channel = channel;
        }

        /// <summary>
        /// Removes a service bus namespace.
        /// </summary>
        public override void ExecuteCmdlet()
        {
            string subscriptionId = CurrentSubscription.SubscriptionId;
            string name = Name;

            try
            {
                if (Regex.IsMatch(name, ServiceBusConstants.NamespaceNamePattern))
                {

                    ConfirmAction(
                        Force.IsPresent,
                        string.Format(Resources.RemoveServiceBusNamespaceConfirmation, name),
                        string.Format(Resources.RemovingNamespaceMessage),
                        name,
                        () =>
                        {
                            Channel.DeleteServiceBusNamespace(subscriptionId, name);

                            if (PassThru)
                            {
                                WriteObject(true);
                            }
                        });
                }
                else
                {
                    WriteExceptionError(new ArgumentException(string.Format(Resources.InvalidNamespaceName, name), "Name"));
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Equals(Resources.InternalServerErrorMessage))
                {
                    WriteExceptionError(new Exception(Resources.RemoveNamespaceErrorMessage));
                }
            }
        }
    }
}