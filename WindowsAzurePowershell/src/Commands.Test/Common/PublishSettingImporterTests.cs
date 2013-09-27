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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Xml.Linq;
    using Commands.Utilities.Common;
    using Utilities.Resources;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PublishSettingImporterTests
    {
        [TestMethod]
        public void ImportingValidPublishSettingsSchema1ReturnsExpectedSubscriptions()
        {
            TestReading("Azure.publishsettings");
        }

        [TestMethod]
        public void ImportingValidPublishSettingsSchema2ReturnsExpectedSubscriptions()
        {
            TestReading("ValidProfile2.PublishSettings");
        }

        private void TestReading(string resourceName)
        {
            using (Stream s = GetPublishSettingsStream(resourceName))
            {
                XDocument expected = ReadExpected(s);

                List<WindowsAzureSubscription> subscriptions = ReadPublishSettings(s);

                Assert.AreEqual(expected.Descendants("Subscription").Count(), subscriptions.Count);

                var i = 0;
                foreach (var expectedSub in expected.Descendants("Subscription"))
                {
                    Assert.AreEqual(expectedSub.Attribute("Name").Value, subscriptions[i].Name);
                    Assert.AreEqual(ExpectedManagementCertificate(expected, expectedSub),
                        subscriptions[i].Certificate);
                    Assert.AreEqual(expectedSub.Attribute("Id").Value,
                        subscriptions[i].SubscriptionId);
                    Assert.AreEqual(ExpectedManagementUri(expected, expectedSub),
                                    subscriptions[i].ServiceEndpoint);
                    ++i;
                }
            }
        }

        private Stream GetPublishSettingsStream(string resourceName)
        {
            Type locator = typeof (ResourceLocator);
            return locator.Assembly.GetManifestResourceStream(locator, resourceName);
        }

        private XDocument ReadExpected(Stream s)
        {
            var xdoc = XDocument.Load(s);
            s.Seek(0, SeekOrigin.Begin);
            return xdoc;
        }

        private List<WindowsAzureSubscription> ReadPublishSettings(Stream s)
        {
            return PublishSettingsImporter.Import(s).ToList();
        }

        private X509Certificate2 ExpectedManagementCertificate(XDocument doc, XElement sub)
        {
            string base64Cert;
            if (sub.Attribute("ManagementCertificate") != null)
            {
                base64Cert = sub.Attribute("ManagementCertificate").Value;
            }
            else
            {
                base64Cert = doc.Descendants("PublishProfile").First()
                    .Attribute("ManagementCertificate").Value;
            }

            return new X509Certificate2(Convert.FromBase64String(base64Cert), string.Empty);
        }

        private Uri ExpectedManagementUri(XDocument doc, XElement sub)
        {
            if (sub.Attributes("ServiceManagementUrl").FirstOrDefault() != null)
            {
                return new Uri(sub.Attribute("ServiceManagementUrl").Value);
            }
            return new Uri(doc.Descendants("PublishProfile").First().Attribute("Url").Value);
        }
    }
}
