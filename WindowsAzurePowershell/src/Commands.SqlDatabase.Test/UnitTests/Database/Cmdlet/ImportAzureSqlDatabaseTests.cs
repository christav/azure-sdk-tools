// ----------------------------------------------------------------------------------
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

namespace Microsoft.WindowsAzure.Commands.SqlDatabase.Test.UnitTests.Database.Cmdlet
{
    using System;
    using System.Xml;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Commands.SqlDatabase.Database.Cmdlet;
    using Microsoft.WindowsAzure.Commands.SqlDatabase.Services.ImportExport;
    using Microsoft.WindowsAzure.Commands.Test.Utilities.Common;
    using Microsoft.WindowsAzure.Commands.Utilities.Common;

    /// <summary>
    /// Test class for testing the Import-AzureSqlDatabase cmdlet
    /// </summary>
    [TestClass]
    public class ImportAzureSqlDatabaseTests : TestBase
    {
        /// <summary>
        /// Tests the ImportAzureSqlDatabaseProcess function 
        /// </summary>
        [TestMethod]
        public void ImportAzureSqlDatabaseProcessTest()
        {
            string serverName = "TestServer";
            ImportInput input = new ImportInput()
            {
                AzureEdition = "Web",
                DatabaseSizeInGB = 1,
                BlobCredentials = new BlobStorageAccessKeyCredentials()
                {
                    Uri = "blobUri",
                    StorageAccessKey = "storage access key"
                },
                ConnectionInfo = new ConnectionInfo()
                {
                    DatabaseName = "databaseName",
                    Password = "password",
                    ServerName = "serverName",
                    UserName = "userName"
                }
            };

            Guid testGuid = Guid.NewGuid();

            MockCommandRuntime commandRuntime = new MockCommandRuntime();
            SimpleSqlDatabaseManagement channel = new SimpleSqlDatabaseManagement();
            channel.ImportDatabaseThunk = ar =>
            {
                Assert.AreEqual(serverName, (string)ar.Values["serverName"]);
                Assert.AreEqual(
                    input.AzureEdition,
                    ((ImportInput)ar.Values["input"]).AzureEdition);
                Assert.AreEqual(
                    input.DatabaseSizeInGB,
                    ((ImportInput)ar.Values["input"]).DatabaseSizeInGB);
                Assert.AreEqual(
                    input.BlobCredentials.Uri,
                    ((ImportInput)ar.Values["input"]).BlobCredentials.Uri);
                Assert.AreEqual(
                    input.ConnectionInfo.DatabaseName,
                    ((ImportInput)ar.Values["input"]).ConnectionInfo.DatabaseName);
                Assert.AreEqual(
                    input.ConnectionInfo.Password,
                    ((ImportInput)ar.Values["input"]).ConnectionInfo.Password);
                Assert.AreEqual(
                    input.ConnectionInfo.ServerName,
                    ((ImportInput)ar.Values["input"]).ConnectionInfo.ServerName);
                Assert.AreEqual(
                    input.ConnectionInfo.UserName,
                    ((ImportInput)ar.Values["input"]).ConnectionInfo.UserName);
                
                XmlElement operationResult = 
                    new XmlDocument().CreateElement(
                        "guid", 
                        "http://schemas.microsoft.com/2003/10/Serialization/");

                operationResult.InnerText = testGuid.ToString();
                return operationResult;
            };

            StartAzureSqlDatabaseImport importAzureSqlDatabase =
                new StartAzureSqlDatabaseImport(channel) { ShareChannel = true };
            importAzureSqlDatabase.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            importAzureSqlDatabase.CommandRuntime = commandRuntime;
            var result = importAzureSqlDatabase.ImportSqlAzureDatabaseProcess(serverName, input);
            Assert.AreEqual(testGuid.ToString(), result.RequestGuid);

            Assert.AreEqual(0, commandRuntime.ErrorStream.Count);
        }
    }
}
