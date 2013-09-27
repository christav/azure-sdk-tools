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
    using System.Management.Automation;
    using Commands.Utilities.Common;
    using VisualStudio.TestTools.UnitTesting;
    using Utilities.Websites;
    using Commands.Utilities.Websites;
    using Commands.Websites;
    using Moq;

    [TestClass]
    public class DisableAzureWebsiteApplicationDiagnosticTests : WebsitesTestBase
    {
        private const string websiteName = "website1";

        private Mock<IWebsitesClient> websitesClientMock = new Mock<IWebsitesClient>();

        private DisableAzureWebsiteApplicationDiagnosticCommand disableAzureWebsiteApplicationDiagnosticCommand;

        private Mock<ICommandRuntime> commandRuntimeMock;

        [TestInitialize]
        public override void SetupTest()
        {
            websitesClientMock = new Mock<IWebsitesClient>();
            commandRuntimeMock = new Mock<ICommandRuntime>();
        }

        [TestMethod]
        public void DisableAzureWebsiteApplicationDiagnosticApplication()
        {
            // Setup
            websitesClientMock.Setup(f => f.DisableApplicationDiagnostic(
                websiteName,
                WebsiteDiagnosticOutput.FileSystem));

            disableAzureWebsiteApplicationDiagnosticCommand = new DisableAzureWebsiteApplicationDiagnosticCommand()
            {
                ShareChannel = true,
                CommandRuntime = commandRuntimeMock.Object,
                Name = websiteName,
                CurrentSubscription = new WindowsAzureSubscription { SubscriptionId = base.subscriptionId },
                WebsitesClient = websitesClientMock.Object,
                File = true,
            };

            // Test
            disableAzureWebsiteApplicationDiagnosticCommand.ExecuteCmdlet();

            // Assert
            websitesClientMock.Verify(f => f.DisableApplicationDiagnostic(
                websiteName,
                WebsiteDiagnosticOutput.FileSystem), Times.Once());

            commandRuntimeMock.Verify(f => f.WriteObject(true), Times.Never());
        }

        [TestMethod]
        public void DisableAzureWebsiteApplicationDiagnosticApplicationTableLog()
        {
            // Setup
            websitesClientMock.Setup(f => f.DisableApplicationDiagnostic(
                websiteName,
                WebsiteDiagnosticOutput.StorageTable));

            disableAzureWebsiteApplicationDiagnosticCommand = new DisableAzureWebsiteApplicationDiagnosticCommand()
            {
                ShareChannel = true,
                CommandRuntime = commandRuntimeMock.Object,
                Name = websiteName,
                CurrentSubscription = new WindowsAzureSubscription { SubscriptionId = base.subscriptionId },
                WebsitesClient = websitesClientMock.Object,
                Storage = true
            };

            // Test
            disableAzureWebsiteApplicationDiagnosticCommand.ExecuteCmdlet();

            // Assert
            websitesClientMock.Verify(f => f.DisableApplicationDiagnostic(
                websiteName,
                WebsiteDiagnosticOutput.StorageTable), Times.Once());

            commandRuntimeMock.Verify(f => f.WriteObject(true), Times.Never());
        }
    }
}
