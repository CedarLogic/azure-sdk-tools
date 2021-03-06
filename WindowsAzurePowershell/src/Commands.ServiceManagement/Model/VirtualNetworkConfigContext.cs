﻿// ----------------------------------------------------------------------------------
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

namespace Microsoft.WindowsAzure.Commands.ServiceManagement.Model
{
    using System;
    using System.IO;
    using Utilities.Common;
    using Properties;

    public class VirtualNetworkConfigContext : ManagementOperationContext
    {
        public string XMLConfiguration { get; set; }

        public void ExportToFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath", Resources.VirtualNetworkConfigContextMissingFilePath);
            }

            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                throw new ArgumentException(Resources.VirtualNetworkConfigContextDirectoryDoesNotExist, "filePath");
            }

            using (StreamWriter outfile = new StreamWriter(filePath))
            {
                outfile.Write(this.XMLConfiguration);
            }
        }
    }
}
