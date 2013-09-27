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
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Security.Permissions;
    using Commands.Utilities.Common;
    using Utilities.Properties;

    /// <summary>
    /// Removes a Windows Azure environment.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "AzureEnvironment"), OutputType(typeof(bool))]
    public class RemoveAzureEnvironmentCommand : CmdletBase
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, 
            HelpMessage = "The environment name")]
        public string Name { get; set; }

        [Parameter(Position = 1, Mandatory = false, HelpMessage = "Returns a Boolean in success")]
        public string PassThru { get; set; }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public override void ExecuteCmdlet()
        {
            try
            {
                WindowsAzureProfile.Instance.RemoveEnvironment(Name);
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(string.Format(Resources.EnvironmentNotFound, Name), ex);
            }
        }
    }
}
