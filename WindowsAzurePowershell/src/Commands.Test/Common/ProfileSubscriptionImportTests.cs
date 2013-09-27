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

namespace Microsoft.WindowsAzure.Commands.Test.Common
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Commands.Utilities.Common;
    using Moq;
    using Utilities.Resources;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ProfileSubscriptionImportTests
    {
        [TestMethod]
        public void ImportingSchema1PublishSettingsAddsSubscriptionsToProfile()
        {
            var store = new Mock<IProfileStore>();
            var profile = new WindowsAzureProfile(store.Object);
            XDocument expected;

            using (Stream s = GetPublishSettingsStream("Azure.publishsettings"))
            {
                expected = ReadExpected(s);
                s.Seek(0, SeekOrigin.Begin);
                profile.ImportPublishSettings(s);
            }

            // This sample file has two subscriptions with the same ID.
            // When added to the profile the duplication is stripped out.
            Assert.AreEqual(expected.Descendants("Subscription").Count() - 1,
                profile.Subscriptions.Count);

            foreach (var id in expected.Descendants("Subscription").Select(s => s.Attribute("Id").Value))
            {
                Assert.IsTrue(profile.Subscriptions.Any(s => s.SubscriptionId == id));
            }

            foreach(var name in expected.Descendants("Subscription").Skip(1).Select(s => s.Attribute("Name").Value))
            {
                Assert.IsTrue(profile.Subscriptions.Any(s => s.Name == name));
            }

            Uri expectedManagementUri =
                expected.Descendants("PublishProfile").Select(p => new Uri(p.Attribute("Url").Value)).First();
            foreach (var s in profile.Subscriptions)
            {
                Assert.AreEqual(expectedManagementUri, s.ServiceEndpoint);
            }

            store.Verify(s => s.Save(It.IsAny<ProfileData>()), Times.Once);
        }


        [TestMethod]
        public void ImportingSchema2PublishSettingsAddsSubscriptionsToProfile()
        {
            var store = new Mock<IProfileStore>();
            var profile = new WindowsAzureProfile(store.Object);
            XDocument expected;

            using (Stream s = GetPublishSettingsStream("ValidProfile2.PublishSettings"))
            {
                expected = ReadExpected(s);
                s.Seek(0, SeekOrigin.Begin);
                profile.ImportPublishSettings(s);
            }

            Assert.AreEqual(expected.Descendants("Subscription").Count(), profile.Subscriptions.Count);

            foreach (var id in expected.Descendants("Subscription").Select(s => s.Attribute("Id").Value))
            {
                Assert.IsTrue(profile.Subscriptions.Any(s => s.SubscriptionId == id));
            }

            foreach (var name in expected.Descendants("Subscription").Select(s => s.Attribute("Name").Value))
            {
                Assert.IsTrue(profile.Subscriptions.Any(s => s.Name == name));
            }

            foreach (var uri in
                expected.Descendants("Subscription")
                .Select(s => new Uri(s.Attribute("ServiceManagementUrl").Value)))
            {
                Assert.IsTrue(profile.Subscriptions.Any(s => s.ServiceEndpoint == uri));
            }
        }

        private Stream GetPublishSettingsStream(string resourceName)
        {
            Type locator = typeof(ResourceLocator);
            return locator.Assembly.GetManifestResourceStream(locator, resourceName);
        }

        private XDocument ReadExpected(Stream s)
        {
            var xdoc = XDocument.Load(s);
            s.Seek(0, SeekOrigin.Begin);
            return xdoc;
        }

    }
}
