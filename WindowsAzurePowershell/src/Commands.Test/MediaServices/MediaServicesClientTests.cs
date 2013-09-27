using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Commands.Utilities.Common;
using Microsoft.WindowsAzure.Commands.Utilities.MediaServices;
using Microsoft.WindowsAzure.Commands.Utilities.MediaServices.Services.Entities;
using Microsoft.WindowsAzure.ServiceManagement;

namespace Microsoft.WindowsAzure.Commands.Test.MediaServices
{
    [TestClass]
    public class MediaServicesClientTests
    {
        private string _accountName = "testacc";
        private string _subscriptionId = "foo";

        [TestMethod]
        public void TestDeleteAzureMediaServiceAccountAsync()
        {
            HttpClient fakeHttpClient = new FakeHttpMessageHandler().CreateIMediaServicesHttpClient();

            var target = new MediaServicesClient(new WindowsAzureSubscription
            {
                SubscriptionId = _subscriptionId
            },
                null,
                fakeHttpClient,
                fakeHttpClient);

            bool result = target.DeleteAzureMediaServiceAccountAsync(_accountName).Result;

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestDeleteAzureMediaServiceAccountAsync404()
        {
            var fakeHttpHandler = new FakeHttpMessageHandler();

            string responseText = "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">{\"Code\":\"NotFound\",\"Message\":\"The specified account was not found.\"}</string>";

            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new FakeHttpContent(responseText)
            };

            fakeHttpHandler.Send = request => response;

            HttpClient fakeHttpClient = fakeHttpHandler.CreateIMediaServicesHttpClient();

            var target = new MediaServicesClient(new WindowsAzureSubscription
            {
                SubscriptionId = _subscriptionId
            },
                null,
                fakeHttpClient,
                fakeHttpClient);

            try
            {
                bool result = target.DeleteAzureMediaServiceAccountAsync(_accountName).Result;
            }
            catch (AggregateException ax)
            {
                var x = (ServiceManagementClientException) ax.InnerExceptions.Single();
                Assert.AreEqual("NotFound", x.ErrorDetails.Code);
                return;
            }

            Assert.Fail("ServiceManagementClientException expected");
        }

        [TestMethod]
        public void TestRegenerateMediaServicesAccountAsync()
        {
            HttpClient fakeHttpClient = new FakeHttpMessageHandler().CreateIMediaServicesHttpClient();

            var target = new MediaServicesClient(new WindowsAzureSubscription
            {
                SubscriptionId = _subscriptionId
            },
                null,
                fakeHttpClient,
                fakeHttpClient);

            bool result = target.RegenerateMediaServicesAccountAsync(_accountName, "Primary").Result;

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestGetMediaServiceAsync()
        {
            var fakeHttpHandler = new FakeHttpMessageHandler();

            string responseText = "{\"AccountKey\":\"primarykey\",\"AccountKeys\":{\"Primary\":\"primarykey\",\"Secondary\":\"secondarykey\"},\"AccountName\":\"testps\",\"AccountRegion\":\"West US\",\"StorageAccountName\":\"nimbusorigintrial\"}";


            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new FakeHttpContent(responseText)
            };

            fakeHttpHandler.Send = request => response;

            HttpClient fakeHttpClient = fakeHttpHandler.CreateIMediaServicesHttpClient();

            var target = new MediaServicesClient(new WindowsAzureSubscription
            {
                SubscriptionId = _subscriptionId
            },
                null,
                fakeHttpClient,
                fakeHttpClient);

            MediaServiceAccountDetails result = target.GetMediaServiceAsync(_accountName).Result;
            Assert.AreEqual("primarykey", result.MediaServicesPrimaryAccountKey);
            Assert.AreEqual("secondarykey", result.MediaServicesSecondaryAccountKey);
            Assert.AreEqual("testps", result.Name);
        }

        [TestMethod]
        public void TestGetMediaServiceAccountsAsync()
        {
            var fakeHttpHandler = new FakeHttpMessageHandler();

            string responseText =
                "[{\"Name\":\"testps\",\"Type\":\"MediaService\",\"State\":\"Active\",\"AccountId\":\"E0658294-5C96-4B0F-AD55-F7446CE4F788\"},{\"Name\":\"test2\",\"Type\":\"MediaService\",\"State\":\"Active\",\"AccountId\":\"C92B17C8-5422-4CD1-8D3C-61E576E861DD\"}]";

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new FakeHttpContent(responseText)
            };

            fakeHttpHandler.Send = request => response;

            HttpClient fakeHttpClient = fakeHttpHandler.CreateIMediaServicesHttpClient();

            var target = new MediaServicesClient(new WindowsAzureSubscription
            {
                SubscriptionId = _subscriptionId
            },
                null,
                fakeHttpClient,
                fakeHttpClient);

            MediaServiceAccount[] result = target.GetMediaServiceAccountsAsync().Result.ToArray();
            Assert.AreEqual(Guid.Parse("E0658294-5C96-4B0F-AD55-F7446CE4F788"), result[0].AccountId);
            Assert.AreEqual(Guid.Parse("C92B17C8-5422-4CD1-8D3C-61E576E861DD"), result[1].AccountId);
        }

