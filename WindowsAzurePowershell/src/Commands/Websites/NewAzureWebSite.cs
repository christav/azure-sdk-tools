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
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Net;
    using System.Security.Permissions;
    using System.ServiceModel;
    using System.Text.RegularExpressions;
    using Utilities.Properties;
    using Utilities.Websites.Common;
    using Utilities.Websites.Services;
    using Utilities.Websites.Services.Github;
    using Utilities.Websites.Services.WebEntities;
    using GitClass = Utilities.Websites.Services.Git;

    /// <summary>
    /// Creates a new azure website.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "AzureWebsite"), OutputType(typeof(SiteWithConfig))]
    public class NewAzureWebsiteCommand : WebsiteContextBaseCmdlet, IGithubCmdlet
    {
        [Parameter(Position = 1, Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The geographic region to create the website.")]
        [ValidateNotNullOrEmpty]
        public string Location
        {
            get;
            set;
        }

        [Parameter(Position = 2, Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Custom host name to use.")]
        [ValidateNotNullOrEmpty]
        public string Hostname
        {
            get;
            set;
        }

        [Parameter(Position = 3, Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The publishing user name.")]
        [ValidateNotNullOrEmpty]
        public string PublishingUsername
        {
            get;
            set;
        }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Configure git on the web site and local folder.")]
        public SwitchParameter Git
        {
            get;
            set;
        }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Configure github on the web site.")]
        public SwitchParameter GitHub
        {
            get;
            set;
        }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The github credentials.")]
        [ValidateNotNullOrEmpty]
        public PSCredential GithubCredentials
        {
            get;
            set;
        }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The github repository.")]
        [ValidateNotNullOrEmpty]
        public string GithubRepository
        {
            get;
            set;
        }

        public IGithubServiceManagement GithubChannel { get; set; }

        /// <summary>
        /// Initializes a new instance of the NewAzureWebsiteCommand class.
        /// </summary>
        public NewAzureWebsiteCommand()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the NewAzureWebsiteCommand class.
        /// </summary>
        /// <param name="githubChannel">
        /// Channel used for communication with the github APIs.
        /// </param>
        public NewAzureWebsiteCommand(IGithubServiceManagement githubChannel)
        {
            GithubChannel = githubChannel;
        }

        internal void CopyIisNodeWhenServerJsPresent()
        {
            if (!File.Exists("iisnode.yml") && (File.Exists("server.js") || File.Exists("app.js")))
            {
                string cmdletPath = Directory.GetParent(MyInvocation.MyCommand.Module.Path).FullName;
                File.Copy(Path.Combine(cmdletPath, "Scaffolding/Node/Website/iisnode.yml"), "iisnode.yml");
            }
        }

        internal void UpdateLocalConfigWithSiteName(string websiteName, string webspace)
        {
            GitWebsite gitWebsite = new GitWebsite(websiteName, webspace);
            gitWebsite.WriteConfiguration();
        }

        internal string GetPublishingUser()
        {
            if (!string.IsNullOrEmpty(PublishingUsername))
            {
                return PublishingUsername;
            }

            // Get publishing users
            IList<string> users = null;
            try
            {
                users = WebsitesClient.ListPublishingUserNames();
            }
            catch
            {
                throw new Exception(Resources.NeedPublishingUsernames);
            }

            if (users.Count == 0)
            {
                throw new ArgumentException(Resources.InvalidGitCredentials);
            }
            
            if (users.Count != 1) 
            {
                throw new Exception(Resources.MultiplePublishingUsernames);
            }

            return users.First();
        }

        internal void InitializeRemoteRepo(string webspace, string websiteName)
        {
            try
            {
                // Create website repository
                WebsitesClient.CreateWebsiteRepository(webspace, websiteName);
            }
            catch (Exception ex)
            {
                if (SiteRepositoryAlreadyExists(ex))
                {
                    WriteWarning(ex.Message);
                }
                else
                {
                    WriteExceptionError(ex);
                }
            }
        }

        internal void AddRemoteToLocalGitRepo(Site website)
        {
            // Get remote repos
            IList<string> remoteRepositories = GitClass.GetRemoteRepositories();
            if (remoteRepositories.Any(repository => repository.Equals("azure")))
            {
                // Removing existing azure remote alias
                GitClass.RemoveRemoteRepository("azure");
            }

            string repositoryUri = website.GetProperty("RepositoryUri");

            string uri = GitClass.GetUri(repositoryUri, Name, PublishingUsername);
            GitClass.AddRemoteRepository("azure", uri);
        }

        [EnvironmentPermission(SecurityAction.Demand, Unrestricted = true)]
        public override void ExecuteCmdlet()
        {
            string suffix = WebsitesClient.GetWebsiteDnsSuffix();

            if (Git && GitHub)
            {
                throw new Exception("Please run the command with either -Git or -GitHub options. Not both.");
            }

            if (Git)
            {
                PublishingUsername = GetPublishingUser();
            }

            var webspace = CreateNewSite(suffix);
            if (Git || GitHub)
            {
                UpdateSourceControlPublishing(webspace);
            }
        }

        private void UpdateSourceControlPublishing(WebSpace webspace)
        {
            try
            {
                Directory.SetCurrentDirectory(SessionState.Path.CurrentFileSystemLocation.Path);
            }
            catch (Exception)
            {
                // Do nothing if session state is not present
            }

            using (LinkedRevisionControl linkedRevisionControl = CreateLinkedRevisionControl())
            {
                linkedRevisionControl.Init();

                CopyIisNodeWhenServerJsPresent();
                UpdateLocalConfigWithSiteName(Name, webspace.Name);

                InitializeRemoteRepo(webspace.Name, Name);

                Site updatedWebsite = WebsitesClient.GetWebsite(Name);

                if (Git)
                {
                    AddRemoteToLocalGitRepo(updatedWebsite);
                }

                linkedRevisionControl.Deploy(updatedWebsite);
            }
        }

        private LinkedRevisionControl CreateLinkedRevisionControl()
        {
            if (Git)
            {
                return new GitClient(this);
            }
            return new GithubClient(this, GithubCredentials, GithubRepository);
        }

        private WebSpace CreateNewSite(string suffix)
        {
            var webspaceList = WebsitesClient.ListWebSpaces();
            if (Git && webspaceList.Count == 0)
            {
                string error = string.Format(Resources.PortalInstructions, Name);
                throw new Exception(string.Format("{0}\n{1}", error, Resources.PortalInstructionsGit));
            }

            WebSpace webspace = FindWebSpace(webspaceList);

            var website = new SiteWithWebSpace
            {
                Name = Name,
                HostNames = BuildHostNames(suffix),
                WebSpace = webspace.Name,
                WebSpaceToCreate = webspace
            };

            try
            {
                CreateSite(webspace, website);
            }
            catch (EndpointNotFoundException)
            {
                // Create webspace with VirtualPlan failed, try with subscription id
                // This supports Windows Azure Pack
                webspace.Plan = CurrentSubscription.SubscriptionId;
                CreateSite(webspace, website);
            }
            return webspace;
        }
        
        private string[] BuildHostNames(string suffix)
        {
            var hostnames = new List<string> { string.Format(CultureInfo.InvariantCulture, "{0}.{1}", Name, suffix) };
            if (!string.IsNullOrEmpty(Hostname))
            {
                hostnames.Add(Hostname);
            }
            return hostnames.ToArray();
        }

        private WebSpace FindWebSpace(IList<WebSpace> webspaceList)
        {
            if (string.IsNullOrEmpty(Location))
            {
                return GetDefaultWebSpace(webspaceList);
            }
            return GetNamedWebSpace(webspaceList);
        }

        private WebSpace GetDefaultWebSpace(IList<WebSpace> webspaceList)
        {
            WebSpace webspace = webspaceList.FirstOrDefault();
            if (webspace == null)
            {
                try
                {
                    string defaultLocation = WebsitesClient.GetDefaultLocation();
                    webspace = WebSpaceForLocation(defaultLocation);
                }
                catch
                {
                    throw new Exception(Resources.CreateWebsiteFailed);
                }
            }
            return webspace;
        }

        private WebSpace GetNamedWebSpace(IList<WebSpace> webspaceList)
        {
            // Find the webspace that corresponds to the georegion
            return webspaceList.FirstOrDefault(w => w.GeoRegion.Equals(Location, StringComparison.OrdinalIgnoreCase)) ??
                WebSpaceForLocation(Location);
        }

        private WebSpace WebSpaceForLocation(string location)
        {
            return new WebSpace
            {
                Name = Regex.Replace(location.ToLower(), " ", "") + "webspace",
                GeoRegion = location,
                Subscription = CurrentSubscription.SubscriptionId,
                Plan = "VirtualDedicatedPlan"
            };
        }

        private void CreateSite(WebSpace webspace, SiteWithWebSpace website)
        {
            try
            {
                WebsitesClient.CreateWebsite(webspace.Name, website);
                Cache.AddSite(CurrentSubscription.SubscriptionId, website);
                SiteConfig websiteConfiguration = WebsitesClient.GetWebsiteConfiguration(Name);
                WriteObject(new SiteWithConfig(website, websiteConfiguration));
            }
            catch (CloudException ex)
            {
                if (SiteAlreadyExists(ex) && (Git || GitHub))
                {
                    // Handle conflict - it's ok to attempt to use cmdlet on an
                    // existing website if you're updating the source control stuff.
                    WriteWarning(ex.Message);
                }
                else if (HostNameValidationFailed(ex))
                {
                    WriteExceptionError(new Exception(Resources.InvalidHostnameValidation));
                }
                else if (BadPlan(ex))
                {
                    throw new EndpointNotFoundException();
                }
                else
                {
                    WriteExceptionError(new Exception(ex.Message));
                }
            }
        }

        public Action<string> GetLogger()
        {
            return WriteDebug;
        }

        private bool SiteAlreadyExists(CloudException ex)
        {
            // TODO: Verify this is the right error code/detection logic
            return ex.Response.StatusCode == HttpStatusCode.Conflict;
        }

        private bool HostNameValidationFailed(CloudException ex)
        {
            // TODO: Verify this is the right error code/detection logic
            return ex.Response.StatusCode == HttpStatusCode.BadRequest;
        }

        // Calling Windows Azure Pack, will fail due to plan string
        private bool BadPlan(CloudException ex)
        {
            // TODO: Verify this is the right error code/detection logic
            return ex.Response.StatusCode == HttpStatusCode.NotFound;
        }

        private bool SiteRepositoryAlreadyExists(Exception ex)
        {
            var cex = ex as CloudException;
            if (cex != null)
            {
                // TODO: Verify this is the right error code/detection logic
                return cex.Response.StatusCode == HttpStatusCode.BadRequest;
            }
            return false;
        }
    }
}
