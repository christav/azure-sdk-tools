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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;
    using Utilities.Properties;
    using Utilities.Websites.Common;
    using Utilities.Websites.Services;
    using Utilities.Websites.Services.DeploymentEntities;
    using Utilities.Websites.Services.WebEntities;

    /// <summary>
    /// Gets an azure website.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "AzureWebsite"), OutputType(typeof(SiteWithConfig), typeof(IEnumerable<Site>))]
    public class GetAzureWebsiteCommand : NewWebsitesBaseCmdlet
    {
        [Parameter(Position = 0, Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The web site name.")]
        [ValidateNotNullOrEmpty]
        public string Name
        {
            get;
            set;
        }

        protected virtual void WriteWebsites(IEnumerable<Site> websites)
        {
            WriteObject(websites, true);
        }

        public override void ExecuteCmdlet()
        {
            EnsureCurrentSubscription();

            if (!string.IsNullOrEmpty(Name))
            {
                GetByName();
            }
            else
            {
                GetNoName();
            }
        }

        private void GetByName()
        {
            Do(() =>
                {
                    Site websiteObject = WebsitesClient.GetWebsite(Name);
                    SiteConfig config = WebsitesClient.GetWebsiteConfiguration(Name);
                    Cache.AddSite(CurrentSubscription.SubscriptionId, websiteObject);

                    var diagnosticSettings = new DiagnosticsSettings();
                    try
                    {
                        diagnosticSettings = WebsitesClient.GetApplicationDiagnosticsSettings(Name);
                    }
                    catch
                    {
                        // Ignore exception and use default values
                    }

                    WriteObject(new SiteWithConfig(websiteObject, config, diagnosticSettings), false);
                });
        }

        private void GetNoName()
        {
            Do(() =>
                {
                    var websites = WebsitesClient.ListWebSpaces()
                        .SelectMany(space => WebsitesClient.ListSitesInWebSpace(space.Name))
                        .ToList();
                    Cache.SaveSites(CurrentSubscription.SubscriptionId, new Sites(websites));
                    WriteWebsites(websites);
                });
        }

        private void EnsureCurrentSubscription()
        {
            if (CurrentSubscription == null)
            {
                throw new Exception(Resources.NoDefaultSubscriptionMessage);
            }
        }

        private void Do(Action call)
        {
            try
            {
                call();
            }
            catch (CloudException ex)
            {
                if (ex.Response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    WriteError(new ErrorRecord(new Exception(Resources.CommunicationCouldNotBeEstablished, ex), string.Empty, ErrorCategory.InvalidData, null));
                    throw;
                }
                if (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new Exception(string.Format(Resources.InvalidWebsite, Name));
                }
                throw;
            }
        }
    }
}
