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

namespace Microsoft.WindowsAzure.Commands.Test.Store
{
    using System.Management.Automation;
    using Commands.Utilities.Common;
    using Commands.Store;
    using Utilities.Common;
    using Microsoft.WindowsAzure.Commands.Utilities.Properties;
    using Commands.Utilities.Store;
    using Commands.Utilities.Store.ResourceModel;
    using Moq;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SetAzureStoreAddOnTests : TestBase
    {
        Mock<ICommandRuntime> mockCommandRuntime;

        Mock<StoreClient> mockStoreClient;

        Mock<PowerShellCustomConfirmation> mockConfirmation;

        SetAzureStoreAddOnCommand cmdlet;

        [TestInitialize]
        public void SetupTest()
        {
            new FileSystemHelper(this).CreateAzureSdkDirectoryAndImportPublishSettings();
            mockCommandRuntime = new Mock<ICommandRuntime>();
            mockStoreClient = new Mock<StoreClient>();
            mockConfirmation = new Mock<PowerShellCustomConfirmation>();
            cmdlet = new SetAzureStoreAddOnCommand()
            {
                StoreClient = mockStoreClient.Object,
                CommandRuntime = mockCommandRuntime.Object,
                CustomConfirmation = mockConfirmation.Object
            };
        }

        [TestMethod]
        public void SetAzureStoreAddOnWithSuccessful()
        {
            // Setup
            bool expected = true;
            string name = "TestAddOn";
            string addonId = "Search";
            string plan = "free";
            string message = "Expected message for set";
            cmdlet.Name = name;
            cmdlet.Plan = plan;
            WindowsAzureAddOn addon = new WindowsAzureAddOn(new Resource() { Type = addonId }, "West US", null);
            mockConfirmation.Setup(f => f.ShouldProcess(Resources.SetAddOnConformation, message)).Returns(true);
            mockStoreClient.Setup(f => f.TryGetAddOn(name, out addon)).Returns(true);
            mockStoreClient.Setup(f => f.UpdateAddOn(name, plan, null));
            mockStoreClient.Setup(f => f.GetConfirmationMessage(OperationType.Set, addonId, plan)).Returns(message);

            // Test
            cmdlet.ExecuteCmdlet();

            // Assert
            mockStoreClient.Verify(f => f.UpdateAddOn(name, plan, null), Times.Once());
            mockConfirmation.Verify(f => f.ShouldProcess(Resources.SetAddOnConformation, message), Times.Once());
            mockCommandRuntime.Verify(f => f.WriteObject(expected), Times.Never());
        }

        [TestMethod]
        public void SetAzureStoreAddOnWithPassThru()
        {
            // Setup
            bool expected = true;
            string name = "TestAddOn";
            string addonId = "Search";
            string plan = "free";
            string message = "Expected message for set";
            cmdlet.Name = name;
            cmdlet.Plan = plan;
            cmdlet.PassThru = true;
            WindowsAzureAddOn addon = new WindowsAzureAddOn(new Resource() { Type = addonId }, "West US", null);
            mockConfirmation.Setup(f => f.ShouldProcess(Resources.SetAddOnConformation, message)).Returns(true);
            mockStoreClient.Setup(f => f.TryGetAddOn(name, out addon)).Returns(true);
            mockStoreClient.Setup(f => f.UpdateAddOn(name, plan, null));
            mockStoreClient.Setup(f => f.GetConfirmationMessage(OperationType.Set, addonId, plan)).Returns(message);

            // Test
            cmdlet.ExecuteCmdlet();

            // Assert
            mockStoreClient.Verify(f => f.UpdateAddOn(name, plan, null), Times.Once());
            mockConfirmation.Verify(f => f.ShouldProcess(Resources.SetAddOnConformation, message), Times.Once());
            mockCommandRuntime.Verify(f => f.WriteObject(expected), Times.Once());
        }

        [TestMethod]
        public void SetAzureStoreAddOnWithNo()
        {
            // Setup
            bool expected = true;
            string name = "TestAddOn";
            string addonId = "Search";
            string plan = "free";
            string message = "Expected message for set";
            cmdlet.Name = name;
            cmdlet.Plan = plan;
            WindowsAzureAddOn addon = new WindowsAzureAddOn(new Resource() { Type = addonId }, "West US", null);
            mockConfirmation.Setup(f => f.ShouldProcess(Resources.SetAddOnConformation, message)).Returns(false);
            mockStoreClient.Setup(f => f.TryGetAddOn(name, out addon)).Returns(true);
            mockStoreClient.Setup(f => f.UpdateAddOn(name, plan, null));
            mockStoreClient.Setup(f => f.GetConfirmationMessage(OperationType.Set, addonId, plan)).Returns(message);

            // Test
            cmdlet.ExecuteCmdlet();

            // Assert
            mockStoreClient.Verify(f => f.UpdateAddOn(name, plan, null), Times.Never());
            mockConfirmation.Verify(f => f.ShouldProcess(Resources.SetAddOnConformation, message), Times.Once());
            mockCommandRuntime.Verify(f => f.WriteObject(expected), Times.Never());
        }
    }
}