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
    /// Test class for testing the Export-AzureSqlDatabase cmdlet
    /// </summary>
    [TestClass]
    public class ExportAzureSqlDatabaseTests : TestBase
    {
        /// <summary>
        /// Tests the ExportAzureSqlDatabaseProcess function 
        /// </summary>
        [TestMethod]
        public void ExportAzureSqlDatabaseProcessTest()
        {
            string serverName = "TestServer";
            ExportInput input = new ExportInput()
            {
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

            Guid guid = Guid.NewGuid();

            MockCommandRuntime commandRuntime = new MockCommandRuntime();
            SimpleSqlDatabaseManagement channel = new SimpleSqlDatabaseManagement();
            channel.ExportDatabaseThunk = ar =>
            {
                Assert.AreEqual(serverName, (string)ar.Values["serverName"]);
                Assert.AreEqual(
                    input.BlobCredentials.Uri, 
                    ((ExportInput)ar.Values["input"]).BlobCredentials.Uri);
                Assert.AreEqual(
                    input.ConnectionInfo.DatabaseName, 
                    ((ExportInput)ar.Values["input"]).ConnectionInfo.DatabaseName);
                Assert.AreEqual(
                    input.ConnectionInfo.Password,
                    ((ExportInput)ar.Values["input"]).ConnectionInfo.Password);
                Assert.AreEqual(
                    input.ConnectionInfo.ServerName,
                    ((ExportInput)ar.Values["input"]).ConnectionInfo.ServerName);
                Assert.AreEqual(
                    input.ConnectionInfo.UserName, 
                    ((ExportInput)ar.Values["input"]).ConnectionInfo.UserName);
                
                XmlElement operationResult = 
                    new XmlDocument().CreateElement(
                        "guid", 
                        "http://schemas.microsoft.com/2003/10/Serialization/");

                operationResult.InnerText = guid.ToString();
                return operationResult;
            };

            StartAzureSqlDatabaseExport exportAzureSqlDatabase =
                new StartAzureSqlDatabaseExport(channel) { ShareChannel = true };
            exportAzureSqlDatabase.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            exportAzureSqlDatabase.CommandRuntime = commandRuntime;
            
            var result = exportAzureSqlDatabase.ExportSqlAzureDatabaseProcess(serverName, input);
            Assert.AreEqual(guid.ToString(), result.RequestGuid);

            Assert.AreEqual(0, commandRuntime.ErrorStream.Count);
        }
    }
}
