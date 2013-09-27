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
    using System.Collections;
    using System.Collections.Generic;
    using System.Management.Automation;
    using Utilities.Websites.Common;
    using Utilities.Websites.Services.WebEntities;
    using Utilities.Common;

    /// <summary>
    /// Sets an azure website properties.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "AzureWebsite"), OutputType(typeof(bool))]
    public class SetAzureWebsiteCommand : WebsiteContextBaseCmdlet
    {
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Number of workers.")]
        [ValidateNotNullOrEmpty]
        public int? NumberOfWorkers { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Default Documents.")]
        public string[] DefaultDocuments { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = ".NET framework version.")]
        [ValidateNotNullOrEmpty]
        public string NetFrameworkVersion { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "PHP Version.")]
        [ValidateNotNullOrEmpty]
        public string PhpVersion { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Request tracing enabled.")]
        [ValidateNotNullOrEmpty]
        public bool? RequestTracingEnabled { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "HTTP Logging enabled.")]
        [ValidateNotNullOrEmpty]
        public bool? HttpLoggingEnabled { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Detailed Error Logging enabled.")]
        [ValidateNotNullOrEmpty]
        public bool? DetailedErrorLoggingEnabled { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Hostnames.")]
        [ValidateNotNullOrEmpty]
        public string[] HostNames { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "A string for the App Settings.")]
        public Hashtable AppSettings { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The Metadata.")]
        public List<NameValuePair> Metadata { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The Connection Strings.")]
        public ConnStringPropertyBag ConnectionStrings { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The Handler Mappings.")]
        public HandlerMapping[] HandlerMappings { get; set; }

        [Parameter(Position = 1, Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "A previous site configuration.")]
        public SiteWithConfig SiteWithConfig { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru { get; set; }

        private Site website;
        private SiteConfig currentSiteConfig;

        public override void ExecuteCmdlet()
        {
            base.ExecuteCmdlet();

            GetCurrentSiteState();
            UpdateConfig();
            UpdateHostNames();

            if (PassThru.IsPresent)
            {
                WriteObject(true);
            }
        }

        private void GetCurrentSiteState()
        {
            website = WebsitesClient.GetWebsite(Name);
            currentSiteConfig = WebsitesClient.GetWebsiteConfiguration(Name);
        }

        private void UpdateConfig()
        {
            bool changes = false;
            var websiteConfigUpdate = new SiteWithConfig(website, currentSiteConfig);
            if (SiteWithConfig != null)
            {
                websiteConfigUpdate = SiteWithConfig;
                changes = true;
            }

            changes = changes || ObjectDeltaMapper.Map(this, currentSiteConfig, websiteConfigUpdate, "HostNames", "SiteWithConfig", "PassThru");

            if (changes)
            {
                WebsitesClient.UpdateWebsiteConfiguration(Name, websiteConfigUpdate.GetSiteConfig());
            }
        }

        private void UpdateHostNames()
        {
            if (HostNames != null)
            {
                string suffix = WebsitesClient.GetWebsiteDnsSuffix(); 
                var newHostNames = new List<string> { string.Format("{0}.{1}", Name, suffix) };
                newHostNames.AddRange(HostNames);
                WebsitesClient.UpdateWebsiteHostNames(website, newHostNames);
            }
            
        }
    }
}
