#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MetadataDestinationTests.cs">
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
using Ektron.Cms;
using GoodlyFere.Import.Ektron.Destination;
using Xunit;

#endregion

namespace GoodlyFere.Import.Ektron.Tests
{
    public class MetadataDestinationTests : IDisposable
    {
        #region Constants and Fields

        private readonly MetadataDestination _dest;
        private readonly string _expectedFolderPath;

        #endregion

        #region Constructors and Destructors

        public MetadataDestinationTests()
        {
            _expectedFolderPath = EktronTestHelper.TestFolderPath;
            string servicesUrl = ConfigurationManager.AppSettings["EktronServicesPath"];
            string username = ConfigurationManager.AppSettings["EktronAdminUsername"];
            string password = ConfigurationManager.AppSettings["EktronAdminPassword"];
            _dest = new MetadataDestination(servicesUrl, username, password);
        }

        #endregion

        #region Public Methods

        public void Dispose()
        {
            var list = EktronTestHelper.GetContentByFolderPath(_expectedFolderPath);
            EktronTestHelper.DeleteContent(list);
        }

        [Fact]
        public void NoContentId_Throws()
        {
            var table = GetValidSchemaTable();
            table.Columns.Remove("contentId");

            Assert.Throws<ArgumentException>(() => _dest.Receive(table));
        }

        [Fact]
        public void NoRows_Throws()
        {
            var table = GetValidSchemaTable();

            Assert.Throws<ArgumentException>(() => _dest.Receive(table));
        }

        [Fact]
        public void ValidRow_UpdatesMetadataFields()
        {
            var table = GetValidTable();

            ContentData addedContent = EktronTestHelper.AddContent(
                new ContentData
                    {
                        Title = "Meta data test",
                        Html = "Meta data test content"
                    });

            table.Rows[0]["contentId"] = addedContent.Id;

            _dest.Receive(table);

            var row = table.Rows[0];
            addedContent = EktronTestHelper.GetContent(addedContent.Id);
            Assert.Equal(row["Title"].ToString(), addedContent.MetaData.Single(md => md.Name == "Title").Text);
            Assert.Equal(
                row["Description"].ToString(), addedContent.MetaData.Single(md => md.Name == "Description").Text);
            Assert.Equal(row["Keywords"].ToString(), addedContent.MetaData.Single(md => md.Name == "Keywords").Text);
        }

        #endregion

        #region Methods

        private static DataTable GetValidSchemaTable()
        {
            var table = new DataTable("test");
            table.Columns.Add(new DataColumn("contentId", typeof(long)));
            table.Columns.Add(new DataColumn("Title", typeof(string)));
            table.Columns.Add(new DataColumn("Description", typeof(string)));
            table.Columns.Add(new DataColumn("Keywords", typeof(string)));
            return table;
        }

        private DataTable GetValidTable()
        {
            var table = GetValidSchemaTable();
            var row = table.NewRow();
            row["contentId"] = 0;
            row["Title"] = "metadata title";
            row["Description"] = "meta data description";
            row["Keywords"] = "one, keyword, and, another";
            table.Rows.Add(row);
            return table;
        }

        #endregion
    }
}