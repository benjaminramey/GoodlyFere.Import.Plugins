#region Usings

using System;
using System.Data;
using System.Linq;
using System.Xml;
using GoodlyFere.Import.Ektron.Destination;
using Xunit;

#endregion

namespace GoodlyFere.Import.Ektron.Tests
{
    public class SmartFormDestinationTests : IDisposable
    {
        #region Constants and Fields

        private readonly string _expectedFolderName;

        #endregion

        #region Constructors and Destructors

        public SmartFormDestinationTests()
        {
            _expectedFolderName = EktronTestHelper.TestFolderName;
        }

        #endregion

        #region Public Methods

        public void Dispose()
        {
            var list = EktronTestHelper.GetContentByFolderName(_expectedFolderName);
            EktronTestHelper.DeleteContent(list);
        }

        [Fact]
        public void AddedContentFieldsMatchRowValues()
        {
            var destination = new SmartFormDestination();
            var table = GetValidTable();

            destination.Receive(table);
            var contentItem = EktronTestHelper.GetContentByFolderName(_expectedFolderName).First();

            DataRow row = table.Rows[0];
            Assert.Equal((long)row["smartFormId"], contentItem.XmlConfiguration.Id);
        }
        
        [Fact]
        public void NoSmartFormIdField_Throws()
        {
            var destination = new SmartFormDestination();
            var table = GetValidTable();
            table.Columns.Remove("smartFormId");

            Assert.Throws<ArgumentException>(() => destination.Receive(table));
        }

        [Fact]
        public void ValidTable_DoesntThrow()
        {
            var destination = new SmartFormDestination();
            var table = GetValidTable();

            Assert.DoesNotThrow(() => destination.Receive(table));
        }

        #endregion

        #region Methods

        private static DataTable GetValidSchemaTable()
        {
            var table = new DataTable("test");
            table.Columns.Add(new DataColumn("folderName", typeof(string)));
            table.Columns.Add(new DataColumn("html", typeof(string)));
            table.Columns.Add(new DataColumn("title", typeof(string)));
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
            table.Rows.Add(row);
            return table;
        }

        #endregion
    }
}