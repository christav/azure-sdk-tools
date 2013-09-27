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
    using Commands.Utilities.Common;
    using Commands.Utilities.Websites;
    using Moq;
    using Utilities.Common;
    using Utilities.Websites;
    using Commands.Utilities.Websites.Services.WebEntities;
    using Commands.Websites;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ShowAzureWebsiteTests : WebsitesTestBase
    {
        [TestMethod]
        public void ProcessShowWebsiteTest()
        {
            // Setup
            var mockClient = new Mock<IWebsitesClient>();
            mockClient.Setup(c => c.GetWebsite("website1"))
                .Returns(new Site
                {
                    Name = "website1",
                    WebSpace = "webspace1",
                    HostNames = new[] {"website1.cloudapp.com"}
                });

            // Test
            ShowAzureWebsiteCommand showAzureWebsiteCommand = new ShowAzureWebsiteCommand
            {
                ShareChannel = true,
                CommandRuntime = new MockCommandRuntime(),
                Name = "website1",
                CurrentSubscription = new WindowsAzureSubscription { SubscriptionId = base.subscriptionId },
                WebsitesClient = mockClient.Object
            };

            // Show existing website
            showAzureWebsiteCommand.ExecuteCmdlet();
        }
    }
}