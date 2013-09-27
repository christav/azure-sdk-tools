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
    using System.Management.Automation;
    using Commands.Utilities.Common;
    using Utilities.Common;
    using Utilities.Websites;
    using Commands.Utilities.Websites;
    using Commands.Utilities.Websites.Services.DeploymentEntities;
    using Commands.Utilities.Websites.Services.WebEntities;
    using Commands.Websites;
    using Moq;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SetAzureWebsiteTests : WebsitesTestBase
    {
        [TestMethod]
        public void SetAzureWebsiteProcess()
        {
            const string websiteName = "website1";
            const string webspaceName = "webspace";
            const string suffix = "azurewebsites.com";

            // Setup
            Mock<IWebsitesClient> clientMock = new Mock<IWebsitesClient>();
            clientMock.Setup(f => f.GetWebsiteDnsSuffix()).Returns(suffix);

            bool updatedSite = false;
            bool updatedSiteConfig = false;

            clientMock.Setup(c => c.GetWebsite(websiteName))
                .Returns(new Site {Name = websiteName, WebSpace = webspaceName});
            clientMock.Setup(c => c.GetWebsiteConfiguration(websiteName))
                .Returns(new SiteConfig {NumberOfWorkers = 1});
            clientMock.Setup(c => c.UpdateWebsiteConfiguration(websiteName, It.IsAny<SiteConfig>()))
                .Callback((string name, SiteConfig config) =>
                    {
                        Assert.IsNotNull(config);
                        Assert.AreEqual(config.NumberOfWorkers, 3);
                        updatedSiteConfig = true;
                    }).Verifiable();

            clientMock.Setup(c => c.UpdateWebsiteHostNames(It.IsAny<Site>(), It.IsAny<IEnumerable<string>>()))
                .Callback((Site site, IEnumerable<string> names) =>
                    {
                        Assert.AreEqual(websiteName, site.Name);
                        Assert.IsTrue(names.Any(hostname => hostname.Equals(string.Format("{0}.{1}", websiteName, suffix))));
                        Assert.IsTrue(names.Any(hostname => hostname.Equals("stuff.com")));
                        updatedSite = true;
                    });

            // Test
            SetAzureWebsiteCommand setAzureWebsiteCommand = new SetAzureWebsiteCommand
            {
                ShareChannel = true,
                CommandRuntime = new MockCommandRuntime(),
                Name = websiteName,
                CurrentSubscription = new WindowsAzureSubscription { SubscriptionId = base.subscriptionId },
                NumberOfWorkers = 3,
                WebsitesClient = clientMock.Object
            };

            setAzureWebsiteCommand.ExecuteCmdlet();
            Assert.IsTrue(updatedSiteConfig);
            Assert.IsFalse(updatedSite);

            // Test updating site only and not configurations
            updatedSite = false;
            updatedSiteConfig = false;
            setAzureWebsiteCommand = new SetAzureWebsiteCommand
            {
                ShareChannel = true,
                CommandRuntime = new MockCommandRuntime(),
                Name = websiteName,
                CurrentSubscription = new WindowsAzureSubscription { SubscriptionId = base.subscriptionId },
                HostNames = new [] { "stuff.com" },
                WebsitesClient = clientMock.Object
            };

            setAzureWebsiteCommand.ExecuteCmdlet();
            Assert.IsFalse(updatedSiteConfig);
            Assert.IsTrue(updatedSite);
        }
    }
}
