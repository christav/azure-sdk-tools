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
    using Commands.Utilities.Common;
    using Utilities.Common;
    using Utilities.Websites;
    using Commands.Utilities.Websites;
    using Commands.Utilities.Websites.Services.WebEntities;
    using Commands.Websites;
    using Moq;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class NewAzureWebsiteTests : WebsitesTestBase
    {
        [TestMethod]
        public void ProcessNewWebsiteTest()
        {
            const string websiteName = "website1";
            const string webspaceName = "webspace1";
            const string suffix = "azurewebsites.com";

            // Setup
            Mock<IWebsitesClient> clientMock = new Mock<IWebsitesClient>();
            clientMock.Setup(c => c.GetWebsiteDnsSuffix()).Returns(suffix);
            clientMock.Setup(c => c.ListWebSpaces())
                .Returns(new[]
                {
                    new WebSpace {Name = "webspace1", GeoRegion = "webspace1"},
                    new WebSpace {Name = "webspace2", GeoRegion = "webspace2"}
                });

            clientMock.Setup(c => c.GetWebsiteConfiguration("website1"))
                .Returns(new SiteConfig {PublishingUsername = "user1"});

            string createdSiteName = null;
            string createdWebspaceName = null;

            clientMock.Setup(c => c.CreateWebsite(webspaceName, It.IsAny<SiteWithWebSpace>()))
                .Returns((string space, SiteWithWebSpace site) => site)
                .Callback((string space, SiteWithWebSpace site) =>
                    {
                        createdSiteName = site.Name;
                        createdWebspaceName = space;
                    });

            // Test
            MockCommandRuntime mockRuntime = new MockCommandRuntime();
            NewAzureWebsiteCommand newAzureWebsiteCommand = new NewAzureWebsiteCommand
            {
                ShareChannel = true,
                CommandRuntime = mockRuntime,
                Name = websiteName,
                Location = webspaceName,
                CurrentSubscription = new WindowsAzureSubscription { SubscriptionId = base.subscriptionId },
                WebsitesClient = clientMock.Object
            };

            newAzureWebsiteCommand.ExecuteCmdlet();
            Assert.AreEqual(websiteName, createdSiteName);
            Assert.AreEqual(webspaceName, createdWebspaceName);
            Assert.AreEqual<string>(websiteName, (mockRuntime.OutputPipeline[0] as SiteWithConfig).Name);
        }

        [TestMethod]
        public void GetsWebsiteDefaultLocation()
        {
            const string websiteName = "website1";
            const string suffix = "azurewebsites.com";
            const string location = "West US";

            bool created = false;

            // Setup
            Mock<IWebsitesClient> clientMock = new Mock<IWebsitesClient>();
            clientMock.Setup(c => c.GetWebsiteDnsSuffix()).Returns(suffix);
            clientMock.Setup(c => c.GetDefaultLocation()).Returns(location);
            
            clientMock.Setup(c => c.ListWebSpaces()).Returns(new WebSpaces());
            clientMock.Setup(c => c.GetWebsiteConfiguration(websiteName))
                .Returns(new SiteConfig
                {
                    PublishingUsername = "user1"
                });

            clientMock.Setup(c => c.CreateWebsite(It.IsAny<string>(), It.IsAny<SiteWithWebSpace>()))
                .Returns((string space, SiteWithWebSpace site) => site)
                .Callback((string space, SiteWithWebSpace site) =>
                    {
                        created = true;
                    });

            // Test
            MockCommandRuntime mockRuntime = new MockCommandRuntime();
            NewAzureWebsiteCommand newAzureWebsiteCommand = new NewAzureWebsiteCommand()
            {
                ShareChannel = true,
                CommandRuntime = mockRuntime,
                Name = websiteName,
                CurrentSubscription = new WindowsAzureSubscription { SubscriptionId = base.subscriptionId },
                WebsitesClient = clientMock.Object
            };

            newAzureWebsiteCommand.ExecuteCmdlet();
            Assert.IsTrue(created);
            Assert.AreEqual<string>(websiteName, (mockRuntime.OutputPipeline[0] as SiteWithConfig).Name);
            clientMock.Verify(f => f.GetDefaultLocation(), Times.Once());
        }
    }
}
