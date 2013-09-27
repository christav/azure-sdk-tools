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
    using CloudService;
    using Newtonsoft.Json;
    using Properties;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Permissions;
    using System.ServiceModel.Channels;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using XmlSchema.ServiceConfigurationSchema;
    using JsonFormatting = Newtonsoft.Json.Formatting;
    using System.Net.Http;

    public static class General
    {
        private static Assembly _assembly = Assembly.GetExecutingAssembly();

        private static bool TryFindCertificatesInStore(string thumbprint,
            System.Security.Cryptography.X509Certificates.StoreLocation location, out X509Certificate2Collection certificates)
        {
            X509Store store = new X509Store(StoreName.My, location);
            store.Open(OpenFlags.ReadOnly);
            certificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
            store.Close();

            return certificates != null && certificates.Count > 0;
        }

        private static string TryGetEnvironmentVariable(string environmentVariableName, string defaultValue)
        {
            string value = Environment.GetEnvironmentVariable(environmentVariableName);
            return (string.IsNullOrEmpty(value)) ? defaultValue : value;
        }

        public static string GetNodeModulesPath()
        {
            return Path.Combine(GetAssemblyDirectory(), Resources.NodeModulesPath);
        }

        public static string GetAssemblyDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static string GetWithProgramFilesPath(string directoryName, bool throwIfNotFound)
        {
            string programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (Directory.Exists(Path.Combine(programFilesPath, directoryName)))
            {
                return Path.Combine(programFilesPath, directoryName);
            }
            else
            {
                if (programFilesPath.IndexOf(Resources.x86InProgramFiles) == -1)
                {
                    programFilesPath += Resources.x86InProgramFiles;
                    if (throwIfNotFound)
                    {
                        Validate.ValidateDirectoryExists(Path.Combine(programFilesPath, directoryName));
                    }
                    return Path.Combine(programFilesPath, directoryName);
                }
                else
                {
                    programFilesPath = programFilesPath.Replace(Resources.x86InProgramFiles, string.Empty);
                    if (throwIfNotFound)
                    {
                        Validate.ValidateDirectoryExists(Path.Combine(programFilesPath, directoryName));
                    }
                    return Path.Combine(programFilesPath, directoryName);
                }
            }
        }

        public static T DeserializeXmlFile<T>(string fileName, string exceptionMessage = null)
        {
            // TODO: fix and uncomment. second parameter is wrong
            // Validate.ValidateFileFull(fileName, string.Format(Resources.PathDoesNotExistForElement, string.Empty, fileName));

            T item = default(T);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (TextReader reader = new StreamReader(fileName, true))
            {
                try { item = (T)xmlSerializer.Deserialize(reader); }
                catch
                {
                    if (!string.IsNullOrEmpty(exceptionMessage))
                    {
                        throw new InvalidOperationException(exceptionMessage);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return item;
        }

        public static void SerializeXmlFile<T>(T obj, string fileName)
        {
            Validate.ValidatePathName(fileName, string.Format(Resources.PathDoesNotExistForElement, string.Empty, fileName));
            Validate.ValidateStringIsNullOrEmpty(fileName, string.Empty);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            Encoding encoding = GetFileEncoding(fileName);
            using (TextWriter writer = new StreamWriter(new FileStream(fileName, FileMode.Create), encoding))
            {
                xmlSerializer.Serialize(writer, obj);
            }
        }

        public static T DeserializeXmlStream<T>(Stream stream)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            T obj = (T)xmlSerializer.Deserialize(stream);
            stream.Close();

            return obj;
        }

        public static T DeserializeXmlString<T>(string contents)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            T obj;

            using (StringReader reader = new StringReader(contents))
            {
                obj = (T)xmlSerializer.Deserialize(reader);
            }

            return obj;
        }

        public static byte[] GetResourceContents(string resourceName)
        {
            Stream stream = _assembly.GetManifestResourceStream(resourceName);
            byte[] contents = new byte[stream.Length];
            stream.Read(contents, (int)stream.Position, (int)stream.Length);
            return contents;
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public static void LaunchWebPage(string target)
        {
            ProcessHelper.Start(target);
        }

        public static void CreateDirectories(IEnumerable<string> directories)
        {
            foreach (string directoryName in directories)
            {
                Directory.CreateDirectory(directoryName);
            }
        }

        public static void CopyFilesFromResources(IDictionary<string, string> filesPair)
        {
            foreach (KeyValuePair<string, string> filePair in filesPair)
            {
                File.WriteAllBytes(filePair.Value, General.GetResourceContents(filePair.Key));
            }
        }

        /// <summary>
        /// Write all of the given bytes to a file.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <param name="mode">Mode to open the file.</param>
        /// <param name="bytes">Contents of the file.</param>
        public static void WriteAllBytes(string path, FileMode mode, byte[] bytes)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(Enum.IsDefined(typeof(FileMode), mode));
            Debug.Assert(bytes != null && bytes.Length > 0);

            // Note: We're not wrapping the file in a using statement because
            // that could lead to a double dispose when the writer is disposed.
            FileStream file = null;
            try
            {
                file = new FileStream(path, mode);
                using (BinaryWriter writer = new BinaryWriter(file))
                {
                    // Clear the reference to file so it won't get disposed twice
                    file = null;

                    writer.Write(bytes);
                }
            }
            finally
            {
                if (file != null)
                {
                    file.Dispose();
                }
            }
        }

        public static int GetRandomFromTwo(int first, int second)
        {
            return (new Random(DateTime.Now.Millisecond).Next(2) == 0) ? first : second;
        }

        public static string[] GetResourceNames(string resourcesFullFolderName)
        {
            return _assembly.GetManifestResourceNames().Where<string>(item => item.StartsWith(resourcesFullFolderName)).ToArray<string>();
        }

        public static TResult InvokeMethod<T, TResult>(string methodName, object instance, params object[] arguments)
        {
            MethodInfo info = typeof(T).GetMethod(methodName);
            return (TResult)info.Invoke(instance, arguments);
        }

        public static X509Certificate2 GetCertificateFromStore(string thumbprint)
        {
            Validate.ValidateStringIsNullOrEmpty(thumbprint, "certificate thumbprint");
            X509Certificate2Collection certificates;
            if (TryFindCertificatesInStore(thumbprint, StoreLocation.CurrentUser, out certificates) ||
                TryFindCertificatesInStore(thumbprint, StoreLocation.LocalMachine, out certificates))
            {
                return certificates[0];
            }
            else
            {
                throw new ArgumentException(string.Format(Resources.CertificateNotFoundInStore, thumbprint));
            }
        }

        public static void AddCertificateToStore(X509Certificate2 certificate)
        {
            Validate.ValidateNullArgument(certificate, Resources.InvalidCertificate);
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate);
            store.Close();
        }

        public static void RemoveCertificateFromStore(X509Certificate2 certificate)
        {
            if (certificate != null)
            {
                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                store.Remove(certificate);
                store.Close();
            }
        }

        /// <summary>
        /// Gets the value of blob endpoint uri from environment if set, otherwise returns the default value.
        /// </summary>
        /// <param name="accountName">The storage account name</param>
        /// <returns>The full blob endpoint uri including the storage account name</returns>
        public static string BlobEndpointUri(string accountName)
        {
            return string.Format(CultureInfo.InvariantCulture,
                TryGetEnvironmentVariable(Resources.BlobEndpointUriEnv, Resources.BlobEndpointUri),
                accountName);
        }

        /// <summary>
        /// Convert the given array into string.
        /// </summary>
        /// <typeparam name="T">The type of the object array is holding</typeparam>
        /// <param name="array">The collection</param>
        /// <param name="delimiter">The used delimiter between array elements</param>
        /// <returns>The array into string representation</returns>
        public static string ArrayToString<T>(this T[] array, string delimiter)
        {
            return (array == null) ? null : (array.Length == 0) ? string.Empty : array.Skip(1).Aggregate(new StringBuilder(array[0].ToString()), (s, i) => s.Append(delimiter).Append(i), s => s.ToString());
        }

        /// <summary>
        /// Copies a directory.
        /// </summary>
        /// <param name="sourceDirName">The source directory name</param>
        /// <param name="destDirName">The destination directory name</param>
        /// <param name="copySubDirs">Should the copy be recursive</param>
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(string.Format(Resources.PathDoesNotExist, sourceDirName));
            }

            Directory.CreateDirectory(destDirName);

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        /// <summary>
        /// Ensures that a directory exists beofre attempting to write a file
        /// </summary>
        /// <param name="pathName">The path to the file that will be created</param>
        public static void EnsureDirectoryExists(string pathName)
        {
            Validate.ValidateStringIsNullOrEmpty(pathName, "Settings directory");
            string directoryPath = Path.GetDirectoryName(pathName);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// Compares two strings with handling special case that base string can be empty.
        /// </summary>
        /// <param name="leftHandSide">The base string.</param>
        /// <param name="rightHandSide">The comparer string.</param>
        /// <returns>True if equals or leftHandSide is null/empty, false otherwise.</returns>
        public static bool TryEquals(string leftHandSide, string rightHandSide)
        {
            if (string.IsNullOrEmpty(leftHandSide) ||
                leftHandSide.Equals(rightHandSide, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static string ReadMessageBody(ref Message originalMessage)
        {
            StringBuilder strBuilder = new StringBuilder();

            using (MessageBuffer messageBuffer = originalMessage.CreateBufferedCopy(int.MaxValue))
            {
                Message message = messageBuffer.CreateMessage();
                XmlWriter writer = XmlWriter.Create(strBuilder);
                using (XmlDictionaryWriter dictionaryWriter = XmlDictionaryWriter.CreateDictionaryWriter(writer))
                {
                    message.WriteBodyContents(dictionaryWriter);
                }

                originalMessage = messageBuffer.CreateMessage();
            }

            return Beautify(strBuilder.ToString());
        }

        /// <summary>
        /// Formats given string into well formatted XML.
        /// </summary>
        /// <param name="unformattedXml">The unformatted xml string</param>
        /// <returns>The formatted XML string</returns>
        public static string Beautify(string unformattedXml)
        {
            string formattedXml = string.Empty;
            if (!string.IsNullOrEmpty(unformattedXml))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(unformattedXml);
                StringBuilder stringBuilder = new StringBuilder();
                XmlWriterSettings settings = new XmlWriterSettings()
                {
                    Indent = true,
                    IndentChars = "\t",
                    NewLineChars = Environment.NewLine,
                    NewLineHandling = NewLineHandling.Replace
                };
                using (XmlWriter writer = XmlWriter.Create(stringBuilder, settings))
                {
                    doc.Save(writer);
                }
                formattedXml = stringBuilder.ToString();
            }

            return formattedXml;
        }

        public static string GetConfiguration(string configurationPath)
        {
            var configuration = string.Join(string.Empty, File.ReadAllLines(configurationPath));
            return configuration;
        }

        /// <summary>
        /// Create a unique temp directory.
        /// </summary>
        /// <returns>Path to the temp directory.</returns>
        public static string CreateTempDirectory()
        {
            string tempPath = null;
            do
            {
                tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            }
            while (Directory.Exists(tempPath) || File.Exists(tempPath));

            Directory.CreateDirectory(tempPath);
            return tempPath;
        }

        /// <summary>
        /// Copy a directory from one path to another.
        /// </summary>
        /// <param name="sourceDirectory">Source directory.</param>
        /// <param name="destinationDirectory">Destination directory.</param>
        public static void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            Debug.Assert(!string.IsNullOrEmpty(sourceDirectory), "sourceDictory cannot be null or empty!");
            Debug.Assert(Directory.Exists(sourceDirectory), "sourceDirectory must exist!");
            Debug.Assert(!string.IsNullOrEmpty(destinationDirectory), "destinationDirectory cannot be null or empty!");
            Debug.Assert(!Directory.Exists(destinationDirectory), "destinationDirectory must not exist!");

            foreach (string file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(
                    sourceDirectory.Length + 1,
                    file.Length - sourceDirectory.Length - 1);
                string destinationPath = Path.Combine(destinationDirectory, relativePath);

                string destinationDir = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                File.Copy(file, destinationPath);
            }
        }

        /// <summary>
        /// Get the value for a given key in a dictionary or return a default
        /// value if the key isn't present in the dictionary.
        /// </summary>
        /// <typeparam name="K">The type of the key.</typeparam>
        /// <typeparam name="V">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">A default value</param>
        /// <returns>The corresponding value or default value.</returns>
        public static V GetValueOrDefault<K, V>(this IDictionary<K, V> dictionary, K key, V defaultValue)
        {
            Debug.Assert(dictionary != null, "dictionary cannot be null!");

            V value;
            if (!dictionary.TryGetValue(key, out value))
            {
                value = defaultValue;
            }
            return value;
        }

        /// <summary>
        /// Returns a non-null sequence by either passing back the original
        /// sequence or creating a new empty sequence if the original was null.
        /// </summary>
        /// <typeparam name="T">Type of elements in the sequence.</typeparam>
        /// <param name="sequence">The sequence.</param>
        /// <returns>A non-null sequence.</returns>
        public static IEnumerable<T> NonNull<T>(this IEnumerable<T> sequence)
        {
            return (sequence != null) ?
                sequence :
                Enumerable.Empty<T>();
        }

        /// <summary>
        /// Perform an action on each element of a sequence.
        /// </summary>
        /// <typeparam name="T">Type of elements in the sequence.</typeparam>
        /// <param name="sequence">The sequence.</param>
        /// <param name="action">The action to perform.</param>
        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            Debug.Assert(sequence != null, "sequence cannot be null!");
            Debug.Assert(action != null, "action cannot be null!");

            foreach (T element in sequence)
            {
                action(element);
            }
        }

        /// <summary>
        /// Append an element to the end of an array.
        /// </summary>
        /// <typeparam name="T">Type of the arrays.</typeparam>
        /// <param name="left">The left array.</param>
        /// <param name="right">The right array.</param>
        /// <returns>The concatenated arrays.</returns>
        public static T[] Append<T>(T[] left, T right)
        {
            if (left == null)
            {
                return right != null ?
                    new T[] { right } :
                    new T[] { };
            }
            else if (right == null)
            {
                return left;
            }
            else
            {
                return Enumerable.Concat(left, new T[] { right }).ToArray();
            }
        }

        public static TResult MaxOrDefault<T, TResult>(this IEnumerable<T> sequence, Func<T, TResult> selector, TResult defaultValue)
        {
            return (sequence != null) ? sequence.Max(selector) : defaultValue;
        }

        /// <summary>
        /// Extends the array with one element.
        /// </summary>
        /// <typeparam name="T">The array type</typeparam>
        /// <param name="collection">The array holding elements</param>
        /// <param name="item">The item to add</param>
        /// <returns>New array with added item</returns>
        public static T[] ExtendArray<T>(IEnumerable<T> collection, T item)
        {
            if (collection == null)
            {
                collection = new T[0];
            }

            List<T> list = new List<T>(collection);
            list.Add(item);
            return list.ToArray<T>();
        }

        /// <summary>
        /// Extends the array with another array
        /// </summary>
        /// <typeparam name="T">The array type</typeparam>
        /// <param name="collection">The array holding elements</param>
        /// <param name="items">The items to add</param>
        /// <returns>New array with added items</returns>
        public static T[] ExtendArray<T>(IEnumerable<T> collection, IEnumerable<T> items)
        {
            if (collection == null)
            {
                collection = new T[0];
            }

            if (items == null)
            {
                items = new T[0];
            }

            return collection.Concat<T>(items).ToArray<T>();
        }

        /// <summary>
        /// Initializes given object if its set to null.
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <param name="obj">The object to initialize</param>
        /// <returns>Initialized object</returns>
        public static T InitializeIfNull<T>(T obj)
            where T : new()
        {
            if (obj == null)
            {
                return new T();
            }

            return obj;
        }

        public static ServiceSettings GetDefaultSettings(
            string rootPath,
            string inServiceName,
            string slot,
            string location,
            string affinityGroup,
            string storageName,
            string subscription,
            out string serviceName)
        {
            ServiceSettings serviceSettings;

            if (string.IsNullOrEmpty(rootPath))
            {
                serviceSettings = ServiceSettings.LoadDefault(null, slot, location, affinityGroup, subscription, storageName, inServiceName, null, out serviceName);
            }
            else
            {
                serviceSettings = ServiceSettings.LoadDefault(
                    new CloudServiceProject(rootPath, null).Paths.Settings,
                    slot,
                    location,
                    affinityGroup,
                    subscription,
                    storageName,
                    inServiceName,
                    new CloudServiceProject(rootPath, null).ServiceName,
                    out serviceName);
            }

            return serviceSettings;
        }

        /// <summary>
        /// Gets role name for the current pathif exists.
        /// </summary>
        /// <returns>The role name</returns>
        public static string GetRoleName(string rootPath, string currentPath)
        {
            bool found = false;
            string roleName = null;

            if (!(rootPath.Length >= currentPath.Length))
            {
                string difference = currentPath.Replace(rootPath, string.Empty);
                roleName = difference.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).GetValue(0).ToString();
                CloudServiceProject service = new CloudServiceProject(rootPath, null);
                found = service.Components.RoleExists(roleName);
            }

            if (!found)
            {
                throw new ArgumentException(string.Format(Resources.CannotFindRole, currentPath));
            }

            return roleName;
        }

        /// <summary>
        /// Tries to get service path, if not return null.
        /// </summary>
        /// <param name="currentPath">The current path</param>
        /// <returns>The service root path</returns>
        public static string TryGetServiceRootPath(string currentPath)
        {
            try { return GetServiceRootPath(currentPath); }
            catch (Exception) { return null; }
        }

        public static string GetServiceRootPath(string currentPath)
        {
            // Get the service path
            string servicePath = FindServiceRootDirectory(currentPath);

            // Was the service path found?
            if (servicePath == null)
            {
                throw new InvalidOperationException(Resources.CannotFindServiceRoot);
            }

            CloudServiceProject service = new CloudServiceProject(servicePath, null);
            if (service.Components.CloudConfig.Role != null)
            {
                foreach (RoleSettings role in service.Components.CloudConfig.Role)
                {
                    string roleDirectory = Path.Combine(service.Paths.RootPath, role.name);

                    if (!Directory.Exists(roleDirectory))
                    {
                        throw new InvalidOperationException(Resources.CannotFindServiceRoot);
                    }
                }
            }

            return servicePath;
        }

        private static string FindServiceRootDirectory(string path)
        {
            // Is the csdef file present in the folder
            bool found = Directory.GetFiles(path, Resources.ServiceDefinitionFileName).Length == 1;

            if (found)
            {
                return path;
            }

            // Find the last slash
            int slash = path.LastIndexOf('\\');
            if (slash > 0)
            {
                // Slash found trim off the last path
                path = path.Substring(0, slash);

                // Recurse
                return FindServiceRootDirectory(path);
            }

            // Couldn't locate the service root, exit
            return null;
        }

        public static string EnsureTrailingSlash(string url)
        {
            UriBuilder address = new UriBuilder(url);
            if (!address.Path.EndsWith("/", StringComparison.Ordinal))
            {
                address.Path += "/";
            }
            return address.Uri.AbsoluteUri;
        }

        public static string GetHttpResponseLog(string statusCode, WebHeaderCollection headers, string body)
        {
            StringBuilder httpResponseLog = new StringBuilder();
            httpResponseLog.AppendLine(string.Format("============================ HTTP RESPONSE ============================{0}", Environment.NewLine));
            httpResponseLog.AppendLine(string.Format("Status Code:{0}{1}{0}", Environment.NewLine, statusCode));
            httpResponseLog.AppendLine(string.Format("Headers:{0}{1}", Environment.NewLine, MessageHeadersToString(headers)));
            httpResponseLog.AppendLine(string.Format("Body:{0}{1}{0}", Environment.NewLine, body));

            return httpResponseLog.ToString();
        }

        public static string GetHttpResponseLog(string statusCode, HttpHeaders headers, string body)
        {
            return GetHttpResponseLog(statusCode, ConvertHttpHeadersToWebHeaderCollection(headers), body);
        }

        public static string GetHttpRequestLog(
            string method,
            string requestUri,
            WebHeaderCollection headers,
            string body)
        {
            StringBuilder httpRequestLog = new StringBuilder();
            httpRequestLog.AppendLine(string.Format("============================ HTTP REQUEST ============================{0}", Environment.NewLine));
            httpRequestLog.AppendLine(string.Format("HTTP Method:{0}{1}{0}", Environment.NewLine, method));
            httpRequestLog.AppendLine(string.Format("Absolute Uri:{0}{1}{0}", Environment.NewLine, requestUri));
            httpRequestLog.AppendLine(string.Format("Headers:{0}{1}", Environment.NewLine, MessageHeadersToString(headers)));
            httpRequestLog.AppendLine(string.Format("Body:{0}{1}{0}", Environment.NewLine, body));

            return httpRequestLog.ToString();
        }

        public static string GetHttpRequestLog(string method, string requestUri, HttpHeaders headers, string body)
        {
            return GetHttpRequestLog(method, requestUri, ConvertHttpHeadersToWebHeaderCollection(headers), body);
        }

        public static string GetLog(HttpResponseMessage response)
        {
            string body = response.Content == null ? string.Empty : FormatString(response.Content.ReadAsStringAsync().Result);

            return GetHttpResponseLog(
                response.StatusCode.ToString(),
                response.Headers,
                body);
        }

        public static string GetLog(HttpRequestMessage request)
        {
            string body = request.Content == null ? string.Empty : FormatString(request.Content.ReadAsStringAsync().Result);

            return GetHttpRequestLog(
                request.Method.ToString(),
                request.RequestUri.ToString(),
                (HttpHeaders)request.Headers,
                body);
        }

        public static string FormatString(string content)
        {
            if (IsXml(content))
            {
                return TryFormatXml(content);
            }
            else if (IsJson(content))
            {
                return General.TryFormatJson(content);
            }
            else
            {
                return content;
            }
        }

        private static WebHeaderCollection ConvertHttpHeadersToWebHeaderCollection(HttpHeaders headers)
        {
            WebHeaderCollection webHeaders = new WebHeaderCollection();
            foreach (KeyValuePair<string, IEnumerable<string>> pair in headers)
            {
                pair.Value.ForEach<string>(v => webHeaders.Add(pair.Key, v));
            }

            return webHeaders;
        }

        private static string MessageHeadersToString(WebHeaderCollection headers)
        {
            string[] keys = headers.AllKeys;
            StringBuilder result = new StringBuilder();

            foreach (string key in keys)
            {
                result.AppendLine(string.Format(
                    "{0,-30}: {1}",
                    key,
                    General.ArrayToString<string>(headers.GetValues(key), ",")));
            }

            return result.ToString();
        }

        public static Encoding GetFileEncoding(string path)
        {
            Encoding encoding;


            if (File.Exists(path))
            {
                using (StreamReader r = new StreamReader(path, true))
                {
                    encoding = r.CurrentEncoding;
                }
            }
            else
            {
                encoding = Encoding.Default;
            }

            return encoding;
        }

        /// <summary>
        /// Creates https endpoint from the given endpoint.
        /// </summary>
        /// <param name="endpointUri">The endpoint uri.</param>
        /// <returns>The https endpoint uri.</returns>
        public static Uri CreateHttpsEndpoint(string endpointUri)
        {
            UriBuilder builder = new UriBuilder(endpointUri) { Scheme = "https" };
            string endpoint = builder.Uri.GetComponents(
                UriComponents.AbsoluteUri & ~UriComponents.Port,
                UriFormat.UriEscaped);

            return new Uri(endpoint);
        }

        /// <summary>
        /// Formats the given XML into indented way.
        /// </summary>
        /// <param name="content">The input xml string</param>
        /// <returns>The formatted xml string</returns>
        public static string TryFormatXml(string content)
        {
            try
            {
                XDocument doc = XDocument.Parse(content);
                return doc.ToString();
            }
            catch (Exception)
            {
                return content;
            }
        }

        /// <summary>
        /// Checks if the content is valid XML or not.
        /// </summary>
        /// <param name="content">The text to check</param>
        /// <returns>True if XML, false otherwise</returns>
        public static bool IsXml(string content)
        {
            try
            {
                XDocument doc = XDocument.Parse(content);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Handling the failure by returning the original string.")]
        public static string TryFormatJson(string str)
        {
            try
            {
                object parsedJson = JsonConvert.DeserializeObject(str);
                return JsonConvert.SerializeObject(parsedJson, JsonFormatting.Indented);
            }
            catch
            {
                // can't parse JSON, return the original string
                return str;
            }
        }

        public static bool IsJson(string content)
        {
            content = content.Trim();
            return content.StartsWith("{") && content.EndsWith("}")
                   || content.StartsWith("[") && content.EndsWith("]");
        }

        public static string GetNonEmptyValue(string oldValue, string newValue)
        {
            return string.IsNullOrEmpty(newValue) ? oldValue : newValue;
        }

        public static string CombinePath(params string[] paths)
        {
            return Path.Combine(paths);
        }
    }
}