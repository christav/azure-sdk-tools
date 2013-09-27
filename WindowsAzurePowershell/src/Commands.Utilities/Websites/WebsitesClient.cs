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

namespace Microsoft.WindowsAzure.Commands.Utilities.Websites
{
    using CloudService;
    using Management.WebSites;
    using Management.WebSites.Models;
    using Newtonsoft.Json.Linq;
    using Properties;
    using Services;
    using Services.DeploymentEntities;
    using Services.WebEntities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Web;
    using Utilities.Common;

    public class WebsitesClient : IWebsitesClient
    {
        private readonly CloudServiceClient cloudServiceClient;

        public const string WebsitesServiceVersion = "2012-12-01";

        public IWebSiteManagementClient WebsiteManagementClient { get; internal set; }

        public Action<string> Logger { get; set; }

        /// <summary>
        /// Creates new WebsitesClient
        /// </summary>
        /// <param name="subscription">Subscription containing websites to manipulate</param>
        /// <param name="logger">The logger action</param>
        public WebsitesClient(WindowsAzureSubscription subscription, Action<string> logger)
        {
            Logger = logger;
            cloudServiceClient = new CloudServiceClient(subscription, debugStream: logger);
            WebsiteManagementClient = subscription.CreateClient<WebSiteManagementClient>();
        }

        /// <summary>
        /// Gets website name in the current directory.
        /// </summary>
        /// <returns></returns>
        private string GetWebsiteFromCurrentDirectory()
        {
            return GitWebsite.ReadConfiguration().Name;
        }

        private Repository GetRepository(string websiteName)
        {
            Site site = WebsiteManagementClient.GetSiteWithCache(websiteName);

            if (site != null)
            {
                return new Repository(site);
            }

            throw new Exception(Resources.RepositoryNotSetup);
        }

        private HttpClient CreateDeploymentHttpClient(string websiteName)
        {
            Repository repository;
            ICredentials credentials;
            GetWebsiteDeploymentHttpConfiguration(websiteName, out repository, out credentials);
            return HttpClientHelper.CreateClient(repository.RepositoryUri, credentials);
        }

        private string GetWebsiteDeploymentHttpConfiguration(
            string name,
            out Repository repository,
            out ICredentials credentials)
        {
            name = GetWebsiteName(name);
            repository = GetRepository(name);
            credentials = new NetworkCredential(
                repository.PublishingUsername,
                repository.PublishingPassword);
            return name;
        }

        private string GetWebsiteName(string name)
        {
            return string.IsNullOrEmpty(name) ? GetWebsiteFromCurrentDirectory() : name;
        }

        private void ChangeWebsiteState(string name, string webspace, WebsiteState state)
        {
            WebsiteManagementClient.WebSites.Update(webspace, name, new WebSiteUpdateParameters
            {
                State = state == WebsiteState.Running ? WebSiteState.Running : WebSiteState.Stopped
            });
        }

        private void SetApplicationDiagnosticsSettings(
            string name,
            WebsiteDiagnosticOutput output,
            bool setFlag,
            Dictionary<DiagnosticProperties, object> properties = null)
        {
            Site website = GetWebsite(name);

            using (HttpClient client = CreateDeploymentHttpClient(website.Name))
            {
                DiagnosticsSettings diagnosticsSettings = GetApplicationDiagnosticsSettings(website.Name);
                switch (output)
                {
                    case WebsiteDiagnosticOutput.FileSystem:
                        diagnosticsSettings.AzureDriveTraceEnabled = setFlag;
                        diagnosticsSettings.AzureDriveTraceLevel = setFlag ?
                        (LogEntryType)properties[DiagnosticProperties.LogLevel] : 
                        diagnosticsSettings.AzureDriveTraceLevel;
                        break;

                    case WebsiteDiagnosticOutput.StorageTable:
                        diagnosticsSettings.AzureTableTraceEnabled = setFlag;
                        if (setFlag)
                        {
                            const string storageTableName = "CLOUD_STORAGE_ACCOUNT";
                            string storageAccountName = (string)properties[DiagnosticProperties.StorageAccountName];
                            string connectionString = cloudServiceClient.GetStorageServiceConnectionString(
                                storageAccountName);
                            SetConnectionString(website.Name, storageTableName, connectionString, DatabaseType.Custom);
                            
                            diagnosticsSettings.AzureTableTraceLevel = setFlag ?
                                (LogEntryType)properties[DiagnosticProperties.LogLevel] : 
                                diagnosticsSettings.AzureTableTraceLevel;
                        }
                        break;

                    default:
                        throw new ArgumentException();
                }

                JObject json = new JObject(
                    new JProperty(UriElements.AzureDriveTraceEnabled, diagnosticsSettings.AzureDriveTraceEnabled),
                    new JProperty(UriElements.AzureDriveTraceLevel, diagnosticsSettings.AzureDriveTraceLevel.ToString()),
                    new JProperty(UriElements.AzureTableTraceEnabled, diagnosticsSettings.AzureTableTraceEnabled),
                    new JProperty(UriElements.AzureTableTraceLevel, diagnosticsSettings.AzureTableTraceLevel.ToString()));
                client.PostJson(UriElements.DiagnosticsSettings, json, Logger);
            }
        }

