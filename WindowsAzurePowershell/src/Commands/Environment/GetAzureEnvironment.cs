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
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Security.Permissions;
    using Utilities.Common;
    using Utilities.CloudService;

    /// <summary>
    /// Gets the available Windows Azure environments.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "AzureEnvironment"), OutputType(typeof(List<WindowsAzureEnvironment>), typeof(WindowsAzureEnvironment))]
    public class GetAzureEnvironmentCommand : CmdletBase
    {
        [Parameter(Position = 0, Mandatory = false, ValueFromPipelineByPropertyName = true, 
            HelpMessage = "The environment name")]
        public string Name { get; set; }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public override void ExecuteCmdlet()
        {
            if (string.IsNullOrEmpty(Name))
            {
                List<PSObject> output = new List<PSObject>();
                WindowsAzureProfile.Instance.Environments.Values.ForEach(e => output.Add(base.ConstructPSObject(
                    null,
                    Parameters.EnvironmentName, e.Name,
                    Parameters.ServiceEndpoint, e.ServiceEndpoint,
                    Parameters.PublishSettingsFileUrl, e.PublishSettingsFileUrl)));

                WriteObject(output, true);
            }
            else
            {
                WriteObject(WindowsAzureProfile.Instance.Environments[Name]);
            }
        }
    }
}