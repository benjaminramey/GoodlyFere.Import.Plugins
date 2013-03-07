#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ContentDestinationTests.cs">
// GoodlyFere.Import.Ektron.Tests
// 
// Copyright (C) 2013 Benjamin Ramey
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// 
// http://www.gnu.org/licenses/lgpl-2.1-standalone.html
// 
// You can contact me at ben.ramey@gmail.com.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#endregion

#region Usings

using System;
using System.Configuration;
using System.Data;
using System.Linq;
using GoodlyFere.Import.Ektron.Destination;
using Xunit;

#endregion

namespace GoodlyFere.Import.Ektron.Tests
{
    public class ContentDestinationTests : IDisposable
    {
        #region Constants and Fields

        private readonly ContentDestination _destination;
        private readonly string _expectedFolderName;

        #endregion

        #region Constructors and Destructors

        public ContentDestinationTests()
        {
            _expectedFolderName = EktronTestHelper.TestFolderName;
            string servicesUrl = ConfigurationManager.AppSettings["EktronServicesPath"];
            string username = ConfigurationManager.AppSettings["EktronAdminUsername"];
            string password = ConfigurationManager.AppSettings["EktronAdminPassword"];
            _destination = new ContentDestination(servicesUrl, username, password);
        }

        #endregion

        #region Public Methods

        [Fact]
        public void AddedContentFieldsMatchRowValues()
        {
            var table = GetValidTable();

            _destination.Receive(table);
            var contentItem = EktronTestHelper.GetContentByFolderName(_expectedFolderName).First();

            DataRow row = table.Rows[0];
            Assert.Equal(row["title"].ToString(), contentItem.Title);
            Assert.Equal(row["folderName"].ToString(), contentItem.FolderName);
            Assert.Equal(row["html"].ToString(), contentItem.Html);
        }

        public void Dispose()
        {
            var list = EktronTestHelper.GetContentByFolderName(_expectedFolderName);
            EktronTestHelper.DeleteContent(list);
        }

        [Fact]
        public void DoesNotAddDuplicates_AcrossRequests()
        {
            string expectedTitle = "testing ektron destination";

            DataTable[] tables = new[]
                {
                    GetValidSchemaTable(),
                    GetValidSchemaTable()
                };

            for (int i = 0; i < 2; i++)
            {
                var row = tables[i].NewRow();
                row["html"] = "this is some html!";
                row["title"] = expectedTitle;
                row["folderName"] = _expectedFolderName;
                tables[i].Rows.Add(row);
            }

            var beforeList = EktronTestHelper.GetContentByFolderName(_expectedFolderName);
            _destination.Receive(tables[0]);
            _destination.Receive(tables[1]);
            var afterList = EktronTestHelper.GetContentByFolderName(_expectedFolderName);

            Assert.True(afterList.Count - beforeList.Count > 0, "Destination did not add any items");
            Assert.True(afterList.Count - beforeList.Count == 1, "Destination added more than one identical item");
        }

        [Fact]
        public void DoesNotAddDuplicates_WithinARequest()
        {
            string expectedTitle = "testing ektron destination";

            var table = GetValidSchemaTable();

            for (int i = 0; i < 2; i++)
            {
                var row = table.NewRow();
                row["html"] = "this is some html!";
                row["title"] = expectedTitle;
                row["folderName"] = _expectedFolderName;
                table.Rows.Add(row);
            }

            var beforeList = EktronTestHelper.GetContentByFolderName(_expectedFolderName);
            _destination.Receive(table);
            var afterList = EktronTestHelper.GetContentByFolderName(_expectedFolderName);

            Assert.True(afterList.Count - beforeList.Count > 0, "Destination did not add any items");
            Assert.True(afterList.Count - beforeList.Count == 1, "Destination added more than one identical item");
        }

        [Fact]
        public void IncorrectColumns_Throws()
        {
            var table = new DataTable("bob");

            Assert.Throws<ArgumentException>(() => _destination.Receive(table));
        }

        [Fact]
        public void NoRows_Throws()
        {
            var table = GetValidSchemaTable();

            Assert.Throws<ArgumentException>(() => _destination.Receive(table));
        }

        [Fact]
        public void UpdatedContentFieldsMatchRowValues()
        {
            var table = GetValidTable();

            _destination.Receive(table);
            var contentItem = EktronTestHelper.GetContentByFolderName(_expectedFolderName).First();

            DataRow row = table.Rows[0];
            row["contentId"] = contentItem.Id;
            row["html"] = "this is the updated html!!!";

            _destination.Receive(table);
            contentItem = EktronTestHelper.GetContentByFolderName(_expectedFolderName).First();

            Assert.Equal(row["title"].ToString(), contentItem.Title);
            Assert.Equal(row["folderName"].ToString(), contentItem.FolderName);
            Assert.Equal(row["html"].ToString(), contentItem.Html);
        }

        [Fact]
        public void ValidTable_DoesntThrow()
        {
            var table = GetValidTable();

            Assert.DoesNotThrow(() => _destination.Receive(table));
        }

        [Fact]
        public void ValidTable_InsertIntoEktron()
        {
            string expectedTitle = "testing ektron destination";

            var table = GetValidSchemaTable();

            var row = table.NewRow();
            row["html"] = "this is some html!";
            row["title"] = expectedTitle;
            row["folderName"] = _expectedFolderName;
            table.Rows.Add(row);

            _destination.Receive(table);
            var list = EktronTestHelper.GetContentByFolderName(_expectedFolderName);

            Assert.True(list.Count > 0, "No items found");
            Assert.True(list.Any(c => c.Title == expectedTitle), "No item with expected title found");
        }

        #endregion

        #region Methods

        private static DataTable GetValidSchemaTable()
        {
            var table = new DataTable("test");
            table.Columns.Add(new DataColumn("folderName", typeof(string)));
            table.Columns.Add(new DataColumn("html", typeof(string)));
            table.Columns.Add(new DataColumn("title", typeof(string)));
            table.Columns.Add(new DataColumn("contentId", typeof(long)));
            return table;
        }

        private DataTable GetValidTable()
        {
            var table = GetValidSchemaTable();
            var row = table.NewRow();
            row["html"] = "html";
            row["folderName"] = _expectedFolderName;
            row["title"] = "title";
            row["contentId"] = 0;
            table.Rows.Add(row);
            return table;
        }

        #endregion
    }
}