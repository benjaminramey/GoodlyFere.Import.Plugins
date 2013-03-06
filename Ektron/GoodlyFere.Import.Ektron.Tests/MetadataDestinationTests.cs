#region Usings

using System;
using System.Data;
using System.Linq;
using Ektron.Cms;
using GoodlyFere.Import.Ektron.Destination;
using Xunit;

#endregion

namespace GoodlyFere.Import.Ektron.Tests
{
    public class MetadataDestinationTests
    {
        #region Public Methods

        [Fact]
        public void NoContentId_Throws()
        {
            var dest = new MetadataDestination();
            var table = GetValidSchemaTable();
            table.Columns.Remove("contentId");

            Assert.Throws<ArgumentException>(() => dest.Receive(table));
        }

        [Fact]
        public void NoRows_Throws()
        {
            var dest = new MetadataDestination();
            var table = GetValidSchemaTable();

            Assert.Throws<ArgumentException>(() => dest.Receive(table));
        }

        [Fact]
        public void ValidRow_UpdatesMetadataFields()
        {
            var dest = new MetadataDestination();
            var table = GetValidTable();

            ContentData addedContent = EktronTestHelper.AddContent(
                new ContentData
                    {
                        Title = "Meta data test",
                        Html = "Meta data test content"
                    });

            table.Rows[0]["contentId"] = addedContent.Id;

            dest.Receive(table);

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