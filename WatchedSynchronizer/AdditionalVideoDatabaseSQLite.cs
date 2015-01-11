#region GPL License

//Mediaportal (http://www.team-mediaportal.com/) Plugin - WatchedSynchronizer
//Copyright (C) 2015 Max Wimmelbacher
//
//This program is free software; you can redistribute it and/or
//modify it under the terms of the GNU General Public License
//as published by the Free Software Foundation; either version 2
//of the License, or (at your option) any later version.
//
//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

#endregion

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