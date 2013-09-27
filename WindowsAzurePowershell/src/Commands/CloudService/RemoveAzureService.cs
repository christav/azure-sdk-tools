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

namespace Microsoft.WindowsAzure.Commands.CloudService
{
    using System.Management.Automation;
    using Utilities.CloudService;
    using Utilities.Common;
    using Utilities.Properties;

    /// <summary>
    /// Deletes the specified hosted service from Windows Azure.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "AzureService", SupportsShouldProcess = true), OutputType(typeof(bool))]
    public class RemoveAzureServiceCommand : CmdletWithSubscriptionBase
    {
        public ICloudServiceClient CloudServiceClient { get; set; }

        [Parameter(Position = 0, Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "name of the hosted service")]
        public string ServiceName { get; set; }

        [Parameter(Position = 1, HelpMessage = "Do not confirm deletion of deployment")]
        public SwitchParameter Force { get; set; }

        [Parameter(Position = 2, Mandatory = false)]
        public SwitchParameter PassThru { get; set; }

        public override void ExecuteCmdlet()
        {
            ConfirmAction(
                Force.IsPresent,
                string.Format(Resources.RemoveServiceWarning),
                Resources.RemoveServiceWhatIfMessage,
                string.Empty,
                () =>
                {
                    CloudServiceClient = CloudServiceClient ?? new CloudServiceClient(
                        CurrentSubscription,
                        SessionState.Path.CurrentLocation.Path,
                        WriteDebug,
                        WriteVerbose,
                        WriteWarning);

                    CloudServiceClient.RemoveCloudService(ServiceName);

                    if (PassThru)
                    {
                        WriteObject(true);
                    }
                });
        }
    }
}