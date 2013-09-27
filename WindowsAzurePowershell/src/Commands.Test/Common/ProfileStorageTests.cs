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
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Security.Cryptography.X509Certificates;
    using Commands.Utilities.Common;
    using Moq;
    using Utilities.Resources;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ProfileStorageTests
    {
        private static ProfileData testProfileData = new ProfileData
        {
            DefaultEnvironmentName = "AzureCloud",
            Environments = new[]
            {
                new AzureEnvironmentData
                {
                    AdTenantUrl = "adtenant",
                    CommonTenantId = "Common",
                    ManagementPortalUrl = "https://management.windowsazure.net/",
                    ServiceEndpoint = "https://do.some.stuff",
                    Name = "Custom1",
                    PublishSettingsFileUrl = "SomeUrl",
                    StorageEndpointSuffix = "storage.on.azure"                    },
                new AzureEnvironmentData
                {
                    AdTenantUrl = "adtenant2",
                    CommonTenantId = "Common",
                    ManagementPortalUrl = "https://management.windowsazure.net/2",
                    ServiceEndpoint = "https://do.some.other.stuff",
                    Name = "Custom2",
                    PublishSettingsFileUrl = "SomeUrl2",
                    StorageEndpointSuffix = "storage.on.other"
                }
            },
            Subscriptions = new []
            {
                new AzureSubscriptionData
                {
                    Name = "subscription1",
                    IsDefault = false,
                    ManagementCertificate = "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",
                    ManagementEndpoint = "https://do.some.stuff",
                    SubscriptionId = "subscription1Id"
                }, 
                new AzureSubscriptionData
                {
                    Name = "subscription2",
                    IsDefault = true,
                    ManagementCertificate = "MIIKJAIBAzCCCeQGCSqGSIb3DQEHAaCCCdUEggnRMIIJzTCCBe4GCSqGSIb3DQEHAaCCBd8EggXbMIIF1zCCBdMGCyqGSIb3DQEMCgECoIIE7jCCBOowHAYKKoZIhvcNAQwBAzAOBAjilB4DFutYJwICB9AEggTItMCor/6dq+ynHyoo82U2N8bT9fBn57xuvF4zTtZdl503n+q48ZE5SLcUFoeAZkrYoCiyPn4ayVA4pfAHou5I2XEG1B4YF46hD0Bz0igWRSrsVigdoYP98BGGaMgl43d9AQGeV8iJ3d3In/TxMGjHUYzZwoIg1jE7xhQ8dMr2Xenw8pLrxl8FybI1isyxzAUjFE7E/Znv9DYi83VNwjC1uPg8q16PzXUQ/smFVzoZMtvmp8MxPrnI/gHqcS5g7SnnisTLmJcjqdLVywBZqiMo1ALs90EEgc7qgbim9lxGczUh+SI9cj2m5w9XMmXro4XJNJTLFG26DDOVMPfMSr9ij9P4rmxckVK7nHrGhQpshrLr37dF5KGFo6mh79VUadbwn/a4rXjfX9gXm5N/ZS8wq3U4/4Pl7t5N+bwB5izt8JG4aMhX6M6jshNrpe/gZHI9u6jNAo1yRxNfBdoxA7P2sZdlHO4CYTc9zZcZqTgH2QjRLTelIDn17PEQL9L4rEzqhT322WMzNnSMH9TCu3D5l2RuO6hsHl0JK4saiq3s04kkYoLXF9i+ovS0xSmu0zxemnFAGB1q1mlwoWoD06zlXEjHM2Q3T2b8ip1tK6/GFpU8Qs5BOUDanBOCqVLWlyvM/ilXUyN9cyLRMKM1sgEmn5ue0wsZlflU6egqChF8qjSJzq/34FgTjPazvkXkXv0e2vBz5+qzeC/1R8xySdFoehglny42VTkCRH4BzhoXf+MrfrC6tW85WCTKOj8SiTSzYXRragIwfG8RyLViOzdIW9pEAJF3UOloKOGGL1NREAnRPgxm9UVxD1oUj+pqYkPRRXcHuEnbiYEqE8Dgwk6GaSVOZ4CKjKAcapOwwW8bTxHgFOCrwgZhxIFXQhIZVoH8NphqN2WWwIUPa1gsc3uPwVXecgt8y8S01QEYCCFo9dT5sBS0rAOXMTOnSudWSHvz7c36IJSG2KyJwW3YO2UopIQ1V14MBZQhwUyddUILeuOy50u1j2eVOV3XESHO99oNP9FfalmgZw19LQDqX8S861x1w+GuU/NG//LZ0aXXaw1IhddIMZlpZVTADMunXIJbd0OiunfblXFwGZ33M1y/wGvFAZ6ofOuZv6vM0kmtufg3AHl/Vg+jzLOp1bYbKx4f7FHoYAerV88EA/ELXr2NTOLwwRYdk0cLWk4VY2lCLs8lcyoIUrcOS/+af8oX8dgJo9qkx2AiKp6AgYAWwrdpolOH7sMLmtu1rrthoMesExLz6xpUq/rYrWQJuyXWUmwbdxpDYFP8spqcW3KdbroNWhPEvM0tdocSK6lPWNnFMgqbb2qJJqjyV87LBZPEpHI8TPraofE7h4NWjXx/OqA6/dF1t3RvrvYqyC7kvrnaJ2LWfQI/88K9s7LAVvfDIbxWtIadrGXlo4gbtbQDSFzjve123DngBJkXqpzqRoL7mdpFvsgpg0upIKQ1fIbtaksC115g8BGBOzwGlo0Y3f4+ob6++OkePHoLkGhLahCMyDmGV1mxFz3ZUkXyxmfPSeynwXe/N8TxeZ2ixLZMF3sa61CpFsuHfEmVEetFxP5t3rrO5ZIbE87KVtvl6jCr8JQ3h81TZJBaeu8iiNC0MVspJpNQ/irYFElTMYHRMBMGCSqGSIb3DQEJFTEGBAQBAAAAMFsGCSqGSIb3DQEJFDFOHkwAewA0ADgANQBFADQAOQBCADYALQBFAEUARQA4AC0ANAA3ADUAQgAtADgAOQAwAEQALQA5ADcAQQA3ADYARgAyADQANgBEADkAMwB9MF0GCSsGAQQBgjcRATFQHk4ATQBpAGMAcgBvAHMAbwBmAHQAIABTAG8AZgB0AHcAYQByAGUAIABLAGUAeQAgAFMAdABvAHIAYQBnAGUAIABQAHIAbwB2AGkAZABlAHIwggPXBgkqhkiG9w0BBwagggPIMIIDxAIBADCCA70GCSqGSIb3DQEHATAcBgoqhkiG9w0BDAEGMA4ECG9kWMFPd2j2AgIH0ICCA5AUBLyrnhFVIYZKNWVLOWn0nfwmhADWS2FA3LGyGirb/lgpPcolLiQwGnXih0xxESn1CsZcWDpXiUvAfjQF1kxKHyCIUQBkrKQliYIT+RErliVuAY/vv1YW2Zj+bPUtTZKXUDzIPjNgb43+uxvf/wu+gGhAV/dV5oIWLjFhC1u4+Gp/LA5C6j60NtBXG7barSflAWTSOjGt2IIb5mBrUw+GkrhoYOqA+HYG40j2fkmkWpMCkImzcxxEM65ZElGUt7H1QY+GSRAxt7icA5ka9L+A0UM8a1SCFhbBK6Voo0IAkBZctJ6I7h4znhoHtqMDYYzraaYDVAK4SPdwOUMUyYdai0QwOYSL3frwVzC/ZHvCJkRmOsQXj9U44OGoXXrJ4rWIQIkcxFO3rEC3alI9lV5h5w73DWQRjex8Nz214B1yBRdlkoC/HQpgJ6IwFfEyJOn/lGgqkRPbgntTKSjNQZr5Ot60Z1SUYmmcMTpB8jRg+hy0LbWmx+79q9ERUnLO4yrtcXjQza12/FwAdpJOwbFrXMZb3QcuhQfn9aDF9/iNRkhTdxDmumS/C5gjZSYBzTugGDWsyS1hqws7LaYfcs6aWWRafqxt68cpNy4FaNXZ3XwXRVzuH+brnGvnWXRqhzwCbeGxEKDCEPxO9hO8NVrndsGlGfTZmxfTkKnPyRPD6vk4BG0Rc5BniyrmhnaZgSq0M04MeoAjp1s6S8CcIG73H5KkmoqQwSiKUbY3aA15nxqYhQj6L83WK5dPnVlmaV/xOeqkggzsdkaa+eQfA1e5RR27Gkyr5Rl20PQUR6J/sIGWIVCSSaqD2kxmDTODEORsF7jhL4YXZr96hqvNWtyNncxrqvjPsaFi/P2JFxjfZ8wmnF1HDsVW4W/i8cdRTyEz7Go4kzoRvSvC2sCPRAMa3D+o341r7L0hBlCnFfMU5Le8jatMKsw+Nk1TeOc4Cvc+w3gczSKrlhJnPtJjVZ67kKe8Ror8mKOP6afSr27avEizUYvJcCpKztUM59ukEbM2chEb2rrFPWxnB67KaLF825pRm+6Nl3mx0jaPDgK2ToydGfuVBA+9TSpnuV26imsd+K2yL2nwrdvBJPE/t2lPzVIR0hnf4AJ8/9BR0vTGmxiWwy8VMxrS3PyouLPZMXAgdT6ddRVwmewNjTe5g/tciGazIW+nROgg6fsgyObMp7keONMvtFMrJQLa2oKarGkwNzAfMAcGBSsOAwIaBBQXFDnqplMX7OuyknHK7B+HA/N8tAQUsL21+IY37DPL968vhVzqz09W/so=",
                    ManagementEndpoint = "https://do.some.other.stuff",
                    SubscriptionId = "subscription2Id"
                }
            }
        };

        [ClassInitialize]
        public static void SetupCerts(TestContext ctx)
        {
            X509Certificate2 cert =
                new X509Certificate2(
                    Convert.FromBase64String(testProfileData.Subscriptions.First().ManagementCertificate), string.Empty);
            X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            certStore.Open(OpenFlags.ReadWrite);
            certStore.Add(cert);
            certStore.Close();

            foreach (var s in testProfileData.Subscriptions)
            {
                s.ManagementCertificate = cert.Thumbprint;
            }
        }

        [ClassCleanup]
        public static void RemoveCerts()
        {
            X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            certStore.Open(OpenFlags.ReadWrite);
            var certs = certStore.Certificates.Find(X509FindType.FindByThumbprint,
                testProfileData.Subscriptions.First().ManagementCertificate, false);
            certStore.Remove(certs[0]);
            certStore.Close();
        }

        [TestMethod]
        public void DataContractSerializedToXmlAsExpected()
        {
            var profileToSerialize = testProfileData;

            var roundTripped = RoundTrip(profileToSerialize);

            Assert.AreEqual(roundTripped.DefaultEnvironmentName, profileToSerialize.DefaultEnvironmentName);
            Assert.AreEqual(profileToSerialize.Environments.Count(), roundTripped.Environments.Count());
            var environments = roundTripped.Environments.Zip(profileToSerialize.Environments, Tuple.Create);

            foreach (var pair in environments)
            {
                AssertEqual(pair.Item2, pair.Item1);
            }

            Assert.AreEqual(profileToSerialize.Subscriptions.Count(), roundTripped.Subscriptions.Count());
            var subscriptions = roundTripped.Subscriptions.Zip(profileToSerialize.Subscriptions, Tuple.Create);
            foreach (var pair in subscriptions)
            {
                AssertEqual(pair.Item2, pair.Item1);
            }
        }

        [TestMethod]
        public void SavingWritesProfileDataToStore()
        {
            var storeMock = new Mock<IProfileStore>();
            ProfileData savedData = null;
            storeMock.Setup(s => s.Save(It.IsAny<ProfileData>()))
                .Callback((ProfileData data) => { savedData = data; });

            var profile = new WindowsAzureProfile(storeMock.Object);
            var sourceEnv = testProfileData.Environments.First();
            profile.AddEnvironment(new WindowsAzureEnvironment
            {
                ManagementPortalUrl = sourceEnv.ManagementPortalUrl,
                Name = sourceEnv.Name,
                PublishSettingsFileUrl = sourceEnv.PublishSettingsFileUrl,
                ServiceEndpoint = sourceEnv.ServiceEndpoint,
                AdTenantUrl = sourceEnv.AdTenantUrl,
                CommonTenantId = sourceEnv.CommonTenantId,
                StorageEndpointSuffix = sourceEnv.StorageEndpointSuffix
            });

            var locator = typeof (ResourceLocator);
            profile.ImportPublishSettings(locator.Assembly
                .GetManifestResourceStream(locator, "Azure.publishsettings"));

            // Should save twice, one for environment, one for subscription import
            storeMock.Verify(s => s.Save(It.IsAny<ProfileData>()), Times.Exactly(2));

            // Validate that the saved data looks right
            Assert.AreEqual(1, savedData.Environments.Count());
            Assert.AreEqual(EnvironmentName.AzureCloud, savedData.DefaultEnvironmentName);
            AssertEqual(sourceEnv, savedData.Environments.First());
            Assert.AreEqual(3, savedData.Subscriptions.Count());
        }

        [TestMethod]
        public void ProfileLoadsFromStoreOnConstruction()
        {
            var storeMock = new Mock<IProfileStore>();
            storeMock.Setup(s => s.Load())
                .Returns(testProfileData);

            var profile = new WindowsAzureProfile(storeMock.Object);

            storeMock.Verify(s => s.Load(), Times.Once);
            Assert.AreEqual(testProfileData.DefaultEnvironmentName, profile.CurrentEnvironment.Name);
            Assert.AreEqual(testProfileData.Environments.Count(),
                profile.Environments.Count - WindowsAzureEnvironment.PublicEnvironments.Count);
            Assert.IsTrue(profile.Environments.ContainsKey(testProfileData.Environments.First().Name));
            Assert.IsTrue(profile.Environments.ContainsKey(testProfileData.Environments.Skip(1).Take(1).First().Name));

            Assert.AreEqual("subscription1Id", profile.Subscriptions.First().SubscriptionId);
            Assert.AreEqual("subscription2Id", profile.Subscriptions.Skip(1).First().SubscriptionId);
        }

        private T RoundTrip<T>(T objectToSerialize)
        {
            var writeSerializer = new DataContractSerializer(typeof (T));
            MemoryStream s = new MemoryStream();
            writeSerializer.WriteObject(s, objectToSerialize);

            s.Seek(0, SeekOrigin.Begin);

            var readSerializer = new DataContractSerializer(typeof (T));
            return (T)readSerializer.ReadObject(s);
        }

        private void AssertEqual(AzureEnvironmentData expected, AzureEnvironmentData actual)
        {
            Assert.IsNotNull(expected);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.AdTenantUrl, actual.AdTenantUrl);
            Assert.AreEqual(expected.CommonTenantId, actual.CommonTenantId);
            Assert.AreEqual(expected.ManagementPortalUrl, actual.ManagementPortalUrl);
            Assert.AreEqual(expected.ServiceEndpoint, actual.ServiceEndpoint);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.PublishSettingsFileUrl, actual.PublishSettingsFileUrl);
            Assert.AreEqual(expected.StorageEndpointSuffix, actual.StorageEndpointSuffix);
        }

        private void AssertEqual(AzureSubscriptionData expected, AzureSubscriptionData actual)
        {
            Assert.IsNotNull(expected);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.SubscriptionId, actual.SubscriptionId);
            Assert.AreEqual(expected.ManagementEndpoint, actual.ManagementEndpoint);
            Assert.AreEqual(expected.ManagementCertificate, actual.ManagementCertificate);
            Assert.AreEqual(expected.IsDefault, actual.IsDefault);
        }
    }
}
