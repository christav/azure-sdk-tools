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

namespace Microsoft.WindowsAzure.Commands.SqlDatabase.Test.UnitTests.Firewall.Cmdlet
{
    using System.Linq;
    using System.ServiceModel;
    using Commands.Utilities.Common;
    using Commands.Test.Utilities.Common;
    using Services;
    using SqlDatabase.Firewall.Cmdlet;
    using VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AzureSqlDatabaseServerTests : TestBase
    {
        [TestMethod]
        public void NewAzureSqlDatabaseServerFirewallRuleProcessTest()
        {
            MockCommandRuntime commandRuntime = new MockCommandRuntime();
            SimpleSqlDatabaseManagement channel = new SimpleSqlDatabaseManagement();
            bool newFirewallRuleCalled = false;
            channel.NewServerFirewallRuleThunk = ar =>
            {
                newFirewallRuleCalled = true;
                Assert.AreEqual("Server1", (string)ar.Values["serverName"]);
                Assert.AreEqual("Rule1", ((SqlDatabaseFirewallRuleInput)ar.Values["input"]).Name);
                Assert.AreEqual("0.0.0.0", ((SqlDatabaseFirewallRuleInput)ar.Values["input"]).StartIPAddress);
                Assert.AreEqual("1.1.1.1", ((SqlDatabaseFirewallRuleInput)ar.Values["input"]).EndIPAddress);
            };

            // New firewall rule with IpRange parameter set
            NewAzureSqlDatabaseServerFirewallRule newAzureSqlDatabaseServerFirewallRule = new NewAzureSqlDatabaseServerFirewallRule(channel) { ShareChannel = true };
            newAzureSqlDatabaseServerFirewallRule.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            newAzureSqlDatabaseServerFirewallRule.CommandRuntime = commandRuntime;
            var newFirewallResult = newAzureSqlDatabaseServerFirewallRule.NewAzureSqlDatabaseServerFirewallRuleProcess("IpRange", "Server1", "Rule1", "0.0.0.0", "1.1.1.1");
            Assert.AreEqual("Server1", newFirewallResult.ServerName);
            Assert.AreEqual("Rule1", newFirewallResult.RuleName);
            Assert.AreEqual("0.0.0.0", newFirewallResult.StartIpAddress);
            Assert.AreEqual("1.1.1.1", newFirewallResult.EndIpAddress);
            Assert.AreEqual("Success", newFirewallResult.OperationStatus);
            Assert.AreEqual(true, newFirewallRuleCalled);
            Assert.AreEqual(0, commandRuntime.ErrorStream.Count);
        }

        [TestMethod]
        public void GetAzureSqlDatabaseServerFirewallRuleProcessTest()
        {
            SqlDatabaseFirewallRulesList firewallList = new SqlDatabaseFirewallRulesList();
            MockCommandRuntime commandRuntime = new MockCommandRuntime();
            SimpleSqlDatabaseManagement channel = new SimpleSqlDatabaseManagement();
            channel.NewServerFirewallRuleThunk = ar =>
            {
                Assert.AreEqual("Server1", (string)ar.Values["serverName"]);
                SqlDatabaseFirewallRule newRule = new SqlDatabaseFirewallRule();
                newRule.Name = ((SqlDatabaseFirewallRuleInput)ar.Values["input"]).Name;
                newRule.StartIPAddress = ((SqlDatabaseFirewallRuleInput)ar.Values["input"]).StartIPAddress;
                newRule.EndIPAddress = ((SqlDatabaseFirewallRuleInput)ar.Values["input"]).EndIPAddress;
                firewallList.Add(newRule);
            };

            channel.GetServerFirewallRulesThunk = ar =>
            {
                return firewallList;
            };

            // New firewall rule with IpRange parameter set
            NewAzureSqlDatabaseServerFirewallRule newAzureSqlDatabaseServerFirewallRule = new NewAzureSqlDatabaseServerFirewallRule(channel) { ShareChannel = true };
            newAzureSqlDatabaseServerFirewallRule.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            newAzureSqlDatabaseServerFirewallRule.CommandRuntime = commandRuntime;
            var newFirewallResult = newAzureSqlDatabaseServerFirewallRule.NewAzureSqlDatabaseServerFirewallRuleProcess("IpRange", "Server1", "Rule1", "0.0.0.0", "1.1.1.1");
            Assert.AreEqual("Success", newFirewallResult.OperationStatus);
            newFirewallResult = newAzureSqlDatabaseServerFirewallRule.NewAzureSqlDatabaseServerFirewallRuleProcess("IpRange", "Server1", "Rule2", "1.1.1.1", "2.2.2.2");
            Assert.AreEqual("Success", newFirewallResult.OperationStatus);

            // Get all rules
            GetAzureSqlDatabaseServerFirewallRule getAzureSqlDatabaseServerFirewallRule = new GetAzureSqlDatabaseServerFirewallRule(channel) { ShareChannel = true };
            getAzureSqlDatabaseServerFirewallRule.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            getAzureSqlDatabaseServerFirewallRule.CommandRuntime = commandRuntime;
            var getFirewallResult = getAzureSqlDatabaseServerFirewallRule.GetAzureSqlDatabaseServerFirewallRuleProcess("Server1", null);
            Assert.AreEqual(2, getFirewallResult.Count());
            var firstRule = getFirewallResult.First();
            Assert.AreEqual("Server1", firstRule.ServerName);
            Assert.AreEqual("Rule1", firstRule.RuleName);
            Assert.AreEqual("0.0.0.0", firstRule.StartIpAddress);
            Assert.AreEqual("1.1.1.1", firstRule.EndIpAddress);
            Assert.AreEqual("Success", firstRule.OperationStatus);
            var lastRule = getFirewallResult.Last();
            Assert.AreEqual("Server1", lastRule.ServerName);
            Assert.AreEqual("Rule2", lastRule.RuleName);
            Assert.AreEqual("1.1.1.1", lastRule.StartIpAddress);
            Assert.AreEqual("2.2.2.2", lastRule.EndIpAddress);
            Assert.AreEqual("Success", lastRule.OperationStatus);

            // Get one rule
            getFirewallResult = getAzureSqlDatabaseServerFirewallRule.GetAzureSqlDatabaseServerFirewallRuleProcess("Server1", "Rule2");
            Assert.AreEqual(1, getFirewallResult.Count());
            firstRule = getFirewallResult.First();
            Assert.AreEqual("Server1", firstRule.ServerName);
            Assert.AreEqual("Rule2", firstRule.RuleName);
            Assert.AreEqual("1.1.1.1", firstRule.StartIpAddress);
            Assert.AreEqual("2.2.2.2", firstRule.EndIpAddress);
            Assert.AreEqual("Success", firstRule.OperationStatus);

            Assert.AreEqual(0, commandRuntime.ErrorStream.Count);
        }

        [TestMethod]
        public void SetAzureSqlDatabaseServerFirewallRuleProcessTest()
        {
            SqlDatabaseFirewallRulesList firewallList = new SqlDatabaseFirewallRulesList();
            MockCommandRuntime commandRuntime = new MockCommandRuntime();
            SimpleSqlDatabaseManagement channel = new SimpleSqlDatabaseManagement();
            channel.NewServerFirewallRuleThunk = ar =>
            {
                Assert.AreEqual("Server1", (string)ar.Values["serverName"]);
                SqlDatabaseFirewallRule newRule = new SqlDatabaseFirewallRule();
                newRule.Name = ((SqlDatabaseFirewallRuleInput)ar.Values["input"]).Name;
                newRule.StartIPAddress = ((SqlDatabaseFirewallRuleInput)ar.Values["input"]).StartIPAddress;
                newRule.EndIPAddress = ((SqlDatabaseFirewallRuleInput)ar.Values["input"]).EndIPAddress;
                firewallList.Add(newRule);
            };

            channel.GetServerFirewallRulesThunk = ar =>
            {
                return firewallList;
            };

            channel.UpdateServerFirewallRuleThunk = ar =>
            {
                string ruleName = ((SqlDatabaseFirewallRuleInput)ar.Values["input"]).Name;
                Assert.AreEqual(ruleName, (string)ar.Values["ruleName"]);
                var ruleToUpdate = firewallList.SingleOrDefault((rule) => rule.Name == ruleName);
                if (ruleToUpdate == null)
                {
                    throw new CommunicationException("Firewall rule does not exist!");
                }

                ruleToUpdate.StartIPAddress = ((SqlDatabaseFirewallRuleInput)ar.Values["input"]).StartIPAddress;
                ruleToUpdate.EndIPAddress = ((SqlDatabaseFirewallRuleInput)ar.Values["input"]).EndIPAddress;
            };

            // New firewall rule with IpRange parameter set
            NewAzureSqlDatabaseServerFirewallRule newAzureSqlDatabaseServerFirewallRule = new NewAzureSqlDatabaseServerFirewallRule(channel) { ShareChannel = true };
            newAzureSqlDatabaseServerFirewallRule.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            newAzureSqlDatabaseServerFirewallRule.CommandRuntime = commandRuntime;
            var newFirewallResult = newAzureSqlDatabaseServerFirewallRule.NewAzureSqlDatabaseServerFirewallRuleProcess("IpRange", "Server1", "Rule1", "0.0.0.0", "1.1.1.1");
            Assert.AreEqual("Success", newFirewallResult.OperationStatus);

            // Get the rule
            GetAzureSqlDatabaseServerFirewallRule getAzureSqlDatabaseServerFirewallRule = new GetAzureSqlDatabaseServerFirewallRule(channel) { ShareChannel = true };
            getAzureSqlDatabaseServerFirewallRule.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            getAzureSqlDatabaseServerFirewallRule.CommandRuntime = commandRuntime;
            var getFirewallResult = getAzureSqlDatabaseServerFirewallRule.GetAzureSqlDatabaseServerFirewallRuleProcess("Server1", null);
            Assert.AreEqual(1, getFirewallResult.Count());
            var firstRule = getFirewallResult.First();
            Assert.AreEqual("Server1", firstRule.ServerName);
            Assert.AreEqual("Rule1", firstRule.RuleName);
            Assert.AreEqual("0.0.0.0", firstRule.StartIpAddress);
            Assert.AreEqual("1.1.1.1", firstRule.EndIpAddress);
            Assert.AreEqual("Success", firstRule.OperationStatus);

            // Update the rule
            SetAzureSqlDatabaseServerFirewallRule setAzureSqlDatabaseServerFirewallRule = new SetAzureSqlDatabaseServerFirewallRule(channel) { ShareChannel = true };
            setAzureSqlDatabaseServerFirewallRule.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            setAzureSqlDatabaseServerFirewallRule.CommandRuntime = commandRuntime;
            var setFirewallResult = setAzureSqlDatabaseServerFirewallRule.SetAzureSqlDatabaseServerFirewallRuleProcess("Server1", "Rule1", "2.2.2.2", "3.3.3.3");
            Assert.AreEqual("Server1", setFirewallResult.ServerName);
            Assert.AreEqual("Rule1", setFirewallResult.RuleName);
            Assert.AreEqual("2.2.2.2", setFirewallResult.StartIpAddress);
            Assert.AreEqual("3.3.3.3", setFirewallResult.EndIpAddress);
            Assert.AreEqual("Success", setFirewallResult.OperationStatus);

            // Get the rule again
            getFirewallResult = getAzureSqlDatabaseServerFirewallRule.GetAzureSqlDatabaseServerFirewallRuleProcess("Server1", "Rule1");
            Assert.AreEqual(1, getFirewallResult.Count());
            firstRule = getFirewallResult.First();
            Assert.AreEqual("Server1", firstRule.ServerName);
            Assert.AreEqual("Rule1", firstRule.RuleName);
            Assert.AreEqual("2.2.2.2", firstRule.StartIpAddress);
            Assert.AreEqual("3.3.3.3", firstRule.EndIpAddress);
            Assert.AreEqual("Success", firstRule.OperationStatus);

            Assert.AreEqual(0, commandRuntime.ErrorStream.Count);
        }

        [TestMethod]
        public void RemoveAzureSqlDatabaseServerFirewallRuleProcessTest()
        {
            SqlDatabaseFirewallRulesList firewallList = new SqlDatabaseFirewallRulesList();
            MockCommandRuntime commandRuntime = new MockCommandRuntime();
            SimpleSqlDatabaseManagement channel = new SimpleSqlDatabaseManagement();
            channel.NewServerFirewallRuleThunk = ar =>
            {
                Assert.AreEqual("Server1", (string)ar.Values["serverName"]);
                SqlDatabaseFirewallRule newRule = new SqlDatabaseFirewallRule();
                newRule.Name = ((SqlDatabaseFirewallRuleInput)ar.Values["input"]).Name;
                newRule.StartIPAddress = ((SqlDatabaseFirewallRuleInput)ar.Values["input"]).StartIPAddress;
                newRule.EndIPAddress = ((SqlDatabaseFirewallRuleInput)ar.Values["input"]).EndIPAddress;
                firewallList.Add(newRule);
            };

            channel.GetServerFirewallRulesThunk = ar =>
            {
                return firewallList;
            };

            channel.RemoveServerFirewallRuleThunk = ar =>
            {
                Assert.AreEqual("Server1", (string)ar.Values["serverName"]);
                string ruleName = (string)ar.Values["ruleName"];
                var ruleToDelete = firewallList.SingleOrDefault((rule) => rule.Name == ruleName);
                if (ruleToDelete == null)
                {
                    throw new CommunicationException("Firewall rule does not exist!");
                }

                firewallList.Remove(ruleToDelete);
            };

            // New firewall rule with IpRange parameter set
            NewAzureSqlDatabaseServerFirewallRule newAzureSqlDatabaseServerFirewallRule = new NewAzureSqlDatabaseServerFirewallRule(channel) { ShareChannel = true };
            newAzureSqlDatabaseServerFirewallRule.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            newAzureSqlDatabaseServerFirewallRule.CommandRuntime = commandRuntime;
            var newFirewallResult = newAzureSqlDatabaseServerFirewallRule.NewAzureSqlDatabaseServerFirewallRuleProcess("IpRange", "Server1", "Rule1", "0.0.0.0", "1.1.1.1");
            Assert.AreEqual("Success", newFirewallResult.OperationStatus);
            newFirewallResult = newAzureSqlDatabaseServerFirewallRule.NewAzureSqlDatabaseServerFirewallRuleProcess("IpRange", "Server1", "Rule2", "1.1.1.1", "2.2.2.2");
            Assert.AreEqual("Success", newFirewallResult.OperationStatus);

            // Get all rules
            GetAzureSqlDatabaseServerFirewallRule getAzureSqlDatabaseServerFirewallRule = new GetAzureSqlDatabaseServerFirewallRule(channel) { ShareChannel = true };
            getAzureSqlDatabaseServerFirewallRule.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            getAzureSqlDatabaseServerFirewallRule.CommandRuntime = commandRuntime;
            var getFirewallResult = getAzureSqlDatabaseServerFirewallRule.GetAzureSqlDatabaseServerFirewallRuleProcess("Server1", null);
            Assert.AreEqual(2, getFirewallResult.Count());

            // Remove Rule1
            RemoveAzureSqlDatabaseServerFirewallRule removeAzureSqlDatabaseServerFirewallRule = new RemoveAzureSqlDatabaseServerFirewallRule(channel) { ShareChannel = true };
            removeAzureSqlDatabaseServerFirewallRule.CurrentSubscription = UnitTestHelper.CreateUnitTestSubscription();
            removeAzureSqlDatabaseServerFirewallRule.CommandRuntime = commandRuntime;
            var removeServerContext = removeAzureSqlDatabaseServerFirewallRule.RemoveAzureSqlDatabaseServerFirewallRuleProcess("Server1", "Rule1");

            // Verify only one rule is left
            getFirewallResult = getAzureSqlDatabaseServerFirewallRule.GetAzureSqlDatabaseServerFirewallRuleProcess("Server1", null);
            Assert.AreEqual(1, getFirewallResult.Count());
            var firstRule = getFirewallResult.First();
            Assert.AreEqual("Server1", firstRule.ServerName);
            Assert.AreEqual("Rule2", firstRule.RuleName);
            Assert.AreEqual("1.1.1.1", firstRule.StartIpAddress);
            Assert.AreEqual("2.2.2.2", firstRule.EndIpAddress);
            Assert.AreEqual("Success", firstRule.OperationStatus);

            Assert.AreEqual(0, commandRuntime.ErrorStream.Count);

            // Remove Rule1 again
            removeServerContext = removeAzureSqlDatabaseServerFirewallRule.RemoveAzureSqlDatabaseServerFirewallRuleProcess("Server1", "Rule1");
            Assert.AreEqual(1, commandRuntime.ErrorStream.Count);
            Assert.IsTrue(commandRuntime.WarningStream.Count > 0);
        }
    }
}
