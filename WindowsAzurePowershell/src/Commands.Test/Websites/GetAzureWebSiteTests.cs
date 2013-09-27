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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Commands.Utilities.Common;
    using Commands.Subscription;
    using Utilities.Common;
    using Utilities.Websites;
    using Commands.Utilities.Properties;
    using Commands.Utilities.Websites;
    using Commands.Utilities.Websites.Services.WebEntities;
    using Commands.Websites;
    using Moq;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class GetAzureWebsiteTests : WebsitesTestBase
    {
        [TestMethod]
        public void ProcessGetWebsiteTest()
        {
            // Setup
            var clientMock = new Mock<IWebsitesClient>();
            clientMock.Setup(c => c.ListWebSpaces())
                .Returns(new[] {new WebSpace {Name = "webspace1"}, new WebSpace {Name = "webspace2"}});

            clientMock.Setup(c => c.ListSitesInWebSpace("webspace1"))
                .Returns(new[] {new Site {Name = "website1", WebSpace = "webspace1"}});

            clientMock.Setup(c => c.ListSitesInWebSpace("webspace2"))
                .Returns(new[] {new Site {Name = "website2", WebSpace = "webspace2"}});

            // Test
            var getAzureWebsiteCommand = new GetAzureWebsiteCommand
            {
                CommandRuntime = new MockCommandRuntime(),
                CurrentSubscription = new WindowsAzureSubscription { SubscriptionId = subscriptionId },
                WebsitesClient = clientMock.Object
            };

            getAzureWebsiteCommand.ExecuteCmdlet();
            Assert.AreEqual(1, ((MockCommandRuntime)getAzureWebsiteCommand.CommandRuntime).OutputPipeline.Count);
            var sites = (IEnumerable<Site>)((MockCommandRuntime)getAzureWebsiteCommand.CommandRuntime).OutputPipeline.FirstOrDefault();
            Assert.IsNotNull(sites);
            Assert.IsTrue(sites.Any(website => (website).Name.Equals("website1") && (website).WebSpace.Equals("webspace1")));
            Assert.IsTrue(sites.Any(website => (website).Name.Equals("website2") && (website).WebSpace.Equals("webspace2")));
        }

        [TestMethod]
        public void GetWebsiteProcessShowTest()
        {
            // Setup
            var clientMock = new Mock<IWebsitesClient>();
            clientMock.Setup(c => c.GetWebsite(It.IsAny<string>()))
                .Returns(new Site
                {
                    Name = "website1",
                    WebSpace = "webspace1"
                });

            clientMock.Setup(c => c.GetWebsiteConfiguration(It.IsAny<string>()))
                .Returns(new SiteConfig {
                    PublishingUsername = "user1"}
                );


            // Test
            var getAzureWebsiteCommand = new GetAzureWebsiteCommand
            {
                CommandRuntime = new MockCommandRuntime(),
                CurrentSubscription = new WindowsAzureSubscription { SubscriptionId = subscriptionId },
                Name = "website1",
                WebsitesClient = clientMock.Object
            };

            getAzureWebsiteCommand.ExecuteCmdlet();
            Assert.AreEqual(1, ((MockCommandRuntime)getAzureWebsiteCommand.CommandRuntime).OutputPipeline.Count);

            var website = ((MockCommandRuntime) getAzureWebsiteCommand.CommandRuntime).OutputPipeline[0] as SiteWithConfig;
            Assert.IsNotNull(website);
            Assert.IsNotNull(website);
            Assert.AreEqual("website1", website.Name);
            Assert.AreEqual("webspace1", website.WebSpace);
            Assert.AreEqual("user1", website.PublishingUsername);

            // Run with mixed casing
            getAzureWebsiteCommand = new GetAzureWebsiteCommand
            {
                CommandRuntime = new MockCommandRuntime(),
                CurrentSubscription = new WindowsAzureSubscription { SubscriptionId = "GetAzureWebSiteTests_GetWebsiteProcessShowTest" },
                Name = "WEBSiTe1",
                WebsitesClient = clientMock.Object
            };

            getAzureWebsiteCommand.ExecuteCmdlet();
            Assert.AreEqual(1, ((MockCommandRuntime)getAzureWebsiteCommand.CommandRuntime).OutputPipeline.Count);

            website = ((MockCommandRuntime)getAzureWebsiteCommand.CommandRuntime).OutputPipeline[0] as SiteWithConfig;
            Assert.IsNotNull(website);
            Assert.IsNotNull(website);
            Assert.AreEqual("website1", website.Name);
            Assert.AreEqual("webspace1", website.WebSpace);
            Assert.AreEqual("user1", website.PublishingUsername);
        }

        [TestMethod]
        public void ProcessGetWebsiteWithNullSubscription()
        {
            // Test
            var getAzureWebsiteCommand = new GetAzureWebsiteCommand
            {
                CommandRuntime = new MockCommandRuntime(),
                CurrentSubscription = null,
                Profile = new WindowsAzureProfile(new Mock<IProfileStore>().Object)
            };

            Testing.AssertThrows<Exception>(getAzureWebsiteCommand.ExecuteCmdlet, Resources.NoDefaultSubscriptionMessage);
        }

        [TestMethod]
        public void TestGetAzureWebsiteWithDiagnosticsSettings()
        {
            // Setup
            var websitesClientMock = new Mock<IWebsitesClient>();
            websitesClientMock.Setup(c => c.GetWebsite(It.IsAny<string>()))
                .Returns(new Site
                {
                    Name = "website1", WebSpace = "webspace1", State = "Running"
                });

            websitesClientMock.Setup(c => c.GetWebsiteConfiguration(It.IsAny<string>()))
                .Returns(new SiteConfig {PublishingUsername = "user1"});

            var getAzureWebsiteCommand = new GetAzureWebsiteCommand
            {
                CommandRuntime = new MockCommandRuntime(),
                CurrentSubscription = new WindowsAzureSubscription { SubscriptionId = subscriptionId },
                Name = "website1",
                WebsitesClient = websitesClientMock.Object
            };

            // Test
            getAzureWebsiteCommand.ExecuteCmdlet();

            // Assert
            Assert.AreEqual(1, ((MockCommandRuntime)getAzureWebsiteCommand.CommandRuntime).OutputPipeline.Count);
            websitesClientMock.Verify(f => f.GetApplicationDiagnosticsSettings("website1"), Times.Once());
        }
    }
}
