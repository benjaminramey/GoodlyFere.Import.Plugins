#region Usings

using System;
using System.Data;
using System.Linq;
using Common.Logging;
using Ektron.Cms;
using GoodlyFere.Import.Ektron.Tools;

#endregion

namespace GoodlyFere.Import.Ektron.Destination
{
    public class SmartFormDestination : ContentDestination
    {
        #region Constants and Fields

        private static readonly ILog Log = LogManager.GetLogger<SmartFormDestination>();

        #endregion

        #region Constructors and Destructors

        public SmartFormDestination(string ektronServicesUrl, string adminUserName, string adminPassword)
            : base(ektronServicesUrl, adminUserName, adminPassword)
        {
        }

        #endregion

        #region Methods

        protected override void SetContentFields(DataRow row, ContentData contentItem)
        {
            base.SetContentFields(row, contentItem);
            contentItem.ContType = 1;
            contentItem.XmlConfiguration = new XmlConfigData { Id = (long)row["smartFormId"] };
        }

        protected override bool TableHasValidSchema()
        {
            bool hasValidSchema = base.TableHasValidSchema()
                                  && DestinationHelper.HasColumn(Data, "smartFormId", typeof(long));

            Log.DebugFormat("Data table has valid schema: {0}", hasValidSchema);
            return hasValidSchema;
        }

        #endregion
    }
}