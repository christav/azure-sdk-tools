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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Properties;

    /// <summary>
    /// This class is the entry point for all the persistent
    /// state related to azure connections, including
    /// current environment, subscriptions, etc.
    /// </summary>
    public class WindowsAzureProfile
    {
        // Internal state

        // Store - responsible for loading and saving a profile to a particular location
        private readonly IProfileStore profileStore;

        // Azure environments
        private readonly Dictionary<string, WindowsAzureEnvironment> environments = new Dictionary<string, WindowsAzureEnvironment>(
            WindowsAzureEnvironment.PublicEnvironments, StringComparer.OrdinalIgnoreCase);

        // And subscriptions
        private readonly List<WindowsAzureSubscription> subscriptions = new List<WindowsAzureSubscription>();

        // Func used to create the default instance
        private static readonly Func<WindowsAzureProfile> defaultCreator =
            () => new WindowsAzureProfile(new PowershellProfileStore());

        // Singleton instance management
        private static Lazy<WindowsAzureProfile> instance =
            new Lazy<WindowsAzureProfile>(defaultCreator);


        /// <summary>
        /// Create an instance of <see cref="WindowsAzureProfile"/> that
        /// stores data in the given store.
        /// </summary>
        /// <param name="profileStore">Data store to read and write from.</param>
        public WindowsAzureProfile(IProfileStore profileStore)
        {
            this.profileStore = profileStore;
            Load();
        }

        /// <summary>
        /// The default instance shared across the process.
        /// </summary>
        public static WindowsAzureProfile Instance
        {
            get
            {
                return instance.Value;
            }

            set
            {
                instance = new Lazy<WindowsAzureProfile>(() => value);
            }
        }

        /// <summary>
        /// Reset the default instance, used when the instance has been replaced for testing.
        /// </summary>
        public static void ResetInstance()
        {
            instance = new Lazy<WindowsAzureProfile>(defaultCreator);
        }

        /// <summary>
        /// Destroy any saved data. Deletes the data store and removes any
        /// certificates from the certificate store.
        /// </summary>
        public void DestroyData()
        {
            foreach (var s in subscriptions)
            {
                if (s != null)
                {
                    WindowsAzureCertificate.DeleteFromStore(s.Certificate);
                }
            }
            profileStore.DestroyData();
        }

        //
        // Azure environments
        //

        public IDictionary<string, WindowsAzureEnvironment> Environments
        {
            get { return new Dictionary<string, WindowsAzureEnvironment>(environments, StringComparer.OrdinalIgnoreCase); }
        }

        private WindowsAzureEnvironment currentEnvironment;

        public WindowsAzureEnvironment CurrentEnvironment
        {
            get
            {
                if (currentEnvironment == null)
                {
                    currentEnvironment = environments[EnvironmentName.AzureCloud];
                }
                return currentEnvironment;
            }

            set
            {
                if (!environments.ContainsKey(value.Name))
                {
                    AddEnvironment(value);
                }
                currentEnvironment = environments[value.Name];

                Save();
            }
        }

        public void AddEnvironment(WindowsAzureEnvironment newEnvironment)
        {
            if (environments.ContainsKey(newEnvironment.Name))
            {
                throw new InvalidOperationException(string.Format(Resources.EnvironmentExists, newEnvironment.Name));
            }

            environments[newEnvironment.Name] = newEnvironment;

            Save();
        }

        public void UpdateEnvironment(WindowsAzureEnvironment newEnvironment)
        {
            GuardEnvironmentExistsAndNonPublic(newEnvironment.Name);
            environments[newEnvironment.Name] = newEnvironment;
            Save();
        }

        public void RemoveEnvironment(string name)
        {
            GuardEnvironmentExistsAndNonPublic(name);
            environments.Remove(name);
            Save();
        }

        private void GuardEnvironmentExistsAndNonPublic(string name)
        {
            if (IsPublicEnvironment(name))
            {
                throw new InvalidOperationException(string.Format(Resources.ChangePublicEnvironmentMessage, name));
            }
            if (!environments.ContainsKey(name))
            {
                throw new KeyNotFoundException(string.Format(Resources.EnvironmentNotFound, name));
            }
        }

        //
        // Subscriptions
        //

        public IList<WindowsAzureSubscription> Subscriptions
        {
            get
            {
                return new List<WindowsAzureSubscription>(subscriptions);
            }
        }

        private WindowsAzureSubscription currentSubscription;

        public WindowsAzureSubscription CurrentSubscription
        {
            get
            {
                return currentSubscription ?? DefaultSubscription;
            }

            set { currentSubscription = value; }
        }

        public WindowsAzureSubscription DefaultSubscription
        {
            get { return subscriptions.FirstOrDefault(s => s.IsDefault); }
        }

        public void AddSubscription(WindowsAzureSubscription s)
        {
            if (subscriptions.Contains(s) ||
                subscriptions.Any(es => string.Compare(s.Name, s.Name, StringComparison.OrdinalIgnoreCase) == 0))
            {
                throw new ArgumentException(
                    string.Format(Resources.SubscriptionAlreadyExists, s.Name));
            }

            subscriptions.Add(s);
            if (s.IsDefault)
            {
                UpdateDefaultSubscription(s);
            }

            Save();
        }

        public void RemoveSubscription(WindowsAzureSubscription s)
        {
            if (s == currentSubscription)
            {
                currentSubscription = null;
            }

            subscriptions.Remove(s);

            if (s.IsDefault && subscriptions.Count > 0)
            {
                subscriptions[0].IsDefault = true;
            }

            Save();
        }

        public void UpdateSubscription(WindowsAzureSubscription s)
        {
            if (!subscriptions.Contains(s))
            {
                throw new ArgumentException(
                    string.Format(Resources.CannotUpdateUnknownSubscription, 
                        s.Name, s.SubscriptionId));
            }

            if (s.IsDefault)
            {
                UpdateDefaultSubscription(s);
            }

            Save();
        }

        private void UpdateDefaultSubscription(WindowsAzureSubscription newDefault)
        {
            foreach (var subs in subscriptions.Where(s => s != newDefault))
            {
                subs.IsDefault = false;
            }
        }

        public void ImportPublishSettings(string fileName)
        {
            using (var s = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                ImportPublishSettings(s);
            }
        }

        public void ImportPublishSettings(Stream stream)
        {
            IEnumerable<WindowsAzureSubscription> newSubscriptions = PublishSettingsImporter.Import(stream);

            foreach (var newSubscription in newSubscriptions)
            {
                var existingSubscription =
                    subscriptions.FirstOrDefault(s => s.SubscriptionId == newSubscription.SubscriptionId);
                if (existingSubscription != null)
                {
                    UpdateExistingSubscription(existingSubscription, newSubscription);
                }
                else
                {
                    subscriptions.Add(newSubscription);
                }
            }

            if (DefaultSubscription == null && subscriptions.Count > 0)
            {
                subscriptions[0].IsDefault = true;
            }

            Save();
        }

        private void UpdateExistingSubscription(WindowsAzureSubscription existingSubscription,
            WindowsAzureSubscription newSubscription)
        {
            // For now, just remove old and add new.
            subscriptions.Add(newSubscription);
            subscriptions.Remove(existingSubscription);
            if (existingSubscription.IsDefault)
            {
                newSubscription.IsDefault = true;
            }
            if (currentSubscription == existingSubscription)
            {
                currentSubscription = newSubscription;
            }
        }

        private void Load()
        {
            if (profileStore != null)
            {
                var profileData = profileStore.Load();
                if (profileData != null)
                {
                    LoadEnvironmentData(profileData);
                    LoadSubscriptionData(profileData);
                }
            }
        }

        private void Save()
        {
            var profileData = new ProfileData();
            SetEnvironmentData(profileData);
            SetSubscriptionData(profileData);
            profileStore.Save(profileData);
        }

        private static bool IsPublicEnvironment(string name)
        {
            return
                WindowsAzureEnvironment.PublicEnvironments.Keys.Any(
                    publicName => string.Compare(publicName, name, StringComparison.OrdinalIgnoreCase) == 0);
        }

        private void SetEnvironmentData(ProfileData data)
        {
            data.DefaultEnvironmentName = CurrentEnvironment.Name;
            data.Environments = Environments.Values
                .Where(e => !IsPublicEnvironment(e.Name))
                .Select(e => new AzureEnvironmentData(e));
        }

        private void LoadEnvironmentData(ProfileData data)
        {
            if (data.Environments != null)
            {
                foreach (var e in data.Environments.Select(e => e.ToAzureEnvironment()))
                {
                    environments[e.Name] = e;
                }
                if (environments.ContainsKey(data.DefaultEnvironmentName))
                {
                    currentEnvironment = environments[data.DefaultEnvironmentName];
                }
            }
        }

        private void SetSubscriptionData(ProfileData data)
        {
            data.Subscriptions = Subscriptions.Select(s => new AzureSubscriptionData(s));
        }

        private void LoadSubscriptionData(ProfileData data)
        {
            if (data.Subscriptions != null)
            {
                foreach (var s in data.Subscriptions)
                {
                    subscriptions.Add(s.ToAzureSubscription());
                }
            }
        }

        private void GuardKnownSubscription(WindowsAzureSubscription s)
        {
            if (!subscriptions.Contains(s))
            {
                throw new ArgumentException(
                    string.Format(Resources.CannotUpdateUnknownSubscription, 
                        s.Name,s.SubscriptionId));
            }
        }
    }
}