        private void SetSiteDiagnosticsSettings(
            string name,
            bool webServerLogging,
            bool detailedErrorMessages,
            bool failedRequestTracing,
            bool setFlag)
        {
            Site website = GetWebsite(name);

            var update = WebsiteManagementClient.WebSites.GetConfiguration(website.WebSpace, website.Name).ToUpdate();
            update.HttpLoggingEnabled = webServerLogging ? setFlag : update.HttpLoggingEnabled;
            update.DetailedErrorLoggingEnabled = detailedErrorMessages ? setFlag : update.DetailedErrorLoggingEnabled;
            update.RequestTracingEnabled = failedRequestTracing ? setFlag : update.RequestTracingEnabled;

            WebsiteManagementClient.WebSites.UpdateConfiguration(website.WebSpace, website.Name, update);
        }

        /// <summary>
        /// Starts log streaming for the given website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="path">The log path, by default root</param>
        /// <param name="message">The substring message</param>
        /// <param name="endStreaming">Predicate to end streaming</param>
        /// <param name="waitInterval">The fetch wait interval</param>
        /// <returns>The log line</returns>
        public IEnumerable<string> StartLogStreaming(
            string name,
            string path,
            string message,
            Predicate<string> endStreaming,
            int waitInterval)
        {
            Repository repository;
            ICredentials credentials;
            name = GetWebsiteDeploymentHttpConfiguration(name, out repository, out credentials);
            path = HttpUtility.UrlEncode(path);
            message = HttpUtility.UrlEncode(message);

            RemoteLogStreamManager manager = new RemoteLogStreamManager(
                repository.RepositoryUri,
                path,
                message,
                credentials,
                Logger);

            using (LogStreamWaitHandle logHandler = new LogStreamWaitHandle(manager.GetStream().Result))
            {
                bool doStreaming = true;

                while (doStreaming)
                {
                    string line = logHandler.WaitNextLine(waitInterval);

                    if (line != null)
                    {
                        yield return line;
                    }

                    doStreaming = endStreaming == null || endStreaming(line);
                }
            }
        }

        /// <summary>
        /// List log paths for a given website.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<LogPath> ListLogPaths(string name)
        {
            using (HttpClient client = CreateDeploymentHttpClient(name))
            {
                return client.GetJson<List<LogPath>>(UriElements.LogPaths, Logger);
            }
        }

        /// <summary>
        /// Gets the application diagnostics settings
        /// </summary>
        /// <param name="name">The website name</param>
        /// <returns>The website application diagnostics settings</returns>
        public DiagnosticsSettings GetApplicationDiagnosticsSettings(string name)
        {
            using (HttpClient client = CreateDeploymentHttpClient(name))
            {
                return client.GetJson<DiagnosticsSettings>(UriElements.DiagnosticsSettings, Logger);
            }
        }

        /// <summary>
        /// Restarts a website.
        /// </summary>
        /// <param name="name">The website name</param>
        public void RestartAzureWebsite(string name)
        {
            Site website = GetWebsite(name);
            ChangeWebsiteState(website.Name, website.WebSpace, WebsiteState.Stopped);
            ChangeWebsiteState(website.Name, website.WebSpace, WebsiteState.Running);
        }

        /// <summary>
        /// Starts a website.
        /// </summary>
        /// <param name="name">The website name</param>
        public void StartAzureWebsite(string name)
        {
            Site website = GetWebsite(name);
            ChangeWebsiteState(website.Name, website.WebSpace, WebsiteState.Running);
        }

        /// <summary>
        /// Stops a website.
        /// </summary>
        /// <param name="name">The website name</param>
        public void StopAzureWebsite(string name)
        {
            Site website = GetWebsite(name);
            ChangeWebsiteState(website.Name, website.WebSpace, WebsiteState.Stopped);
        }

