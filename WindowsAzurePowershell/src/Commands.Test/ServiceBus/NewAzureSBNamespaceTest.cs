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

namespace Microsoft.WindowsAzure.Commands.Test.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using Commands.Utilities.Common;
    using Commands.ServiceBus;
    using Utilities.Common;
    using Utilities.ServiceBus;
    using Microsoft.WindowsAzure.Commands.Utilities.Properties;
    using Commands.Utilities.ServiceBus.ResourceModel;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class NewAzureSBNamespaceTests : TestBase
    {
        [TestInitialize]
        public void SetupTest()
        {
            new FileSystemHelper(this).CreateAzureSdkDirectoryAndImportPublishSettings();
        }

        [TestMethod]
        public void NewAzureSBNamespaceSuccessfull()
        {
            // Setup
            SimpleServiceBusManagement channel = new SimpleServiceBusManagement();
            MockCommandRuntime mockCommandRuntime = new MockCommandRuntime();
            string name = "test";
            string location = "West US";
            NewAzureSBNamespaceCommand cmdlet = new NewAzureSBNamespaceCommand(channel) { Name = name, Location = location, CommandRuntime = mockCommandRuntime };
            ServiceBusNamespace expected = new ServiceBusNamespace { Name = name, Region = location };
            channel.CreateServiceBusNamespaceThunk = csbn => { return expected; };
            channel.ListServiceBusRegionsThunk = lsbr => 
            {
                List<ServiceBusRegion> list = new List<ServiceBusRegion>();
                list.Add(new ServiceBusRegion { Code = location });
                return list;
            };

            // Test
            cmdlet.ExecuteCmdlet();

            // Assert
            ServiceBusNamespace actual = mockCommandRuntime.OutputPipeline[0] as ServiceBusNamespace;
            Assert.AreEqual<ServiceBusNamespace>(expected, actual);
        }

        [TestMethod]
        public void NewAzureSBNamespaceGetsDefaultLocation()
        {
            // Setup
            SimpleServiceBusManagement channel = new SimpleServiceBusManagement();
            MockCommandRuntime mockCommandRuntime = new MockCommandRuntime();
            string name = "test";
            string location = "West US";
            NewAzureSBNamespaceCommand cmdlet = new NewAzureSBNamespaceCommand(channel) { Name = name, CommandRuntime = mockCommandRuntime };
            ServiceBusNamespace expected = new ServiceBusNamespace { Name = name, Region = location };
            channel.CreateServiceBusNamespaceThunk = csbn => { return expected; };
            channel.ListServiceBusRegionsThunk = lsbr =>
            {
                List<ServiceBusRegion> list = new List<ServiceBusRegion>();
                list.Add(new ServiceBusRegion { Code = location });
                return list;
            };

            // Test
            cmdlet.ExecuteCmdlet();

            // Assert
            ServiceBusNamespace actual = mockCommandRuntime.OutputPipeline[0] as ServiceBusNamespace;
            Assert.AreEqual<ServiceBusNamespace>(expected, actual);
        }

        [TestMethod]
        public void NewAzureSBNamespaceWithInvalidNamesFail()
        {
            // Setup
            string[] invalidNames = { "1test", "test#", "test invaid", "-test", "_test" };

            foreach (string invalidName in invalidNames)
            {
                MockCommandRuntime mockCommandRuntime = new MockCommandRuntime();
                NewAzureSBNamespaceCommand cmdlet = new NewAzureSBNamespaceCommand() { Name = invalidName, Location = "West US", CommandRuntime = mockCommandRuntime };
                string expected = string.Format("{0}\r\nParameter name: Name", string.Format(Resources.InvalidNamespaceName, invalidName));

                Testing.AssertThrows<ArgumentException>(() => cmdlet.ExecuteCmdlet(), expected);
            }
        }

        [TestMethod]
        public void NewAzureSBNamespaceWithInternalServerError()
        {
            // Setup
            SimpleServiceBusManagement channel = new SimpleServiceBusManagement();
            MockCommandRuntime mockCommandRuntime = new MockCommandRuntime();
            string name = "test";
            string location = "West US";
            NewAzureSBNamespaceCommand cmdlet = new NewAzureSBNamespaceCommand(channel) { Name = name, Location = location, CommandRuntime = mockCommandRuntime };
            channel.CreateServiceBusNamespaceThunk = csbns => { throw new Exception(Resources.InternalServerErrorMessage); };
            channel.ListServiceBusRegionsThunk = lsbr =>
            {
                List<ServiceBusRegion> list = new List<ServiceBusRegion>();
                list.Add(new ServiceBusRegion { Code = location });
                return list;
            };
            string expected = Resources.NewNamespaceErrorMessage;

            Testing.AssertThrows<Exception>(() => cmdlet.ExecuteCmdlet(), expected);
        }

        [TestMethod]
        public void NewAzureSBNamespaceWithInvalidLocation()
        {
            // Setup
            SimpleServiceBusManagement channel = new SimpleServiceBusManagement();
            MockCommandRuntime mockCommandRuntime = new MockCommandRuntime();
            string name = "test";
            string location = "Invalid location";
            NewAzureSBNamespaceCommand cmdlet = new NewAzureSBNamespaceCommand(channel) { Name = name, Location = location, CommandRuntime = mockCommandRuntime };
            channel.ListServiceBusRegionsThunk = lsbr =>
            {
                List<ServiceBusRegion> list = new List<ServiceBusRegion>();
                list.Add(new ServiceBusRegion { Code = "West US" });
                return list;
            };
            string expected = string.Format("{0}\r\nParameter name: Location", string.Format(Resources.InvalidServiceBusLocation, location));

            Testing.AssertThrows<ArgumentException>(() => cmdlet.ExecuteCmdlet(), expected);
        }

        [TestMethod]
        public void CreatesNewSBCaseInsensitiveRegion()
        {
            // Setup
            SimpleServiceBusManagement channel = new SimpleServiceBusManagement();
            MockCommandRuntime mockCommandRuntime = new MockCommandRuntime();
            string name = "test";
            string location = "West US";
            NewAzureSBNamespaceCommand cmdlet = new NewAzureSBNamespaceCommand(channel)
            {
                Name = name,
                Location = "west Us",
                CommandRuntime = mockCommandRuntime
            };
            ServiceBusNamespace expected = new ServiceBusNamespace { Name = name, Region = location };
            channel.CreateServiceBusNamespaceThunk = csbn => { return expected; };
            channel.ListServiceBusRegionsThunk = lsbr =>
            {
                List<ServiceBusRegion> list = new List<ServiceBusRegion>();
                list.Add(new ServiceBusRegion { Code = location });
                return list;
            };

            // Test
            cmdlet.ExecuteCmdlet();

            // Assert
            ServiceBusNamespace actual = mockCommandRuntime.OutputPipeline[0] as ServiceBusNamespace;
            Assert.AreEqual<ServiceBusNamespace>(expected, actual);
        }
    }
}