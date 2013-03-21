#region Usings

using System;
using System.Data;
using System.IO;
using System.Linq;
using Common.Logging;
using Ektron.Cms;
using Ektron.Cms.Content;
using GoodlyFere.Import.Ektron.Tools;

#endregion

namespace GoodlyFere.Import.Ektron.Destination
{
    public class AssetDestination : ContentDestination
    {
        #region Constants and Fields

        private static readonly ILog Log = LogManager.GetLogger<AssetDestination>();

        #endregion

        #region Constructors and Destructors

        public AssetDestination(string ektronServicesUrl, string adminUserName, string adminPassword)
            : base(ektronServicesUrl, adminUserName, adminPassword)
        {
        }

        #endregion

        #region Properties

        private IAssetManager AssetManager
        {
            get
            {
                IAssetManager mgr = ObjectFactory.GetAssetManager();
                mgr.RequestInformation.AuthenticationToken = AuthToken;
                return mgr;
            }
        }

        #endregion

        #region Methods

        protected override void DoAPIUpdateCall(ContentData data)
        {
            AssetManager.Update((ContentAssetData)data);
        }

        protected override ContentData GetNewContentDataObject()
        {
            return new ContentAssetData();
        }

        protected override void SetContentFields(DataRow row, ContentData contentItem)
        {
            ContentAssetData assetItem = (ContentAssetData)contentItem;
            assetItem.Title = row["title"].ToString();
            assetItem.File = GetFileBytes(row["filePath"].ToString());
        }

        protected override bool TableHasValidSchema()
        {
            bool tableHasValidSchema = DestinationHelper.HasColumn(Data, "folderPath", typeof(string))
                                       && DestinationHelper.HasColumn(Data, "title", typeof(string))
                                       && DestinationHelper.HasColumn(Data, "filePath", typeof(string));
            Log.InfoFormat("Table has valid schema: {0}", tableHasValidSchema);
            return tableHasValidSchema;
        }

        private byte[] GetFileBytes(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }

        #endregion
    }
}