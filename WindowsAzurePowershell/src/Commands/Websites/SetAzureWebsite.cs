﻿// ----------------------------------------------------------------------------------
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
    using System.Collections;
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.ServiceModel;
    using Microsoft.WindowsAzure.Commands.Utilities.Properties;
    using Commands.Utilities.Store;
    using Commands.Utilities.Websites;
    using Commands.Utilities.Websites.Common;
    using Commands.Utilities.Websites.Services;
    using Commands.Utilities.Websites.Services.DeploymentEntities;
    using Commands.Utilities.Websites.Services.WebEntities;

    /// <summary>
    /// Sets an azure website properties.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "AzureWebsite"), OutputType(typeof(bool))]
    public class SetAzureWebsiteCommand : WebsiteContextBaseCmdlet
    {
        private IWebsitesClient websitesClient;

        public IWebsitesClient WebsitesClient
        {
            get
            {
                if (websitesClient == null)
                {
                    websitesClient = new WebsitesClient(CurrentSubscription, WriteDebug);
                }
                return websitesClient;
            }

            set { websitesClient = value; }
        }

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

        private SiteConfig currentSiteConfig;

        public override void ExecuteCmdlet()
        {
            base.ExecuteCmdlet();
            string suffix = WebsitesClient.GetWebsiteDnsSuffix();

            Site website = null;
            SiteConfig websiteConfig = null;

            try
            {
                website = WebsitesClient.GetWebsite(Name);
                currentSiteConfig = WebsitesClient.GetWebsiteConfiguration(Name);
                website = RetryCall(s => Channel.GetSiteWithCache(s, Name, null));
                websiteConfig = RetryCall(s => Channel.GetSiteConfig(s, website.WebSpace, Name));
            }
            catch (CommunicationException ex)
            {
                WriteErrorDetails(ex);
            }

            if (website == null)
            {
                throw new Exception(string.Format(Resources.InvalidWebsite, Name));
            }

            bool changes = false;
            SiteWithConfig websiteConfigUpdate = new SiteWithConfig(website, websiteConfig);
            if (SiteWithConfig != null)
            {
                websiteConfigUpdate = SiteWithConfig;
                changes = true;
            }

            if (NumberOfWorkers != null && !NumberOfWorkers.Equals(websiteConfig.NumberOfWorkers))
            {
                changes = true;
                websiteConfigUpdate.NumberOfWorkers = NumberOfWorkers;
            }

            if (DefaultDocuments != null && !DefaultDocuments.Equals(websiteConfig.DefaultDocuments))
            {
                changes = true;
                websiteConfigUpdate.DefaultDocuments = DefaultDocuments;
            }

            if (NetFrameworkVersion != null && !NetFrameworkVersion.Equals(websiteConfig.NetFrameworkVersion))
            {
                changes = true;
                websiteConfigUpdate.NetFrameworkVersion = NetFrameworkVersion;
            }

            if (PhpVersion != null && !PhpVersion.Equals(websiteConfig.PhpVersion))
            {
                changes = true;
                websiteConfigUpdate.PhpVersion = PhpVersion;
            }

            if (RequestTracingEnabled != null && !RequestTracingEnabled.Equals(websiteConfig.RequestTracingEnabled))
            {
                changes = true;
                websiteConfigUpdate.RequestTracingEnabled = RequestTracingEnabled;
            }

            if (HttpLoggingEnabled != null && !HttpLoggingEnabled.Equals(websiteConfig.HttpLoggingEnabled))
            {
                changes = true;
                websiteConfigUpdate.HttpLoggingEnabled = HttpLoggingEnabled;
            }

            if (DetailedErrorLoggingEnabled != null && !DetailedErrorLoggingEnabled.Equals(websiteConfig.DetailedErrorLoggingEnabled))
            {
                changes = true;
                websiteConfigUpdate.DetailedErrorLoggingEnabled = DetailedErrorLoggingEnabled;
            }

            if (AppSettings != null && !AppSettings.Equals(websiteConfig.AppSettings))
            {
                changes = true;
                websiteConfigUpdate.AppSettings = AppSettings;
            }

            if (Metadata != null && !Metadata.Equals(websiteConfig.Metadata))
            {
                changes = true;
                websiteConfigUpdate.Metadata = Metadata;
            }

            if (ConnectionStrings != null && !ConnectionStrings.Equals(websiteConfig.ConnectionStrings))
            {
                changes = true;
                websiteConfigUpdate.ConnectionStrings = ConnectionStrings;
            }

            if (HandlerMappings != null && !HandlerMappings.Equals(websiteConfig.HandlerMappings))
            {
                changes = true;
                websiteConfigUpdate.HandlerMappings = HandlerMappings;
            }

            bool siteChanges = false;
            Site websiteUpdate = new Site
            {
                Name = Name,
                HostNames = new[] { string.Format("{0}.{1}", Name, suffix) }
            };
            if (HostNames != null)
            {
                siteChanges = true;
                List<string> newHostNames = new List<string> { string.Format("{0}.{1}", Name, suffix) };
                newHostNames.AddRange(HostNames);
                websiteUpdate.HostNames = newHostNames.ToArray();
            }

            if (changes)
            {
                InvokeInOperationContext(() =>
                {
                    try
                    {
                        RetryCall(s => Channel.UpdateSiteConfig(s, website.WebSpace, Name, websiteConfigUpdate.GetSiteConfig()));
                    }
                    catch (CommunicationException ex)
                    {
                        WriteErrorDetails(ex);
                    }
                });
            }

            if (siteChanges)
            {
                InvokeInOperationContext(() =>
                {
                    try
                    {
                        RetryCall(s => Channel.UpdateSite(s, website.WebSpace, Name, websiteUpdate));
                    }
                    catch (CommunicationException ex)
                    {
                        WriteErrorDetails(ex);
                    }
                });
            }

            if (PassThru.IsPresent)
            {
                WriteObject(true);
            }
        }
    }
}