        /// <summary>
        /// Gets a website instance.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <returns>The website instance</returns>
        public Site GetWebsite(string name)
        {
            name = GetWebsiteName(name);
            Site website = WebsiteManagementClient.GetSiteWithCache(name);

            if (website == null)
            {
                throw new Exception(string.Format(Resources.InvalidWebsite, name));
            }

            return website;
        }

        /// <summary>
        /// Create a new website.
        /// </summary>
        /// <param name="webspaceName">Web space to create site in.</param>
        /// <param name="siteToCreate">Details about the site to create.</param>
        /// <returns></returns>
        public Site CreateWebsite(string webspaceName, SiteWithWebSpace siteToCreate)
        {
            var options = new WebSiteCreateParameters
            {
                Name = siteToCreate.Name,
                WebSpace = new WebSiteCreateParameters.WebSpaceDetails
                {
                     GeoRegion = siteToCreate.WebSpaceToCreate.GeoRegion,
                     Name = siteToCreate.WebSpaceToCreate.Name,
                     Plan = siteToCreate.WebSpaceToCreate.Plan
                }
            };
            siteToCreate.HostNames.ForEach(s => options.HostNames.Add(s));

            var response = WebsiteManagementClient.WebSites.Create(webspaceName, options);
            return response.WebSite.ToSite();
        }

        /// <summary>
        /// Update the set of host names for a website.
        /// </summary>
        /// <param name="site">The site name.</param>
        /// <param name="hostNames">The new host names.</param>
        public void UpdateWebsiteHostNames(Site site, IEnumerable<string> hostNames)
        {
            var update = new WebSiteUpdateHostNamesParameters();
            foreach (var name in hostNames)
            {
                update.HostNames.Add(name);
            }

            WebsiteManagementClient.WebSites.UpdateHostNames(site.WebSpace, site.Name, update);
        }

        /// <summary>
        /// Gets the website configuration.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <returns>The website configuration object</returns>
        public SiteConfig GetWebsiteConfiguration(string name)
        {
            Site website = GetWebsite(name);
            SiteConfig configuration =
                WebsiteManagementClient.WebSites.GetConfiguration(website.WebSpace, website.Name).ToSiteConfig();

            return configuration;
        }

        /// <summary>
        /// Update the website configuration
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="newConfiguration">The website configuration object containing updates.</param>
        public void UpdateWebsiteConfiguration(string name, SiteConfig newConfiguration)
        {
            Site website = GetWebsite(name);
            WebsiteManagementClient.WebSites.UpdateConfiguration(website.WebSpace, name,
                newConfiguration.ToConfigUpdateParameters());
        }

        /// <summary>
        /// Create a git repository for the web site.
        /// </summary>
        /// <param name="webspaceName">Webspace that site is in.</param>
        /// <param name="websiteName">The site name.</param>
        public void CreateWebsiteRepository(string webspaceName, string websiteName)
        {
            WebsiteManagementClient.WebSites.CreateRepository(webspaceName, websiteName);
        }

        /// <summary>
        /// Delete a website.
        /// </summary>
        /// <param name="webspaceName">webspace the site is in.</param>
        /// <param name="websiteName">website name.</param>
        /// <param name="deleteMetrics">pass true to delete stored metrics as part of removing site.</param>
        /// <param name="deleteEmptyServerFarm">Pass true to delete server farm is this was the last website in it.</param>
        public void DeleteWebsite(string webspaceName, string websiteName, bool deleteMetrics = false, bool deleteEmptyServerFarm = false)
        {
            WebsiteManagementClient.WebSites.Delete(webspaceName, websiteName, deleteEmptyServerFarm, deleteMetrics);
        }

        /// <summary>
        /// Get the WebSpaces.
        /// </summary>
        /// <returns>Collection of WebSpace objects</returns>
        public IList<WebSpace> ListWebSpaces()
        {
            return WebsiteManagementClient.WebSpaces.List().WebSpaces.Select(ws => ws.ToWebSpace()).ToList();
        }

        /// <summary>
        /// Get the sites in the given webspace
        /// </summary>
        /// <param name="spaceName">Name of webspace</param>
        /// <returns>The sites</returns>
        public IList<Site> ListSitesInWebSpace(string spaceName)
        {
            return WebsiteManagementClient.WebSpaces.ListWebsites(spaceName).WebSites.Select(s => s.ToSite()).ToList();
        }

