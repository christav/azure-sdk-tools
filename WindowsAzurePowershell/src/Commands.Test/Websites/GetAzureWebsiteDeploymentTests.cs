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

namespace Microsoft.WindowsAzure.Commands.Test.Websites
{
    using System.Collections.Generic;
    using System.Linq;
    using Commands.Utilities.Common;
    using Commands.Utilities.Websites;
    using Moq;
    using Utilities.Common;
    using Utilities.Websites;
    using Commands.Utilities.Websites.Services.DeploymentEntities;
    using Commands.Utilities.Websites.Services.WebEntities;
    using Commands.Websites;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class GetAzureWebsiteDeploymentTests : WebsitesTestBase
    {
        [TestMethod]
        public void GetAzureWebsiteDeploymentTest()
        {
            // Setup
            var clientMock = new Mock<IWebsitesClient>();
            var site1 = new Site
            {
                Name = "website1",
                WebSpace = "webspace1",
                SiteProperties = new SiteProperties
                {
                    Properties = new List<NameValuePair>
                    {
                        new NameValuePair {Name = "repositoryuri", Value = "http"},
                        new NameValuePair {Name = "PublishingUsername", Value = "user1"},
                        new NameValuePair {Name = "PublishingPassword", Value = "password1"}
                    }
                }
            };

            clientMock.Setup(c => c.GetWebsite("website1"))
                .Returns(site1);

            clientMock.Setup(c => c.ListWebSpaces())
                .Returns(new[] { new WebSpace { Name = "webspace1" }, new WebSpace { Name = "webspace2" } });
            clientMock.Setup(c => c.ListSitesInWebSpace("webspace1"))
                .Returns(new[] { site1 });

            clientMock.Setup(c => c.ListSitesInWebSpace("webspace2"))
                .Returns(new[] { new Site { Name = "website2", WebSpace = "webspace2" } });

            SimpleDeploymentServiceManagement deploymentChannel = new SimpleDeploymentServiceManagement();
            deploymentChannel.GetDeploymentsThunk = ar => new List<DeployResult> { new DeployResult(), new DeployResult() };

            // Test
            GetAzureWebsiteDeploymentCommand getAzureWebsiteDeploymentCommand = new GetAzureWebsiteDeploymentCommand(deploymentChannel)
            {
                Name = "website1",
                ShareChannel = true,
                WebsitesClient = clientMock.Object,
                CommandRuntime = new MockCommandRuntime(),
                CurrentSubscription = new WindowsAzureSubscription { SubscriptionId = base.subscriptionId }
            };

            getAzureWebsiteDeploymentCommand.ExecuteCmdlet();
            Assert.AreEqual(1, ((MockCommandRuntime)getAzureWebsiteDeploymentCommand.CommandRuntime).OutputPipeline.Count);
            var deployments = (IEnumerable<DeployResult>)((MockCommandRuntime)getAzureWebsiteDeploymentCommand.CommandRuntime).OutputPipeline.FirstOrDefault();
            Assert.IsNotNull(deployments);
            Assert.AreEqual(2, deployments.Count());
        }

        [TestMethod]
        public void GetAzureWebsiteDeploymentLogsTest()
        {
            // Setup
            var clientMock = new Mock<IWebsitesClient>();
            var site1 = new Site
            {
                Name = "website1",
                WebSpace = "webspace1",
                SiteProperties = new SiteProperties
                {
                    Properties = new List<NameValuePair>
                    {
                        new NameValuePair {Name = "repositoryuri", Value = "http"},
                        new NameValuePair {Name = "PublishingUsername", Value = "user1"},
                        new NameValuePair {Name = "PublishingPassword", Value = "password1"}
                    }
                }
            };

            clientMock.Setup(c => c.ListWebSpaces())
                .Returns(new[] {new WebSpace {Name = "webspace1"}, new WebSpace {Name = "webspace2"}});
            clientMock.Setup(c => c.GetWebsite("website1")).Returns(site1);

            SimpleDeploymentServiceManagement deploymentChannel = new SimpleDeploymentServiceManagement();
            deploymentChannel.GetDeploymentsThunk = ar => new List<DeployResult> { new DeployResult { Id = "commit1" }, new DeployResult { Id = "commit2" } };
            deploymentChannel.GetDeploymentLogsThunk = ar =>
            {
                if (ar.Values["commitId"].Equals("commit1"))
                {
                    return new List<LogEntry> { new LogEntry { Id = "log1" }, new LogEntry { Id = "log2" } };
                }

                return new List<LogEntry>();
            };

            // Test
            GetAzureWebsiteDeploymentCommand getAzureWebsiteDeploymentCommand = new GetAzureWebsiteDeploymentCommand(deploymentChannel)
            {
                Name = "website1",
                ShareChannel = true,
                WebsitesClient = clientMock.Object,
                Details = true,
                CommandRuntime = new MockCommandRuntime(),
                CurrentSubscription = new WindowsAzureSubscription { SubscriptionId = base.subscriptionId }
            };

            getAzureWebsiteDeploymentCommand.ExecuteCmdlet();
            Assert.AreEqual(1, ((MockCommandRuntime)getAzureWebsiteDeploymentCommand.CommandRuntime).OutputPipeline.Count);
            var deployments = (IEnumerable<DeployResult>)((MockCommandRuntime)getAzureWebsiteDeploymentCommand.CommandRuntime).OutputPipeline.FirstOrDefault();
            Assert.IsNotNull(deployments);
            Assert.AreEqual(2, deployments.Count());
            Assert.IsNotNull(deployments.First(d => d.Id.Equals("commit1")).Logs);
        }
    }
}
