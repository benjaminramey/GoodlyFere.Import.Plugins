#region Usings

using System.Linq;
using System;
using Common.Logging;
using Ektron.Cms.Framework.User;
using GoodlyFere.Import.Ektron.Destination;

#endregion

namespace GoodlyFere.Import.Ektron.Tools
{
    public class Authenticator
    {
        #region Constants and Fields

        private static readonly ILog Log = LogManager.GetLogger<Authenticator>();
        private readonly string _adminPassword;
        private readonly string _adminUserName;
        private readonly object _authTokenLock = new object();
        private readonly object _authenticatingLock = new object();
        private bool _authenticating;

        #endregion

        #region Constructors and Destructors

        public Authenticator(string adminUserName, string adminPassword)
        {
            _adminUserName = adminUserName;
            _adminPassword = adminPassword;
            Authenticate();
        }

        #endregion

        #region Public Properties

        public string AuthToken { get; set; }

        public bool HasAuthentication
        {
            get
            {
                lock (_authTokenLock)
                {
                    return !string.IsNullOrWhiteSpace(AuthToken);
                }
            }
        }

        #endregion

        #region Public Methods

        public void Authenticate()
        {
            lock (_authenticatingLock)
            {
                if (_authenticating)
                {
                    return;
                }

                _authenticating = true;
            }

            Log.InfoFormat(
                "Authenticating with username: {0} and password: {1}", _adminUserName, _adminPassword);

            lock (_authTokenLock)
            {
                UserManager um = new UserManager();
                AuthToken = um.Authenticate(_adminUserName, _adminPassword);
            }

            lock (_authenticatingLock)
            {
                _authenticating = false;
            }
        }

        #endregion
    }
}