        /// <summary>
        /// Sets an AppSetting of a website.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="key">The app setting name</param>
        /// <param name="value">The app setting value</param>
        public void SetAppSetting(string name, string key, string value)
        {
            Site website = GetWebsite(name);
            var update = WebsiteManagementClient.WebSites.GetConfiguration(website.WebSpace, website.Name).ToUpdate();
            
            update.AppSettings[name] = key;

            WebsiteManagementClient.WebSites.UpdateConfiguration(website.WebSpace, website.Name, update);
        }

        /// <summary>
        /// Sets a connection string for a website.
        /// </summary>
        /// <param name="name">Name of the website.</param>
        /// <param name="key">Connection string key.</param>
        /// <param name="value">Value for the connection string.</param>
        /// <param name="connectionStringType">Type of connection string.</param>
        public void SetConnectionString(string name, string key, string value, DatabaseType connectionStringType)
        {
            Site website = GetWebsite(name);

            var update = WebsiteManagementClient.WebSites.GetConfiguration(website.WebSpace, website.Name).ToUpdate();

            var csToUpdate = update.ConnectionStrings.FirstOrDefault(cs => cs.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (csToUpdate == null)
            {
                csToUpdate = new WebSiteUpdateConfigurationParameters.ConnectionStringInfo
                {
                    ConnectionString = value,
                    Name = key,
                    Type = connectionStringType.ToString()
                };
                update.ConnectionStrings.Add(csToUpdate);
            }
            else
            {
                csToUpdate.ConnectionString = value;
                csToUpdate.Type = connectionStringType.ToString();
            }

            WebsiteManagementClient.WebSites.UpdateConfiguration(website.WebSpace, website.Name, update);
        }

        /// <summary>
        /// Enables website diagnostic.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="webServerLogging">Flag for webServerLogging</param>
        /// <param name="detailedErrorMessages">Flag for detailedErrorMessages</param>
        /// <param name="failedRequestTracing">Flag for failedRequestTracing</param>
        public void EnableSiteDiagnostic(
            string name,
            bool webServerLogging,
            bool detailedErrorMessages,
            bool failedRequestTracing)
        {
            SetSiteDiagnosticsSettings(name, webServerLogging, detailedErrorMessages, failedRequestTracing, true);
        }

        /// <summary>
        /// Disables site diagnostic.
        /// </summary>
        /// <param name="name">The website name</param>
        /// <param name="webServerLogging">Flag for webServerLogging</param>
        /// <param name="detailedErrorMessages">Flag for detailedErrorMessages</param>
        /// <param name="failedRequestTracing">Flag for failedRequestTracing</param>
        public void DisableSiteDiagnostic(
            string name,
            bool webServerLogging,
            bool detailedErrorMessages,
            bool failedRequestTracing)
        {
            SetSiteDiagnosticsSettings(name, webServerLogging, detailedErrorMessages, failedRequestTracing, false);
        }

        public void EnableApplicationDiagnostic(
            string name,
            WebsiteDiagnosticOutput output,
            Dictionary<DiagnosticProperties, object> properties)
        {
            SetApplicationDiagnosticsSettings(name, output, true, properties);
        }

        public void DisableApplicationDiagnostic(string name, WebsiteDiagnosticOutput output)
        {
            SetApplicationDiagnosticsSettings(name, output, false);
        }

        /// <summary>
        /// Lists available website locations.
        /// </summary>
        /// <returns>List of location names</returns>
        public List<string> ListAvailableLocations()
        {
            var webspacesGeoRegions = WebsiteManagementClient.WebSpaces.List()
                .WebSpaces.Select(w => w.GeoRegion);

            var availableRegionsResponse = WebsiteManagementClient.WebSpaces.ListGeoRegions();

            return availableRegionsResponse.GeoRegions.Select(r => r.Name).Union(webspacesGeoRegions).ToList();
        }

        /// <summary>
        /// Gets the default website DNS suffix for the current environment.
        /// </summary>
        /// <returns>The website DNS suffix</returns>
        public string GetWebsiteDnsSuffix()
        {
            return WebsiteManagementClient.WebSpaces.GetDnsSuffix().DnsSuffix;
        }

        /// <summary>
        /// Gets the default location for websites.
        /// </summary>
        /// <returns>The default location name.</returns>
        public string GetDefaultLocation()
        {
            return ListAvailableLocations().First();
        }

        /// <summary>
        /// Get a list of the user names configured to publish to the space.
        /// </summary>
        /// <returns>The list of user names.</returns>
        public IList<string> ListPublishingUserNames()
        {
            return WebsiteManagementClient.WebSpaces.ListPublishingUsers()
                .Users.Select(u => u.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();
        }
    }
}
