using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.GUI.Library;
using MediaPortal.Database;
using MediaPortal.Video.Database;
using SQLite.NET;

namespace WatchedSynchronizer
{

  /// <summary>
  /// The class WatchedSynchronizer.AdditionalVideoDatabaseSQLite provides all necessary properties/methods which do the real communication with a video database.
  /// Objects created from this class are returned by calls to methods of the class AdditionalDatabaseFactory,
  /// </summary>

  class AdditionalVideoDatabaseSQLite : VideoDatabaseSqlLite
  {

    #region Declaration

    private VideoDatabaseSqlLite mInstance = new VideoDatabaseSqlLite();
    private SQLiteClient mDatabase;
    private string mDatabaseFile;

    #endregion

    #region Constructors

    public AdditionalVideoDatabaseSQLite(string strDatabaseFile)
    {
      mDatabaseFile = strDatabaseFile;
      Open();
    }

    #endregion

    #region Properties

    public VideoDatabaseSqlLite Instance
    {
      get
      {
        return mInstance;
      }
    }

    #endregion Properties

    #region Private methods

    private void Open()
    {
      Log.Info("WatchedSynchronizer: Opening video database " + mDatabaseFile);
      try
      {
        if (mDatabase != null)
        {
          Log.Info("WatchedSynchronizer: Video database already opened.");
          return;
        }
        mDatabase = new SQLiteClient(mDatabaseFile);
        DatabaseUtility.SetPragmas(mDatabase);
        mInstance.m_db = mDatabase;
        Log.Info("WatchedSynchronizer: Video database opened.");
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: Video database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    #endregion

  }
}