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
    using System.Management.Automation;
    using Commands.Utilities.Common;
    using Commands.ServiceBus;
    using Utilities.Common;
    using Utilities.ServiceBus;
    using Microsoft.WindowsAzure.Commands.Utilities.Properties;
    using Commands.Utilities.ServiceBus.ResourceModel;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class GetAzureSBNamespaceTests : TestBase
    {
        SimpleServiceBusManagement channel;
        MockCommandRuntime mockCommandRuntime;
        GetAzureSBNamespaceCommand cmdlet;

        [TestInitialize]
        public void SetupTest()
        {
            new FileSystemHelper(this).CreateAzureSdkDirectoryAndImportPublishSettings();
            channel = new SimpleServiceBusManagement();
            mockCommandRuntime = new MockCommandRuntime();
            cmdlet = new GetAzureSBNamespaceCommand(channel) { CommandRuntime = mockCommandRuntime };
        }

        [TestMethod]
        public void GetAzureSBNamespaceSuccessfull()
        {
            // Setup
            string name = "test";
            cmdlet.Name = name;
            ServiceBusNamespace expected = new ServiceBusNamespace { Name = name };
            channel.GetNamespaceThunk = gn => { return expected; };

            // Test
            cmdlet.ExecuteCmdlet();

            // Assert
            ServiceBusNamespace actual = mockCommandRuntime.OutputPipeline[0] as ServiceBusNamespace;
            Assert.AreEqual<string>(expected.Name, actual.Name);
        }

        [TestMethod]
        public void GetAzureSBNamespaceWithNotExistingNameFail()
        {
            // Setup
            string expected = Resources.ServiceBusNamespaceMissingMessage;
            cmdlet.Name = "notExistingName";
            channel.GetNamespaceThunk = gn => {  throw new Exception(Resources.InternalServerErrorMessage); };

            // Test
            cmdlet.ExecuteCmdlet();

            // Assert
            ErrorRecord error = mockCommandRuntime.ErrorStream[0];
            Assert.AreEqual<string>(expected, error.Exception.Message);
        }

        [TestMethod]
        public void ListNamespacesSuccessfull()
        {
            // Setup
            string name1 = "test1";
            string name2 = "test2";
            List<ServiceBusNamespace> expected = new List<ServiceBusNamespace>();
            expected.Add(new ServiceBusNamespace { Name = name1 });
            expected.Add(new ServiceBusNamespace { Name = name2 });
            channel.ListNamespacesThunk = gn => { return expected; };

            // Test
            cmdlet.ExecuteCmdlet();

            // Assert
            List<ServiceBusNamespace> actual = mockCommandRuntime.OutputPipeline[0] as List<ServiceBusNamespace>;
            Assert.AreEqual<int>(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual<string>(expected[i].Name, actual[i].Name);
            }
        }

        [TestMethod]
        public void GetAzureSBNamespaceWithInvalidNamesFail()
        {
            // Setup
            string[] invalidNames = { "1test", "test#", "test invaid", "-test", "_test" };

            foreach (string invalidName in invalidNames)
            {
                MockCommandRuntime mockCommandRuntime = new MockCommandRuntime();
                GetAzureSBNamespaceCommand cmdlet = new GetAzureSBNamespaceCommand() { Name = invalidName, CommandRuntime = mockCommandRuntime };
                string expected = string.Format("{0}\r\nParameter name: Name", string.Format(Resources.InvalidNamespaceName, invalidName));

                Testing.AssertThrows<ArgumentException>(() => cmdlet.ExecuteCmdlet(), expected);
            }
        }
    }
}