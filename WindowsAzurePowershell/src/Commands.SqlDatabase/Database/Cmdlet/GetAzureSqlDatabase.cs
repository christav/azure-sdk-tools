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

namespace Microsoft.WindowsAzure.Commands.SqlDatabase.Database.Cmdlet
{
    using System;
    using System.Management.Automation;
    using Commands.Utilities.Common;
    using Properties;
    using Services.Common;
    using Services.Server;

    /// <summary>
    /// Retrieves a list of Windows Azure SQL Databases in the given server context.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "AzureSqlDatabase", ConfirmImpact = ConfirmImpact.None,
        DefaultParameterSetName = ByConnectionContext)]
    public class GetAzureSqlDatabase : PSCmdlet
    {
        #region Parameter Sets

        /// <summary>
        /// The parameter set string for connecting with a connection context
        /// </summary>
        internal const string ByConnectionContext =
            "ByConnectionContext";

        /// <summary>
        /// The parameter set string for connecting using azure subscription
        /// </summary>
        internal const string ByServerName =
            "ByServerName";

        #endregion

        #region Parameters

        /// <summary>
        /// Gets or sets the server connection context.
        /// </summary>
        [Alias("Context")]
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true,
            ParameterSetName = ByConnectionContext,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The connection context to the specified server.")]
        [ValidateNotNull]
        public IServerDataServiceContext ConnectionContext { get; set; }

        /// <summary>
        /// Gets or sets the server object upon which to operate
        /// </summary>
        [Parameter(Mandatory = true, Position = 0,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = ByServerName,
            HelpMessage = "The name of the server to operate on")]
        [ValidateNotNullOrEmpty]
        public string ServerName { get; set; }

        /// <summary>
        /// Gets or sets the database object to refresh.
        /// </summary>
        [Parameter(Mandatory = false,
            ValueFromPipeline = true, HelpMessage = "The database object to refresh.")]
        [ValidateNotNull]
        public Database Database { get; set; }

        /// <summary>
        /// Gets or sets the name of the database to retrieve.
        /// </summary>
        [Parameter(Mandatory = false,
            HelpMessage = "The name of the database to retrieve.")]
        [ValidateNotNullOrEmpty]
        public string DatabaseName { get; set; }

        #endregion

        /// <summary>
        /// Process the command.
        /// </summary>
        protected override void ProcessRecord()
        {
            // This is to enforce the mutual exclusivity of the parameters: Database
            // and DatabaseName.  This can't be done with parameter sets without changing
            // existing behaviour of the cmdlet.
            if (this.MyInvocation.BoundParameters.ContainsKey("Database") &&
                this.MyInvocation.BoundParameters.ContainsKey("DatabaseName"))
            {
                this.WriteError(new ErrorRecord(
                    new PSArgumentException( 
                        String.Format(Resources.InvalidParameterCombination, "Database", "DatabaseName")),
                    string.Empty,
                    ErrorCategory.InvalidArgument,
                    null));
            }

            // Obtain the database name from the given parameters.
            string databaseName = null;
            if (this.MyInvocation.BoundParameters.ContainsKey("Database"))
            {
                databaseName = this.Database.Name;
            }
            else if (this.MyInvocation.BoundParameters.ContainsKey("DatabaseName"))
            {
                databaseName = this.DatabaseName;
            }

            switch (this.ParameterSetName)
            {
                case ByConnectionContext:
                    this.ProcessWithConnectionContext(databaseName);
                    break;

                case ByServerName:
                    this.ProcessWithServerName(databaseName);
                    break;
            }
        }

        /// <summary>
        /// Process the record with the provided server name
        /// </summary>
        /// <param name="databaseName">The name of the database to retrieve</param>
        private void ProcessWithServerName(string databaseName)
        {
            string clientRequestId = string.Empty;
            try
            {
                // Get the current subscription data.
                WindowsAzureSubscription subscription = WindowsAzureProfile.Instance.CurrentSubscription;

                // create a temporary context
                ServerDataServiceCertAuth context =
                    ServerDataServiceCertAuth.Create(this.ServerName, subscription);

                clientRequestId = context.ClientRequestId;

                if (databaseName != null)
                {
                    // Retrieve the database with the specified name
                    this.WriteObject(context.GetDatabase(databaseName));
                }
                else
                {
                    // No name specified, retrieve all databases in the server
                    this.WriteObject(context.GetDatabases());
                }
            }
            catch (Exception ex)
            {
                SqlDatabaseExceptionHandler.WriteErrorDetails(
                    this,
                    clientRequestId,
                    ex);
            }
        }

        /// <summary>
        /// Process the request using the provided connection context
        /// </summary>
        /// <param name="databaseName">the name of the database to retrieve</param>
        private void ProcessWithConnectionContext(string databaseName)
        {
            try
            {
                if (databaseName != null)
                {
                    // Retrieve the database with the specified name
                    this.WriteObject(this.ConnectionContext.GetDatabase(databaseName));
                }
                else
                {
                    // No name specified, retrieve all databases in the server
                    this.WriteObject(this.ConnectionContext.GetDatabases(), true);
                }
            }
            catch (Exception ex)
            {
                SqlDatabaseExceptionHandler.WriteErrorDetails(
                    this,
                    this.ConnectionContext.ClientRequestId,
                    ex);
            }
        }
    }
}
