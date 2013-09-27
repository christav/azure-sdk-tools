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

namespace Microsoft.WindowsAzure.Commands.Test.Environment
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Management.Automation;
    using Commands.Utilities.Common;
    using Commands.Subscription;
    using Utilities.Common;
    using Microsoft.WindowsAzure.Commands.Utilities.Properties;
    using Moq;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SetAzureEnvironmentTests : TestBase
    {
        private WindowsAzureProfile testProfile;

        [TestInitialize]
        public void SetupTest()
        {
            testProfile = new WindowsAzureProfile(new Mock<IProfileStore>().Object);
            WindowsAzureProfile.Instance = testProfile;
        }

        [TestCleanup]
        public void Cleanup()
        {
            WindowsAzureProfile.ResetInstance();
        }

        [TestMethod]
        public void SetsAzureEnvironment()
        {
            Mock<ICommandRuntime> commandRuntimeMock = new Mock<ICommandRuntime>();
            string name = "Katal";
            testProfile.AddEnvironment(new WindowsAzureEnvironment { Name = name, PublishSettingsFileUrl = "publish file url"});

            SetAzureEnvironmentCommand cmdlet = new SetAzureEnvironmentCommand()
            {
                CommandRuntime = commandRuntimeMock.Object,
                Name = "KATaL",
                PublishSettingsFileUrl = "http://microsoft.com",
                ServiceEndpoint = "endpoint.net",
                ManagementPortalUrl = "management portal url",
                StorageEndpoint = "endpoint.net"
            };

            cmdlet.ExecuteCmdlet();

            commandRuntimeMock.Verify(f => f.WriteObject(It.IsAny<WindowsAzureEnvironment>()), Times.Once());
            WindowsAzureEnvironment env = WindowsAzureProfile.Instance.Environments["KaTaL"];
            Assert.AreEqual(env.Name.ToLower(), cmdlet.Name.ToLower());
            Assert.AreEqual(env.PublishSettingsFileUrl, cmdlet.PublishSettingsFileUrl);
            Assert.AreEqual(env.ServiceEndpoint, cmdlet.ServiceEndpoint);
            Assert.AreEqual(env.ManagementPortalUrl, cmdlet.ManagementPortalUrl);
            Assert.AreEqual(env.StorageBlobEndpointFormat, "{0}://{1}.blob.endpoint.net/");
            Assert.AreEqual(env.StorageQueueEndpointFormat, "{0}://{1}.queue.endpoint.net/");
            Assert.AreEqual(env.StorageTableEndpointFormat, "{0}://{1}.table.endpoint.net/");
        }

        [TestMethod]
        public void FailsForNonExistingEnvironments()
        {
            Mock<ICommandRuntime> commandRuntimeMock = new Mock<ICommandRuntime>();
            SetAzureEnvironmentCommand cmdlet = new SetAzureEnvironmentCommand()
            {
                CommandRuntime = commandRuntimeMock.Object,
                Name = "Katal",
                PublishSettingsFileUrl = "http://microsoft.com",
                ServiceEndpoint = "endpoint.net",
                ManagementPortalUrl = "management portal url",
                StorageEndpoint = "endpoint.net"
            };

            Testing.AssertThrows<KeyNotFoundException>(
                () => cmdlet.ExecuteCmdlet(),
                string.Format(Resources.EnvironmentNotFound, "Katal"));
        }

        [TestMethod]
        public void ThrowsWhenSettingPublicEnvironment()
        {
            Mock<ICommandRuntime> commandRuntimeMock = new Mock<ICommandRuntime>();

            foreach (string name in WindowsAzureEnvironment.PublicEnvironments.Keys)
            {
                SetAzureEnvironmentCommand cmdlet = new SetAzureEnvironmentCommand()
                {
                    CommandRuntime = commandRuntimeMock.Object,
                    Name = name,
                    PublishSettingsFileUrl = "http://microsoft.com"
                };

                Testing.AssertThrows<InvalidOperationException>(
                    () => cmdlet.ExecuteCmdlet(),
                    string.Format(Resources.ChangePublicEnvironmentMessage, name));
            }
        }
    }
}