﻿// ----------------------------------------------------------------------------------
//
// Copyright 2012 Microsoft Corporation
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

namespace Microsoft.WindowsAzure.Commands.Storage.Test.Service
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Model.Contract;

    /// <summary>
    /// Mocked table management
    /// </summary>
    public class MockStorageTableManagement : IStorageTableManagement
    {
        /// <summary>
        /// Exists table lists
        /// </summary>
        public List<CloudTable> tableList = new List<CloudTable>();

        /// <summary>
        /// Table end point
        /// </summary>
        private string TableEndPoint = "http://127.0.0.1/account/";

        /// <summary>
        /// List azure storage tables
        /// </summary>
        /// <param name="prefix">Table name prefix</param>
        /// <param name="requestOptions">Table request options</param>
        /// <param name="operationContext">Operation context</param>
        /// <returns>An enumerable collection of tables that begin with the specified prefix</returns>
        public IEnumerable<CloudTable> ListTables(string prefix, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            if (String.IsNullOrEmpty(prefix))
            {
                return tableList;
            }
            else
            {
                List<CloudTable> prefixTables = new List<CloudTable>();

                foreach (CloudTable table in tableList)
                {
                    if (table.Name.ToLower().StartsWith(prefix.ToLower()))
                    {
                        prefixTables.Add(table);
                    }
                }

                return prefixTables;
            }
        }

        /// <summary>
        /// Get table reference from azure server
        /// </summary>
        /// <param name="name">Table name</param>
        /// <param name="requestOptions">Table request options</param>
        /// <param name="operationContext">Operation context</param>
        /// <returns>A CloudTable object if the specified table exists, otherwise null.</returns>
        public CloudTable GetTableReferenceFromServer(string name, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            foreach (CloudTable table in tableList)
            {
                if (table.Name == name)
                {
                    return table;
                }
            }
            return null;
        }

        /// <summary>
        /// Get a table reference
        /// </summary>
        /// <param name="name">Table name</param>
        /// <returns>Cloud table object</returns>
        public CloudTable GetTableReference(string name)
        {
            Uri tableUri = new Uri(String.Format("{0}{1}", TableEndPoint, name));
            CloudTableClient tableClient = new CloudTableClient(new Uri(TableEndPoint));
            return new CloudTable(tableUri);
        }

        /// <summary>
        /// Cloud a azure storage table if not exists.
        /// </summary>
        /// <param name="table">Cloud table object</param>
        /// <param name="requestOptions">Table request options</param>
        /// <param name="operationContext">Operation context</param>
        /// <returns>True if table was created; otherwise, false.</returns>
        public bool CreateTableIfNotExists(CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            CloudTable tableRef = GetTableReferenceFromServer(table.Name, requestOptions, operationContext);

            if (tableRef != null)
            {
                return false;
            }
            else
            {
                tableRef = GetTableReference(table.Name);
                tableList.Add(tableRef);
                return true;
            }
        }

        /// <summary>
        /// Delete the specified azure storage table
        /// </summary>
        /// <param name="table">Cloud table object</param>
        /// <param name="requestOptions">Table request options</param>
        /// <param name="operationContext">Operation context</param>
        public void Delete(CloudTable table, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            foreach (CloudTable tableRef in tableList)
            {
                if (table.Name == tableRef.Name)
                {
                    tableList.Remove(tableRef);
                    return;
                }
            }
        }

        /// <summary>
        /// Checks whether the table exists.
        /// </summary>
        /// <param name="table">Cloud table object</param>
        /// <param name="requestOptions">Table request options</param>
        /// <param name="operationContext">Operation context</param>
        /// <returns>True if table exists; otherwise, false.</returns>
        public bool DoesTableExist(CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            foreach (CloudTable tableRef in tableList)
            {
                if (table.Name == tableRef.Name)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get table permission
        /// </summary>
        /// <param name="table">CloudTable object</param>
        /// <param name="requestOptions">Table request options</param>
        /// <param name="operationContext">Operation context</param>
        /// <returns>Table permission</returns>
        public TablePermissions GetTablePermissions(CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            throw new NotImplementedException();
        }

        public Model.ResourceModel.AzureStorageContext StorageContext
        {
            get { throw new NotImplementedException(); }
        }
    }
}
