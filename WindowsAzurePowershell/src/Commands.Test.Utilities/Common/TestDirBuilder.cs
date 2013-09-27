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

namespace Microsoft.WindowsAzure.Commands.Test.Utilities.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Helper class to create a test directory and put files
    /// in it.
    /// </summary>
    public class TestDirBuilder
    {
        private readonly string directoryName;
        private readonly List<string> fileNames = new List<string>();
        private readonly List<string> filePaths = new List<string>();

        public TestDirBuilder(string directoryName)
        {
            this.directoryName = directoryName;
            Directory.CreateDirectory(directoryName);
        }

        public TestDirBuilder AddFile(string sourceFileName, string destFileName)
        {
            string filePath = Path.Combine(directoryName, destFileName);
            File.WriteAllText(filePath, File.ReadAllText(sourceFileName));
            fileNames.Add(destFileName);
            filePaths.Add(filePath);
            return this;
        }

        public IDisposable Pushd()
        {
            return new DirStack(directoryName);
        }

        public string DirectoryName
        {
            get { return directoryName; }
        }

        public IList<string> FileNames
        {
            get { return fileNames; }
        }

        public IList<string> FilePaths
        {
            get { return filePaths; }
        }
    }
}
