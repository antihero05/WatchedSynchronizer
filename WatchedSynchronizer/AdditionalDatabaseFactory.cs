using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Video.Database;

namespace WatchedSynchronizer
{
  /// <summary>
  /// The class WatchedSynchronizer.AdditionalDatabaseFactory is providing static methods to get database objects.
  /// These returned objects are mapped to the database residing on the path provided in the input parameter strDatabaseFile.
  /// </summary>
  class AdditionalDatabaseFactory
  {
    #region Public Methods

    public static IVideoDatabase GetVideoDatabase(string strDatabaseFile)
    {
      var objAdditionalDatabase = new AdditionalVideoDatabaseSQLite(strDatabaseFile);
      return objAdditionalDatabase.Instance;
    }

    public static AdditionalTVSeriesDatabaseSQLite GetTVSeriesDatabase(string strDataBaseFile)
    {
      var objAdditionalDatabase = new AdditionalTVSeriesDatabaseSQLite(strDataBaseFile);
      return objAdditionalDatabase;
    }

    #endregion

  }
}