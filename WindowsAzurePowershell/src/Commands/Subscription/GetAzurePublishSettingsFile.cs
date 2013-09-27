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
    using System.Management.Automation;
    using System.Security.Permissions;
    using Commands.Utilities.Common;

    /// <summary>
    /// Get publish profile
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "AzurePublishSettingsFile"), OutputType(typeof(bool))]
    public class GetAzurePublishSettingsFileCommand : CmdletBase
    {
        [Parameter(Position = 0, Mandatory = false, ValueFromPipelineByPropertyName = true,
            HelpMessage = "The targeted Windows Azure environment.")]
        [ValidateNotNullOrEmpty]
        public string Environment { get; set; }

        [Parameter(Position = 1, Mandatory = false, ValueFromPipelineByPropertyName = true, 
            HelpMessage = "Realm of the account.")]
        [ValidateNotNullOrEmpty]
        public string Realm { get; set; }

        [Parameter(Position = 2, Mandatory = false, ValueFromPipelineByPropertyName = true, 
            HelpMessage = "Returns true in success.")]
        public SwitchParameter PassThru { get; set; }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public override void ExecuteCmdlet()
        {
            WindowsAzureEnvironment environment;
            if (string.IsNullOrEmpty(Environment))
            {
                environment = WindowsAzureProfile.Instance.CurrentEnvironment;
            }
            else
            {
                environment = WindowsAzureProfile.Instance.Environments[Environment];
            }

            string url = environment.PublishSettingsFileUrlWithRealm(Realm);
            General.LaunchWebPage(url);

            if (PassThru)
            {
                WriteObject(true);
            }
        }
    }
}