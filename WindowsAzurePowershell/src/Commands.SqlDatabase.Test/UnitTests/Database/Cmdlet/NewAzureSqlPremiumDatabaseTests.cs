﻿// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Commands.SqlDatabase.Test.UnitTests.Database.Cmdlet
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Management.Automation;
    using Commands.Utilities.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Commands.Test.Utilities.Common;
    using MockServer;
    using Services;
    using Services.Server;
    using SqlDatabase.Database.Cmdlet;

    [TestClass]
    public class NewAzureSqlPremiumDatabaseTests : TestBase
    {
        [TestCleanup]
        public void CleanupTest()
        {
            // Remove the test databases
            NewAzureSqlPremiumDatabaseTests.RemoveTestDatabasesWithSqlAuth();

            // Save the mock session results
            MockServerHelper.SaveDefaultSessionCollection();
        }

        /// <summary>
        /// Create $premiumDB on the given context.
        /// </summary>
        /// <param name="powershell">The powershell instance containing the context.</param>
        /// <param name="contextVariable">The variable name that holds the server context.</param>
        [TestMethod]
        public void CreatePremiumDatabasesWithSqlAuth()
        {
            using (System.Management.Automation.PowerShell powershell =
                System.Management.Automation.PowerShell.Create())
            {
                NewAzureSqlDatabaseServerContextTests.CreateServerContextSqlAuth(
                    powershell,
                    "$context");
                HttpSession testSession = MockServerHelper.DefaultSessionCollection.GetSession(
                    "UnitTest.Common.CreatePremiumDatabasesWithSqlAuth");
                DatabaseTestHelper.SetDefaultTestSessionSettings(testSession);                
                testSession.RequestValidator =
                    new Action<HttpMessage, HttpMessage.Request>(
                    (expected, actual) =>
                    {
                        Assert.AreEqual(expected.RequestInfo.Method, actual.Method);
                        Assert.AreEqual(expected.RequestInfo.UserAgent, actual.UserAgent);
                        switch (expected.Index)
                        {
                            // Request 0-6: Query P1 and P2 Service Objective
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                            case 4:
                            case 5:
                            case 6:
                            // Request 7-9: Create NewAzureSqlPremiumDatabaseTests_P1
                            case 7:
                            case 8:
                            case 9:
                            // Request 10-12: Create NewAzureSqlPremiumDatabaseTests_P2
                            case 10:
                            case 11:
                            case 12:
                                DatabaseTestHelper.ValidateHeadersForODataRequest(
                                    expected.RequestInfo,
                                    actual);
                                break;
                            default:
                                Assert.Fail("No more requests expected.");
                                break;
                        }
                    });

                using (AsyncExceptionManager exceptionManager = new AsyncExceptionManager())
                {
                    Collection<PSObject> premiumDB_P1, PremiumDB_P2;
                    using (new MockHttpServer(
                        exceptionManager,
                        MockHttpServer.DefaultServerPrefixUri,
                        testSession))
                    {
                        powershell.InvokeBatchScript(
                            @"$P1 = Get-AzureSqlDatabaseServiceObjective" +
                            @" -Context $context" +
                            @" -ServiceObjectiveName ""Reserved P1""");                        

                        powershell.InvokeBatchScript(
                            @"$P2 = Get-AzureSqlDatabaseServiceObjective " +
                            @"-Context $context" +
                            @" -ServiceObjectiveName ""Reserved P2"""); 

                        premiumDB_P1 = powershell.InvokeBatchScript(
                            @"$premiumDB_P1 = New-AzureSqlDatabase " +
                            @"-Context $context " +
                            @"-DatabaseName NewAzureSqlPremiumDatabaseTests_P1 " +
                            @"-Edition Premium " +
                            @"-ServiceObjective $P1 ");
                        premiumDB_P1 = powershell.InvokeBatchScript("$PremiumDB_P1");

                        powershell.InvokeBatchScript(
                            @"$PremiumDB_P2 = New-AzureSqlDatabase " +
                            @"-Context $context " +
                            @"-DatabaseName NewAzureSqlPremiumDatabaseTests_P2 " +
                            @"-Collation Japanese_CI_AS " +
                            @"-Edition Premium " +
                            @"-ServiceObjective $P2 " +
                            @"-MaxSizeGB 10 " +
                            @"-Force");
                        PremiumDB_P2 = powershell.InvokeBatchScript("$PremiumDB_P2");
                    }

                    Assert.AreEqual(0, powershell.Streams.Error.Count, "Errors during run!");
                    Assert.AreEqual(0, powershell.Streams.Warning.Count, "Warnings during run!");
                    powershell.Streams.ClearStreams();
                    
                    Assert.IsTrue(
                        premiumDB_P1.Single().BaseObject is Services.Server.Database,
                        "Expecting a Database object");
                    Services.Server.Database databaseP1 =
                        (Services.Server.Database)premiumDB_P1.Single().BaseObject;
                    Assert.AreEqual("NewAzureSqlPremiumDatabaseTests_P1", databaseP1.Name, "Expected db name to be NewAzureSqlPremiumDatabaseTests_P1");

                    Assert.IsTrue(
                        PremiumDB_P2.Single().BaseObject is Services.Server.Database,
                        "Expecting a Database object");
                    Services.Server.Database databaseP2 =
                        (Services.Server.Database)PremiumDB_P2.Single().BaseObject;
                    Assert.AreEqual("NewAzureSqlPremiumDatabaseTests_P2", databaseP2.Name, "Expected db name to be NewAzureSqlPremiumDatabaseTests_P2");

                    Assert.AreEqual(
                        "Japanese_CI_AS",
                        databaseP2.CollationName,
                        "Expected collation to be Japanese_CI_AS");
                    /* SQL Server: Defect 1655888: When creating a premium database, 
                     * the immediate returned value do not have valid Edition and Max Database Size info                 
                     * We should active the following asserts once the defect is fixed.
                    Assert.AreEqual("Premium", database2Obj.Edition, "Expected edition to be Premium");
                    Assert.AreEqual(10, database2Obj.MaxSizeGB, "Expected max size to be 10 GB");
                     */
                }
            }
        }

        /// <summary>
        /// Helper function to remove the test databases.
        /// </summary>
        public static void RemoveTestDatabasesWithSqlAuth()
        {
            using (System.Management.Automation.PowerShell powershell =
                System.Management.Automation.PowerShell.Create())
            {
                // Create a context
                NewAzureSqlDatabaseServerContextTests.CreateServerContextSqlAuth(
                    powershell,
                    "$context");

                // Remove the 2 test databases
                NewAzureSqlPremiumDatabaseTests.RemoveTestDatabasesWithSqlAuth(
                    powershell,
                    "$context");
            }
        }

        /// <summary>
        /// Removes all existing db which name starting with PremiumTest on the given context.
        /// </summary>
        /// <param name="powershell">The powershell instance containing the context.</param>
        /// <param name="contextVariable">The variable name that holds the server context.</param>
        public static void RemoveTestDatabasesWithSqlAuth(
            System.Management.Automation.PowerShell powershell,
            string contextVariable)
        {
            HttpSession testSession = MockServerHelper.DefaultSessionCollection.GetSession(
                "UnitTest.Common.RemoveTestPremiumDatabasesWithSqlAuth");
            DatabaseTestHelper.SetDefaultTestSessionSettings(testSession);
            testSession.RequestValidator =
                new Action<HttpMessage, HttpMessage.Request>(
                (expected, actual) =>
                {
                    Assert.AreEqual(expected.RequestInfo.Method, actual.Method);
                    Assert.AreEqual(expected.RequestInfo.UserAgent, actual.UserAgent);
                    switch (expected.Index)
                    {
                        // Request 0-11: Remove database requests
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                            DatabaseTestHelper.ValidateHeadersForODataRequest(
                                expected.RequestInfo,
                                actual);
                            break;
                        default:
                            Assert.Fail("No more requests expected.");
                            break;
                    }
                });

            using (AsyncExceptionManager exceptionManager = new AsyncExceptionManager())
            {
                using (new MockHttpServer(
                    exceptionManager,
                    MockHttpServer.DefaultServerPrefixUri,
                    testSession))
                {
                    powershell.InvokeBatchScript(
                        @"Get-AzureSqlDatabase $context | " +
                        @"? {$_.Name.contains(""NewAzureSqlPremiumDatabaseTests"")} " +
                        @"| Remove-AzureSqlDatabase -Context $context -Force");
                }

                Assert.AreEqual(0, powershell.Streams.Error.Count, "Errors during run!");
                Assert.AreEqual(0, powershell.Streams.Warning.Count, "Warnings during run!");
                powershell.Streams.ClearStreams();
            }
        }
    }
}
