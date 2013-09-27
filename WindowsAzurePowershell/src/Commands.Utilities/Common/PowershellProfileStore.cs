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

namespace Microsoft.WindowsAzure.Commands.Utilities.Common
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Xml;

    /// <summary>
    /// This class provides the serialization for the
    /// WindowsAzureProfile information to and from
    /// the Powershell credentials directory.
    /// </summary>
    public class PowershellProfileStore : IProfileStore
    {
        private string settingsDirectory;
        private string profileFileName;

        public const string DefaultProfileName = "azureProfile.xml";

        private string FullProfilePath
        {
            get { return Path.Combine(settingsDirectory, profileFileName); }
        }

        /// <summary>
        /// Create a new instance of <see cref="PowershellProfileStore"/>
        /// using the given directory and filename.
        /// </summary>
        /// <param name="settingsDirectory">Directory to store / load information from.
        /// If null or blank, uses the current directory.</param>
        /// <param name="fileName">Filename to read from / write to. If null or
        /// blank, uses the default azureProfile.xml file.</param>
        public PowershellProfileStore(string settingsDirectory, string fileName)
        {
            if (string.IsNullOrEmpty(settingsDirectory))
            {
                settingsDirectory = Directory.GetCurrentDirectory();
            }
            this.settingsDirectory = settingsDirectory;

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = DefaultProfileName;
            }
            this.profileFileName = fileName;
            EnsureSettingsDirectoryExists();
            EnsureProfileFileExists();
        }

        /// <summary>
        /// Create an instance of <see cref="PowershellProfileStore"/>
        /// that uses the default powershell file and directory.
        /// </summary>
        public PowershellProfileStore()
            : this(GlobalPathInfo.GlobalSettingsDirectory, DefaultProfileName)
        {
            
        }

        /// <summary>
        /// Save the given profile data to the store.
        /// </summary>
        /// <param name="profile">Data to store.</param>
        public void Save(ProfileData profile)
        {
            string tempFilePath;
            using (var s = CreateTempFile(out tempFilePath))
            {
                var serializer = new DataContractSerializer(typeof (ProfileData));
                serializer.WriteObject(s, profile);
            }
            File.Replace(tempFilePath, FullProfilePath, null);
        }

        /// <summary>
        /// Load from the store.
        /// </summary>
        /// <returns>The loaded data.</returns>
        public ProfileData Load()
        {
            if (File.Exists(FullProfilePath))
            {
                try
                {
                    var serializer = new DataContractSerializer(typeof (ProfileData));
                    using (var s = new FileStream(FullProfilePath, FileMode.Open,
                        FileAccess.Read, FileShare.Read))
                    {
                        return (ProfileData)serializer.ReadObject(s);
                    }
                }
                catch (XmlException)
                {
                    // XML is malformed or file is empty. Treat as no profile.
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Destroy the store and it's backing data.
        /// </summary>
        public void DestroyData()
        {
            Directory.Delete(settingsDirectory, true);
        }

        private void EnsureSettingsDirectoryExists()
        {
            if (!Directory.Exists(settingsDirectory))
            {
                Directory.CreateDirectory(settingsDirectory);
            }
        }

        private void EnsureProfileFileExists()
        {
            if (!File.Exists(FullProfilePath))
            {
                try
                {
                    File.Create(FullProfilePath).Close();
                }
                catch (IOException)
                {
                    // If we got this, then the file was created
                    // between the exists check and the create.
                    // That's fine, the file's there, that's the
                    // important thing.
                }
            }
        }

        private FileStream CreateTempFile(out string finalFileName)
        {
            do
            {
                try
                {
                    string fileName = FullProfilePath + "." + Guid.NewGuid().ToString();
                    var stream = new FileStream(fileName, FileMode.CreateNew, FileAccess.Write);
                    finalFileName = fileName;
                    return stream;
                }
                catch (IOException)
                {
                    // If we got this, the file already existed. Try again.
                }
            } while (true);
        }
    }
}