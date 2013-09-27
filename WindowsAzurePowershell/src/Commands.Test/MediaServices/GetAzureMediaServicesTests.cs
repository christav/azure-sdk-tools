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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Commands.MediaServices;
using Microsoft.WindowsAzure.Commands.Test.Utilities.Common;
using Microsoft.WindowsAzure.Commands.Utilities.Common;
using Microsoft.WindowsAzure.Commands.Utilities.MediaServices;
using Microsoft.WindowsAzure.Commands.Utilities.MediaServices.Services.Entities;
using Microsoft.WindowsAzure.ServiceManagement;
using Moq;

namespace Microsoft.WindowsAzure.Commands.Test.MediaServices
{
    [TestClass]
    public class GetAzureMediaServicesTests : TestBase
    {
        protected string SubscriptionId = "foo";

        [TestInitialize]
        public virtual void SetupTest()
        {
            new FileSystemHelper(this).CreateAzureSdkDirectoryAndImportPublishSettings();
        }

        [TestMethod]
        public void ProcessGetMediaServicesTest()
        {
            // Setup
            var clientMock = new Mock<IMediaServicesClient>();

            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();
            IEnumerable<MediaServiceAccount> accountsForMock = new List<MediaServiceAccount>
            {
                new MediaServiceAccount
                {
                    AccountId = id1,
                    Name = "WAMS Account 1"
                },
                new MediaServiceAccount
                {
                    AccountId = id2,
                    Name = "WAMS Account 2"
                }
            };
            clientMock.Setup(f => f.GetMediaServiceAccountsAsync()).Returns(Task.Factory.StartNew(() => { return accountsForMock; }));

            // Test
            var getAzureMediaServiceCommand = new GetAzureMediaServiceCommand
            {
                CommandRuntime = new MockCommandRuntime(),
                MediaServicesClient = clientMock.Object,
                CurrentSubscription = new WindowsAzureSubscription
                {
                    SubscriptionId = SubscriptionId
                }
            };

            getAzureMediaServiceCommand.ExecuteCmdlet();
            Assert.AreEqual(1, ((MockCommandRuntime) getAzureMediaServiceCommand.CommandRuntime).OutputPipeline.Count);
            var accounts = (IEnumerable<MediaServiceAccount>) ((MockCommandRuntime) getAzureMediaServiceCommand.CommandRuntime).OutputPipeline.FirstOrDefault();
            Assert.IsNotNull(accounts);
            Assert.IsTrue(accounts.Any(mediaservice => (mediaservice).AccountId == id1));
            Assert.IsTrue(accounts.Any(mediaservice => (mediaservice).AccountId == id2));
            Assert.IsTrue(accounts.Any(mediaservice => (mediaservice).Name.Equals("WAMS Account 1")));
            Assert.IsTrue(accounts.Any(mediaservice => (mediaservice).Name.Equals("WAMS Account 2")));
        }

        [TestMethod]
        public void ProcessGetMediaServiceByNameShouldReturnOneMatchingEntry()
        {
            var clientMock = new Mock<IMediaServicesClient>();


            string expectedName = "WAMS Account 1";
            var detail = new MediaServiceAccountDetails
            {
                Name = expectedName
            };

            clientMock.Setup(f => f.GetMediaServiceAsync(detail.Name)).Returns(Task.Factory.StartNew(() => { return detail; }));

            // Test
            var getAzureMediaServiceCommand = new GetAzureMediaServiceCommand
            {
                CommandRuntime = new MockCommandRuntime(),
                MediaServicesClient = clientMock.Object,
                CurrentSubscription = new WindowsAzureSubscription
                {
                    SubscriptionId = SubscriptionId
                }
            };
            getAzureMediaServiceCommand.Name = expectedName;
            getAzureMediaServiceCommand.ExecuteCmdlet();
            Assert.AreEqual(1, ((MockCommandRuntime) getAzureMediaServiceCommand.CommandRuntime).OutputPipeline.Count);
            var accounts = (MediaServiceAccountDetails) ((MockCommandRuntime) getAzureMediaServiceCommand.CommandRuntime).OutputPipeline.FirstOrDefault();
            Assert.IsNotNull(accounts);
            Assert.AreEqual(expectedName, accounts.Name);
        }

        [TestMethod]
        [ExpectedException(typeof (ServiceManagementClientException))]
        public void ProcessGetMediaServiceByNameShouldNotReturnEntriesForNoneMatchingName()
        {
            var clientMock = new Mock<IMediaServicesClient>();
            string mediaServicesAccountName = Guid.NewGuid().ToString();


            clientMock.Setup(f => f.GetMediaServiceAsync(mediaServicesAccountName)).Returns(Task.Factory.StartNew(() =>
            {
                if (String.IsNullOrEmpty(mediaServicesAccountName))
                {
                    return new MediaServiceAccountDetails();
                }
                throw new ServiceManagementClientException(HttpStatusCode.NotFound,
                    new ServiceManagementError
                    {
                        Code = HttpStatusCode.NotFound.ToString(),
                        Message = "Account not found"
                    },
                    string.Empty);
            }));

            // Test
            var getAzureMediaServiceCommand = new GetAzureMediaServiceCommand
            {
                CommandRuntime = new MockCommandRuntime(),
                MediaServicesClient = clientMock.Object,
                CurrentSubscription = new WindowsAzureSubscription
                {
                    SubscriptionId = SubscriptionId
                }
            };


            getAzureMediaServiceCommand.Name = mediaServicesAccountName;
            getAzureMediaServiceCommand.ExecuteCmdlet();
            Assert.AreEqual(0, ((MockCommandRuntime) getAzureMediaServiceCommand.CommandRuntime).OutputPipeline.Count);
        }
    }
}