        [TestMethod]
        public void TestGetStorageServiceKeys()
        {
            var fakeHttpHandler = new FakeHttpMessageHandler();

            string responseText = "<StorageService xmlns=\"http://schemas.microsoft.com/windowsazure\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Url>https://management.core.windows.net/f7190519-c29e-47f2-9019-c5a94c8e75f9/services/storageservices/nimbusivshapo</Url><StorageServiceKeys><Primary>PrimaryKey</Primary><Secondary>SecondaryKey</Secondary></StorageServiceKeys></StorageService>";
           
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new FakeHttpContent(responseText),
                
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
            response.Content.Headers.ContentType.CharSet = "utf-8";

            fakeHttpHandler.Send = request => response;

            var fakeHttpClient = fakeHttpHandler.CreateIMediaServicesHttpClient();

            var target = new MediaServicesClient(
                new WindowsAzureSubscription { SubscriptionId = _subscriptionId },
                null,
                fakeHttpClient,
                fakeHttpClient);

            var result = target.GetStorageServiceKeysAsync(_accountName).Result;
            Assert.AreEqual("PrimaryKey", result.StorageServiceKeys.Primary);
            Assert.AreEqual("SecondaryKey", result.StorageServiceKeys.Secondary);
        }

        [TestMethod]
        public void TestGetStorageServiceKeysInvalidAccountName()
        {
            var fakeHttpHandler = new FakeHttpMessageHandler();

            string responseText = "<Error xmlns=\"http://schemas.microsoft.com/windowsazure\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Code>BadRequest</Code><Message>The name is not a valid.</Message></Error>";

            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new FakeHttpContent(responseText)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
            response.Content.Headers.ContentType.CharSet = "utf-8";

            fakeHttpHandler.Send = request => response;

            var fakeHttpClient = fakeHttpHandler.CreateIMediaServicesHttpClient();

            var target = new MediaServicesClient(
                new WindowsAzureSubscription { SubscriptionId = _subscriptionId },
                null,
                fakeHttpClient,
                fakeHttpClient);

            try
            {
                var result = target.GetStorageServiceKeysAsync(_accountName).Result;
            }
            catch (AggregateException ax)
            {
                ServiceManagementClientException x = (ServiceManagementClientException)ax.InnerExceptions.Single();

                Assert.AreEqual(HttpStatusCode.BadRequest, x.HttpStatus);
                return;
            }

            Assert.Fail("ServiceManagementClientException expected");
        }

        [TestMethod]
        public void TestCreateNewAzureMediaServiceAsync()
        {
            var fakeHttpHandler = new FakeHttpMessageHandler();

            string responseText = "{\"AccountId\":\"abe5afa0-704b-4d07-b5d8-5b0b039474e7\",\"AccountName\":\"tmp\",\"StatusCode\":201,\"Subscription\":\"f7190519-c29e-47f2-9019-c5a94c8e75f9\"}";

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new FakeHttpContent(responseText)
            };

            fakeHttpHandler.Send = request => response;

            var fakeHttpClient = fakeHttpHandler.CreateIMediaServicesHttpClient();

            var target = new MediaServicesClient(
                new WindowsAzureSubscription { SubscriptionId = _subscriptionId },
                null,
                fakeHttpClient,
                fakeHttpClient);

            var creationRequest = new AccountCreationRequest { AccountName = _accountName };

            var result = target.CreateNewAzureMediaServiceAsync(creationRequest).Result;
            Assert.AreEqual("tmp", result.Name);
        }

        [TestMethod]
        public void TestCreateNewAzureMediaServiceAsyncInvalidAccount()
        {
            var fakeHttpHandler = new FakeHttpMessageHandler();

            string responseText = "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\">{\"Code\":\"BadRequest\",\"Message\":\"Account Creation Request contains an invalid account name.\"}</string>";

            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new FakeHttpContent(responseText)
            };

            fakeHttpHandler.Send = request => response;

            var fakeHttpClient = fakeHttpHandler.CreateIMediaServicesHttpClient();

            var target = new MediaServicesClient(
                new WindowsAzureSubscription { SubscriptionId = _subscriptionId },
                null,
                fakeHttpClient,
                fakeHttpClient);

            var creationRequest = new AccountCreationRequest { AccountName = _accountName };

            try
            {
                var result = target.CreateNewAzureMediaServiceAsync(creationRequest).Result;
            }
            catch (AggregateException ax)
            {
                ServiceManagementClientException x = (ServiceManagementClientException)ax.InnerExceptions.Single();

                Assert.AreEqual(HttpStatusCode.BadRequest, x.HttpStatus);
                return;
            }

            Assert.Fail("ServiceManagementClientException expected");
        }
    }
}
