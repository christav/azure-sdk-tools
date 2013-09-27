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
    public class RestoreAzureWebsiteDeploymentTests : WebsitesTestBase
    {
        [TestMethod]
        public void RestoreAzureWebsiteDeploymentTest()
        {
            // Setup
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

            var clientMock = new Mock<IWebsitesClient>();
            clientMock.Setup(c => c.ListWebSpaces())
                .Returns(new[] {new WebSpace {Name = "webspace1"}, new WebSpace {Name = "webspace2"}});
            clientMock.Setup(c => c.GetWebsite("website1"))
                .Returns(site1);

            SimpleDeploymentServiceManagement deploymentChannel = new SimpleDeploymentServiceManagement();

            var deployments = new List<DeployResult> { new DeployResult { Id = "id1", Current = false }, new DeployResult { Id = "id2", Current = true } };
            deploymentChannel.GetDeploymentsThunk = ar => deployments;
            deploymentChannel.DeployThunk = ar =>
            {
                // Keep track of currently deployed id
                DeployResult newDeployment = deployments.FirstOrDefault(d => d.Id.Equals(ar.Values["commitId"]));
                if (newDeployment != null)
                {
                    // Make all inactive
                    deployments.ForEach(d => d.Complete = false);
                    
                    // Set new to active
                    newDeployment.Complete = true;
                }
            };

            // Test
            RestoreAzureWebsiteDeploymentCommand restoreAzureWebsiteDeploymentCommand =
                new RestoreAzureWebsiteDeploymentCommand(deploymentChannel)
            {
                Name = "website1",
                CommitId = "id2",
                ShareChannel = true,
                WebsitesClient = clientMock.Object,
                CommandRuntime = new MockCommandRuntime(),
                CurrentSubscription = new WindowsAzureSubscription { SubscriptionId = base.subscriptionId }
            };

            restoreAzureWebsiteDeploymentCommand.ExecuteCmdlet();
            Assert.AreEqual(1, ((MockCommandRuntime) restoreAzureWebsiteDeploymentCommand.CommandRuntime).OutputPipeline.Count);
            var responseDeployments = (IEnumerable<DeployResult>)((MockCommandRuntime) restoreAzureWebsiteDeploymentCommand.CommandRuntime).OutputPipeline.FirstOrDefault();
            Assert.IsNotNull(responseDeployments);
            Assert.AreEqual(2, responseDeployments.Count());
            Assert.IsTrue(responseDeployments.Any(d => d.Id.Equals("id2") && d.Complete));
            Assert.IsTrue(responseDeployments.Any(d => d.Id.Equals("id1") && !d.Complete));

            // Change active deployment to id1
            restoreAzureWebsiteDeploymentCommand = new RestoreAzureWebsiteDeploymentCommand(deploymentChannel)
            {
                Name = "website1",
                CommitId = "id1",
                ShareChannel = true,
                WebsitesClient = clientMock.Object,
                CommandRuntime = new MockCommandRuntime(),
                CurrentSubscription = new WindowsAzureSubscription { SubscriptionId = base.subscriptionId }
            };

            restoreAzureWebsiteDeploymentCommand.ExecuteCmdlet();
            Assert.AreEqual(1, ((MockCommandRuntime)restoreAzureWebsiteDeploymentCommand.CommandRuntime).OutputPipeline.Count);
            responseDeployments = (IEnumerable<DeployResult>)((MockCommandRuntime)restoreAzureWebsiteDeploymentCommand.CommandRuntime).OutputPipeline.FirstOrDefault();
            Assert.IsNotNull(responseDeployments);
            Assert.AreEqual(2, responseDeployments.Count());
            Assert.IsTrue(responseDeployments.Any(d => d.Id.Equals("id1") && d.Complete));
            Assert.IsTrue(responseDeployments.Any(d => d.Id.Equals("id2") && !d.Complete));
        }
    }
}
