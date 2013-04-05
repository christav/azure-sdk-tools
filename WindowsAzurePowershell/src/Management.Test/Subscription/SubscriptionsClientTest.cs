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

namespace Microsoft.WindowsAzure.Management.Test.Subscription
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Management.Utilities.Common;
    using Management.Utilities.Subscriptions;
    using Management.Utilities.Subscriptions.Contract;
    using Moq;
    using Moq.Protected;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SubscriptionsClientTest
    {
        private SubscriptionData subscriptionData;

        [TestInitialize]
        public void Setup()
        {
            subscriptionData = new SubscriptionData
            {
                SubscriptionId = "test-id",
                ServiceEndpoint = "https://fake.endpoint.example"
            };

        }

        [TestMethod]
        public void CanGetListOfRegisteredProviders()
        {
            string[] knownResourceTypes = {"website", "mobileservice"};

            var mockHandler = CreateMockHandler(() => CreateListResourcesResponseMessage(
                new ProviderResource {Type = "Website", State = "Unregistered"},
                new ProviderResource {Type = "Mobileservice", State = "Registered"}
                ));

            ISubscriptionClient client = new SubscriptionClient(subscriptionData, mockHandler);
            IEnumerable<ProviderResource> actualResourceTypes = client.ListResources(knownResourceTypes);

            CollectionAssert.AreEquivalent(knownResourceTypes, actualResourceTypes.Select(rt => rt.Type.ToLower()).ToList());
        }

        [TestMethod]
        public void CanRegisterProviderIfUnregistered()
        {
            var mockHandler = CreateMockHandler(() => CreateResponseMessageWithStatus(HttpStatusCode.OK));

            ISubscriptionClient client = new SubscriptionClient(subscriptionData, mockHandler);
            bool worked = client.RegisterResourceType("someResource");

            Assert.IsTrue(worked);
        }

        [TestMethod]
        public void RegisterProviderReturnsFalseIfAlreadyRegistered()
        {
            var mockHandler = CreateMockHandler(() => CreateResponseMessageWithStatus(HttpStatusCode.Conflict));

            ISubscriptionClient client = new SubscriptionClient(subscriptionData, mockHandler);
            bool worked = client.RegisterResourceType("someResource");

            Assert.IsFalse(worked);
        }

        [TestMethod]
        public void RegisterProviderThrowsOnServerError()
        {
            var mockHandler = CreateMockHandler(() => CreateResponseMessageWithStatus(HttpStatusCode.BadRequest));

            ISubscriptionClient client = new SubscriptionClient(subscriptionData, mockHandler);

            try
            {
                client.RegisterResourceType("someResource");
                Assert.Fail("Should have gotten an exception");
            }
            catch (HttpRequestException ex)
            {
                Assert.AreNotEqual(-1, ex.Message.IndexOf("400", StringComparison.InvariantCulture));
                // If we get here we're good
            }
        }

        [TestMethod]
        public void CanUnregisterProviderIfRegistered()
        {
            var mockHandler = CreateMockHandler(() => CreateResponseMessageWithStatus(HttpStatusCode.OK));

            ISubscriptionClient client = new SubscriptionClient(subscriptionData, mockHandler);
            bool worked = client.UnregisterResourceType("someResource");

            Assert.IsTrue(worked);
        }


        [TestMethod]
        public void UnregisterProviderReturnsFalseIfAlreadyRegistered()
        {
            var mockHandler = CreateMockHandler(() => CreateResponseMessageWithStatus(HttpStatusCode.Conflict));

            ISubscriptionClient client = new SubscriptionClient(subscriptionData, mockHandler);
            bool worked = client.UnregisterResourceType("someResource");

            Assert.IsFalse(worked);
        }

        [TestMethod]
        public void UnregisterProviderThrowsOnServerError()
        {
            var mockHandler = CreateMockHandler(() => CreateResponseMessageWithStatus(HttpStatusCode.BadRequest));

            ISubscriptionClient client = new SubscriptionClient(subscriptionData, mockHandler);

            try
            {
                client.UnregisterResourceType("someResource");
                Assert.Fail("Should have gotten an exception");
            }
            catch (HttpRequestException ex)
            {
                Assert.AreNotEqual(-1, ex.Message.IndexOf("400", StringComparison.InvariantCulture));
                // If we get here we're good
            }
        }
        
        private HttpResponseMessage CreateListResourcesResponseMessage(params ProviderResource[] expectedResources)
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(CreateListResourcesResponseContent(expectedResources))
            };

            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");

            return response;
        }

        private string CreateListResourcesResponseContent(IEnumerable<ProviderResource> expectedResources)
        {
            XNamespace azureNs = ManagementConstants.ServiceManagementNS;

            var doc = new XDocument(
                new XElement(azureNs + "Services",
                    from resource in expectedResources 
                    select new XElement(azureNs + "Service",
                        new XElement(azureNs + "Resources"),
                        new XElement(azureNs + "State", resource.State),
                        new XElement(azureNs + "Type", resource.Type)
                    )
                )
            );

            return doc.ToString();
        }

        private HttpResponseMessage CreateResponseMessageWithStatus(HttpStatusCode status)
        {
            return new HttpResponseMessage
            {
                StatusCode = status
            };
        }

        private HttpMessageHandler CreateMockHandler(Func<HttpResponseMessage> responseGenerator)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(() => Task.Factory.StartNew(responseGenerator));

            return mock.Object;
        }
    }
}
