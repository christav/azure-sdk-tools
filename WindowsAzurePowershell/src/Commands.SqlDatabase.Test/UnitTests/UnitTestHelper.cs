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

namespace Microsoft.WindowsAzure.Commands.SqlDatabase.Test.UnitTests
{
    using System;
    using System.Collections.ObjectModel;
    using System.Management.Automation;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using Commands.Utilities.Common;
    using VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Common helper functions for SqlDatabase UnitTests.
    /// </summary>
    public static class UnitTestHelper
    {
        /// <summary>
        /// Manifest file for SqlDatabase Tests
        /// </summary>
        private static readonly string SqlDatabaseTestManifest =
            "Microsoft.WindowsAzure.Commands.SqlDatabase.Test.psd1";

        public static void CheckConfirmImpact(Type cmdlet, ConfirmImpact confirmImpact)
        {
            object[] cmdletAttributes = cmdlet.GetCustomAttributes(typeof(CmdletAttribute), true);
            Assert.AreEqual(1, cmdletAttributes.Length);
            CmdletAttribute attribute = (CmdletAttribute)cmdletAttributes[0];
            Assert.AreEqual(confirmImpact, attribute.ConfirmImpact);
        }

        public static void CheckCmdletModifiesData(Type cmdlet, bool supportsShouldProcess)
        {
            // If the Cmdlet modifies data, SupportsShouldProcess should be set to true.
            object[] cmdletAttributes = cmdlet.GetCustomAttributes(typeof(CmdletAttribute), true);
            Assert.AreEqual(1, cmdletAttributes.Length);
            CmdletAttribute attribute = (CmdletAttribute)cmdletAttributes[0];
            Assert.AreEqual(supportsShouldProcess, attribute.SupportsShouldProcess);

            if (supportsShouldProcess)
            {
                // If the Cmdlet modifies data, there needs to be a Force property to bypass
                // ShouldProcess.
                Assert.AreNotEqual(
                    null,
                    cmdlet.GetProperty("Force"),
                    "Force property is expected for Cmdlets that modifies data.");
            }
        }

        public static WindowsAzureSubscription CreateUnitTestSubscription()
        {
            return new WindowsAzureSubscription
            {
                Name = "TestSubscription",
                SubscriptionId = "00000000-0000-0000-0000-000000000000",
                Certificate = new X509Certificate2()
            };
        }

        /// <summary>
        /// Use reflection to invoke a private member of an object.
        /// </summary>
        /// <param name="instance">The object on which to invoke the method.</param>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="paramerters">An array of parameters for this method.</param>
        /// <returns>The return value for the method.</returns>
        public static object InvokePrivate(
            object instance,
            string methodName,
            params object[] paramerters)
        {
            Type cmdletType = instance.GetType();
            MethodInfo getManageUrlMethod = cmdletType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            try
            {
                return getManageUrlMethod.Invoke(instance, paramerters);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        public static void SetFieldValue(
            Type type,
            string fieldName,
            object value)
        {
            FieldInfo field = type.GetField(fieldName);
            field.SetValue(null, value);
        }

        /// <summary>
        /// Invokes an array of scripts using the specified powershell instance.
        /// </summary>
        /// <param name="powershell">The powershell instance that executes the scripts.</param>
        /// <param name="scripts">An array of script to execute.</param>
        public static Collection<PSObject> InvokeBatchScript(
            this PowerShell powershell,
            params string[] scripts)
        {
            if (powershell == null)
            {
                throw new ArgumentNullException("powershell");
            }

            powershell.Commands.Clear();

            foreach (string script in scripts)
            {
                Console.Error.WriteLine(script);
                powershell.AddScript(script);
            }

            Collection<PSObject> results = powershell.Invoke();
            powershell.DumpStreams();
            return results;
        }

        /// <summary>
        /// Dumps all powershell streams to the console.
        /// </summary>
        /// <param name="powershell">The powershell instance containing the streams.</param>
        public static void DumpStreams(this PowerShell powershell)
        {
            if (powershell == null)
            {
                throw new ArgumentNullException("powershell");
            }

            foreach (ProgressRecord record in powershell.Streams.Progress)
            {
                Console.Out.WriteLine("Progress: {0}", record.ToString());
            }

            foreach (DebugRecord record in powershell.Streams.Debug)
            {
                Console.Out.WriteLine("Debug: {0}", record.ToString());
            }

            foreach (VerboseRecord record in powershell.Streams.Verbose)
            {
                Console.Out.WriteLine("Verbose: {0}", record.ToString());
            }

            foreach (WarningRecord record in powershell.Streams.Warning)
            {
                Console.Error.WriteLine("Warning: {0}", record.ToString());
            }

            foreach (ErrorRecord record in powershell.Streams.Error)
            {
                Console.Error.WriteLine("Error: {0}", record.ToString());
            }
        }

        /// <summary>
        /// Imports the SqlDatabase Test Manifest to the given <paramref name="powershell"/>
        /// instance.
        /// </summary>
        /// <param name="powershell">An instance of the <see cref="PowerShell"/> object.</param>
        public static void ImportSqlDatabaseModule(PowerShell powershell)
        {
            // Import the test manifest file
            powershell.InvokeBatchScript(
                string.Format(@"Import-Module .\{0}", SqlDatabaseTestManifest));
            Assert.IsTrue(powershell.Streams.Error.Count == 0);
        }

        /// <summary>
        /// Creates the $credential object in the given <paramref name="powershell"/> instance with
        /// user name "testuser" and password "testpass".
        /// </summary>
        /// <param name="powershell">An instance of the <see cref="PowerShell"/> object.</param>
        public static void CreateTestCredential(PowerShell powershell)
        {
            CreateTestCredential(powershell, "testuser", "testp@ss1");
        }

        /// <summary>
        /// Creates the $credential object in the given <paramref name="powershell"/> instance with
        /// the given user name and password.
        /// </summary>
        /// <param name="powershell">An instance of the <see cref="PowerShell"/> object.</param>
        public static void CreateTestCredential(PowerShell powershell, string username, string password)
        {
            // Create the test credential
            powershell.InvokeBatchScript(
                string.Format(@"$user = ""{0}""", username),
                string.Format(@"$pass = ""{0}"" | ConvertTo-SecureString -asPlainText -Force", password),
                @"$credential = New-Object System.Management.Automation.PSCredential($user, $pass)");
            Assert.IsTrue(powershell.Streams.Error.Count == 0);
        }
    }
}
