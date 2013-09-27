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
    using Moq;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AddAzureEnvironmentTests : TestBase
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
        public void AddsAzureEnvironment()
        {
            Mock<ICommandRuntime> commandRuntimeMock = new Mock<ICommandRuntime>();
            AddAzureEnvironmentCommand cmdlet = new AddAzureEnvironmentCommand()
            {
                CommandRuntime = commandRuntimeMock.Object,
                Name = "Katal",
                PublishSettingsFileUrl = "http://microsoft.com",
                ServiceEndpoint = "endpoint.net",
                ManagementPortalUrl = "management portal url",
                StorageEndpoint = "endpoint.net"
            };

            cmdlet.ExecuteCmdlet();

            commandRuntimeMock.Verify(f => f.WriteObject(It.IsAny<WindowsAzureEnvironment>()), Times.Once());
            WindowsAzureEnvironment env = WindowsAzureProfile.Instance.Environments["KaTaL"];
            Assert.AreEqual(env.Name, cmdlet.Name);
            Assert.AreEqual(env.PublishSettingsFileUrl, cmdlet.PublishSettingsFileUrl);
            Assert.AreEqual(env.ServiceEndpoint, cmdlet.ServiceEndpoint);
            Assert.AreEqual(env.ManagementPortalUrl, cmdlet.ManagementPortalUrl);
            Assert.AreEqual(env.StorageBlobEndpointFormat, "{0}://{1}.blob.endpoint.net/");
            Assert.AreEqual(env.StorageQueueEndpointFormat, "{0}://{1}.queue.endpoint.net/");
            Assert.AreEqual(env.StorageTableEndpointFormat, "{0}://{1}.table.endpoint.net/");
        }

        [TestMethod]
        public void AddsEnvironmentWithMinimumInformation()
        {
            Mock<ICommandRuntime> commandRuntimeMock = new Mock<ICommandRuntime>();
            AddAzureEnvironmentCommand cmdlet = new AddAzureEnvironmentCommand()
            {
                CommandRuntime = commandRuntimeMock.Object,
                Name = "Katal",
                PublishSettingsFileUrl = "http://microsoft.com"
            };

            cmdlet.ExecuteCmdlet();

            commandRuntimeMock.Verify(f => f.WriteObject(It.IsAny<WindowsAzureEnvironment>()), Times.Once());
            WindowsAzureEnvironment env = WindowsAzureProfile.Instance.Environments["KaTaL"];
            Assert.AreEqual(env.Name, cmdlet.Name);
            Assert.AreEqual(env.PublishSettingsFileUrl, cmdlet.PublishSettingsFileUrl);
        }

        [TestMethod]
        public void IgnoresAddingDuplicatedEnvironment()
        {
            Mock<ICommandRuntime> commandRuntimeMock = new Mock<ICommandRuntime>();
            AddAzureEnvironmentCommand cmdlet = new AddAzureEnvironmentCommand()
            {
                CommandRuntime = commandRuntimeMock.Object,
                Name = "Katal",
                PublishSettingsFileUrl = "http://microsoft.com",
                ServiceEndpoint = "endpoint.net",
                ManagementPortalUrl = "management portal url",
                StorageEndpoint = "endpoint.net"
            };
            cmdlet.ExecuteCmdlet();
            int count = WindowsAzureProfile.Instance.Environments.Count;

            // Add again
            cmdlet.Name = "kAtAl";
            Testing.AssertThrows<Exception>(() => cmdlet.ExecuteCmdlet());
        }

        [TestMethod]
        public void IgnoresAddingPublicEnvironment()
        {
            Mock<ICommandRuntime> commandRuntimeMock = new Mock<ICommandRuntime>();
            AddAzureEnvironmentCommand cmdlet = new AddAzureEnvironmentCommand()
            {
                CommandRuntime = commandRuntimeMock.Object,
                Name = EnvironmentName.AzureCloud,
                PublishSettingsFileUrl = "http://microsoft.com"
            };

            Testing.AssertThrows<Exception>(() => cmdlet.ExecuteCmdlet());
        }

        [TestMethod]
        public void AddsEnvironmentWithStorageEndpoint()
        {
            Mock<ICommandRuntime> commandRuntimeMock = new Mock<ICommandRuntime>();
            WindowsAzureEnvironment actual = null;
            commandRuntimeMock.Setup(f => f.WriteObject(It.IsAny<object>()))
                .Callback((object output) => actual = (WindowsAzureEnvironment)output);
            AddAzureEnvironmentCommand cmdlet = new AddAzureEnvironmentCommand()
            {
                CommandRuntime = commandRuntimeMock.Object,
                Name = "Katal",
                PublishSettingsFileUrl = "http://microsoft.com",
                StorageEndpoint = "core.windows.net"
            };

            cmdlet.ExecuteCmdlet();

            commandRuntimeMock.Verify(f => f.WriteObject(It.IsAny<WindowsAzureEnvironment>()), Times.Once());
            WindowsAzureEnvironment env = WindowsAzureProfile.Instance.Environments["KaTaL"];
            Assert.AreEqual(env.Name, cmdlet.Name);
            Assert.AreEqual(env.PublishSettingsFileUrl, actual.PublishSettingsFileUrl);
            Assert.AreEqual(
                WindowsAzureEnvironmentConstants.AzureStorageBlobEndpointFormat,
                actual.StorageBlobEndpointFormat);
            Assert.AreEqual(
                WindowsAzureEnvironmentConstants.AzureStorageQueueEndpointFormat,
                actual.StorageQueueEndpointFormat);
            Assert.AreEqual(
                WindowsAzureEnvironmentConstants.AzureStorageTableEndpointFormat,
                actual.StorageTableEndpointFormat);
        }

        [TestMethod]
        public void AddsEnvironmentWithEmptyStorageEndpoint()
        {
            Mock<ICommandRuntime> commandRuntimeMock = new Mock<ICommandRuntime>();
            WindowsAzureEnvironment actual = null;
            commandRuntimeMock.Setup(f => f.WriteObject(It.IsAny<object>()))
                .Callback((object output) => actual = (WindowsAzureEnvironment)output);
            AddAzureEnvironmentCommand cmdlet = new AddAzureEnvironmentCommand()
            {
                CommandRuntime = commandRuntimeMock.Object,
                Name = "Katal",
                PublishSettingsFileUrl = "http://microsoft.com",
                StorageEndpoint = null
            };

            cmdlet.ExecuteCmdlet();

            commandRuntimeMock.Verify(f => f.WriteObject(It.IsAny<WindowsAzureEnvironment>()), Times.Once());
            WindowsAzureEnvironment env = WindowsAzureProfile.Instance.Environments["KaTaL"];
            Assert.AreEqual(env.Name, cmdlet.Name);
            Assert.AreEqual(env.PublishSettingsFileUrl, actual.PublishSettingsFileUrl);
            Assert.IsTrue(string.IsNullOrEmpty(actual.StorageBlobEndpointFormat));
            Assert.IsTrue(string.IsNullOrEmpty(actual.StorageQueueEndpointFormat));
            Assert.IsTrue(string.IsNullOrEmpty(actual.StorageTableEndpointFormat));
        }
    }
}