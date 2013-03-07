#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SmartFormDestinationTests.cs">
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
    public class SmartFormDestinationTests : IDisposable
    {
        #region Constants and Fields

        private readonly SmartFormDestination _destination;
        private readonly string _expectedFolderName;

        #endregion

        #region Constructors and Destructors

        public SmartFormDestinationTests()
        {
            _expectedFolderName = EktronTestHelper.TestFolderName;
            string servicesUrl = ConfigurationManager.AppSettings["ek_ServicesPath"];
            string username = ConfigurationManager.AppSettings["EktronAdminUsername"];
            string password = ConfigurationManager.AppSettings["EktronAdminPassword"];
            _destination = new SmartFormDestination(servicesUrl, username, password);
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
            Assert.Equal((long)row["smartFormId"], contentItem.XmlConfiguration.Id);
        }

        public void Dispose()
        {
            var list = EktronTestHelper.GetContentByFolderName(_expectedFolderName);
            EktronTestHelper.DeleteContent(list);
        }

        [Fact]
        public void NoSmartFormIdField_Throws()
        {
            var table = GetValidTable();
            table.Columns.Remove("smartFormId");

            Assert.Throws<ArgumentException>(() => _destination.Receive(table));
        }

        [Fact]
        public void ValidTable_DoesntThrow()
        {
            var table = GetValidTable();

            Assert.DoesNotThrow(() => _destination.Receive(table));
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
            table.Columns.Add(new DataColumn("smartFormId", typeof(long)));
            return table;
        }

        private DataTable GetValidTable()
        {
            var table = GetValidSchemaTable();
            var row = table.NewRow();
            row["html"] = @"<root>
  <LastUpage></LastUpage>
  <Values>
    <Row>
      <Label></Label>
      <Value></Value>
    </Row>
  </Values>
</root>
";
            row["folderName"] = _expectedFolderName;
            row["title"] = "title";
            row["smartFormId"] = 39;
            row["contentId"] = 0;
            table.Rows.Add(row);
            return table;
        }

        #endregion
    }
}