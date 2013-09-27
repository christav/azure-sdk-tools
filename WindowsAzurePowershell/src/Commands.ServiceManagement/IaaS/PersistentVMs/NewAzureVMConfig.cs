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


namespace Microsoft.WindowsAzure.Commands.ServiceManagement.IaaS
{
    using System;
    using System.Collections.ObjectModel;
    using System.Management.Automation;
    using Commands.Utilities.Common;
    using WindowsAzure.ServiceManagement;
    using Model;
    using Properties;

    [Cmdlet(VerbsCommon.New, "AzureVMConfig", DefaultParameterSetName = "ImageName"), OutputType(typeof(PersistentVM))]
    public class NewAzureVMConfigCommand : PSCmdlet
    {
        private const string RoleType = "PersistentVMRole";

        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The virtual machine name.")]
        [ValidateNotNullOrEmpty]
        public string Name
        {
            get;
            set;
        }

        [Parameter(Position = 1, Mandatory = true, HelpMessage = "Represents the size of the machine.")]
        [ValidateSet("ExtraSmall", "Small", "Medium", "Large", "ExtraLarge", "A6", "A7", IgnoreCase = true)]
        public string InstanceSize
        {
            get;
            set;
        }

        [Parameter(Position = 2, Mandatory = true, ParameterSetName = "ImageName", HelpMessage = "Reference to a platform stock image or a user image from the image repository.")]
        [ValidateNotNullOrEmpty]
        public string ImageName
        {
            get;
            set;
        }

        [Parameter(Position = 2, Mandatory = false, ParameterSetName = "ImageName", HelpMessage = "Location of the where the VHD should be created. This link refers to a blob in a storage account. If not specified the VHD will be created in the default storage account with the following format :vhds/servicename-vmname-year-month-day-ms")]
        [ValidateNotNullOrEmpty]
        public string MediaLocation
        {
            get;
            set;
        }

        [Parameter(Position = 3, Mandatory = false, ParameterSetName = "ImageName", HelpMessage = "Label of the new disk to be created.")]
        [ValidateNotNullOrEmpty]
        public string DiskLabel
        {
            get;
            set;
        }

        [Parameter(Position = 2, Mandatory = true, ParameterSetName = "DiskName", HelpMessage = "Friendly name of the OS disk in the disk repository.")]
        [ValidateNotNullOrEmpty]
        public string DiskName
        {
            get;
            set;
        }

        [Parameter(HelpMessage = "Controls the platform caching behavior of the OS disk.")]
        [ValidateSet("ReadWrite", "ReadOnly", IgnoreCase = true)]
        public string HostCaching
        {
            get;
            set;
        }

        [Parameter(HelpMessage = "The name of the availability set.")]
        [ValidateNotNullOrEmpty]
        public string AvailabilitySetName
        {
            get;
            set;
        }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The VM label.")]
        [ValidateNotNullOrEmpty]
        public string Label
        {
            get;
            set;
        }

        internal void ExecuteCommand()
        {
            ValidateParameters();

            if (string.IsNullOrEmpty(Label))
            {
                Label = Name;
            }

            Label = Label;

            var role = new PersistentVM
            {
                AvailabilitySetName = AvailabilitySetName,
                ConfigurationSets = new Collection<ConfigurationSet>(),
                DataVirtualHardDisks = new Collection<DataVirtualHardDisk>(),
                RoleName = Name,
                RoleSize = InstanceSize,
                RoleType = RoleType,
                Label = Label
            };

            role.OSVirtualHardDisk = new OSVirtualHardDisk()
            {
                DiskName = DiskName,
                SourceImageName = ImageName,
                MediaLink = string.IsNullOrEmpty(MediaLocation) ? null : new Uri(MediaLocation),
                HostCaching = HostCaching,
                DiskLabel = string.IsNullOrEmpty(DiskLabel) ? null : DiskLabel
            };

            WriteObject(role, true);
        }

        protected override void ProcessRecord()
        {
            try
            {
                base.ProcessRecord();
                ExecuteCommand();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, string.Empty, ErrorCategory.CloseError, null));
            }
        }

        protected void ValidateParameters()
        {
            WindowsAzureSubscription currentSubscription = WindowsAzureProfile.Instance.CurrentSubscription;
            if ((currentSubscription == null || currentSubscription.CurrentStorageAccountName == null) && MediaLocation == null)
            {
                throw new ArgumentException(Resources.MustSpecifyMediaLocationOrHaveCurrentStorageAccount);
            }
        }
    }
}