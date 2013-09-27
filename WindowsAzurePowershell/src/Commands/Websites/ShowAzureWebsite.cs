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

namespace Microsoft.WindowsAzure.Commands.Websites
{
    using System.Linq;
    using System.Management.Automation;
    using Utilities.Common;
    using Utilities.Websites.Common;
    using Utilities.Websites.Services.WebEntities;

    /// <summary>
    /// Shows an azure website.
    /// </summary>
    [Cmdlet(VerbsCommon.Show, "AzureWebsite")]
    public class ShowAzureWebsiteCommand : WebsiteContextBaseCmdlet
    {
        public override void ExecuteCmdlet()
        {
            Site websiteObject = WebsitesClient.GetWebsite(Name);
            General.LaunchWebPage("http://" + websiteObject.HostNames.First());
        }
    }
}
