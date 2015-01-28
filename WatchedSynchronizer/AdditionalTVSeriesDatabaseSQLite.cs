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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.GUI.Library;
using MediaPortal.Database;
using SQLite.NET;
using WindowPlugins.GUITVSeries;

namespace WatchedSynchronizer
{

  /// <summary>
  /// The class WatchedSynchronizer.AdditionalTVSeriesDatabaseSQLite provides all necessary properties/methods which do the real communication with a tvseries database.
  /// Objects created from this class are returned by calls to methods of the class AdditionalDatabaseFactory,
  /// </summary>

  class AdditionalTVSeriesDatabaseSQLite : IDisposable
  {

    #region Declaration

    private Dictionary<string, SQLiteResultCacheItem> mCache;
    private SQLiteClient mDatabase;
    private string mDatabaseFile;

    #endregion

    #region Constructors

    public AdditionalTVSeriesDatabaseSQLite(string strDatabaseFile)
    {
      mDatabaseFile = strDatabaseFile;
      Open();
    }

    #endregion

    #region Properties

    public string DatabaseName
    {
      get
      {
        return mDatabaseFile;
      }
    }

    #endregion Properties

    #region Public methods

    public void Dispose()
    {
      mDatabase.Close();
      mDatabase = null;
    }

    public string GetCompositeId(string strFilenameAndPath)
    {
      try
      {
        string strCompositeID = string.Empty;
        strFilenameAndPath = strFilenameAndPath.Trim();
        DatabaseUtility.RemoveInvalidChars(ref strFilenameAndPath);
        string strSQL = String.Format("SELECT * FROM {0} WHERE {1} = '{2}'", DBEpisode.cTableName, DBEpisode.cFilename, strFilenameAndPath);
        Log.Debug("WatchedSynchronizer: (GetCompositeId) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
        SQLiteResultSet objResults = mDatabase.Execute(strSQL);
        if (objResults.Rows.Count > 0)
        {
          strCompositeID = DatabaseUtility.Get(objResults, 0, DBEpisode.cCompositeID);
          AddToCache(DBEpisode.cTableName, strCompositeID, objResults);
        }
        return strCompositeID;
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
      return string.Empty;
    }

    public void GetCompositeIds(string strCompositeId2, ref ArrayList alsCompositeIds)
    {
      try
      {
        string strSQL = String.Format("SELECT * FROM {0} WHERE {1} = '{2}'", DBEpisode.cTableName, DBEpisode.cCompositeID2, strCompositeId2);
        Log.Debug("WatchedSynchronizer: (GetCompositeIds) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
        SQLiteResultSet objResults = mDatabase.Execute(strSQL);
        if (objResults.Rows.Count == 0)
        {
          return;
        }
        for (int intLoop = 0; intLoop < objResults.Rows.Count; ++intLoop)
        {
          string strFile = DatabaseUtility.Get(objResults, intLoop, DBEpisode.cCompositeID);
          alsCompositeIds.Add(strFile);
        }
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    public string GetCompositeId2(string strCompositeId)
    {
      try
      {
        string strCompositeId2 = string.Empty;
        if (CheckCache(DBEpisode.cTableName, strCompositeId, DBEpisode.cCompositeID2, ref strCompositeId2) == false)
        {
          string strSQL = String.Format("SELECT * FROM {0} WHERE {1} = '{2}'", DBEpisode.cTableName, DBEpisode.cCompositeID, strCompositeId);
          Log.Debug("WatchedSynchronizer: (GetCompositeId2) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          SQLiteResultSet objResults = mDatabase.Execute(strSQL);
          if (objResults.Rows.Count > 0)
          {
            strCompositeId2 = DatabaseUtility.Get(objResults, 0, DBEpisode.cCompositeID2);
            AddToCache(DBEpisode.cTableName, strCompositeId, objResults);
          }
        }
        return strCompositeId2;
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
      return string.Empty;
    }

    public int GetEpisodeStopTime(string strCompositeId)
    {
      try
      {
        string strEpisodeStopTime = string.Empty;
        if (CheckCache(DBEpisode.cTableName, strCompositeId, DBEpisode.cStopTime, ref strEpisodeStopTime) == false)
        {
          string strSQL = String.Format("SELECT * FROM {0} WHERE {1} = '{2}'", DBEpisode.cTableName, DBEpisode.cCompositeID, strCompositeId);
          Log.Debug("WatchedSynchronizer: (GetEpisodeStopTime) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          SQLiteResultSet objResults = mDatabase.Execute(strSQL);
          if (objResults.Rows.Count > 0)
          {
            strEpisodeStopTime = DatabaseUtility.Get(objResults, 0, DBEpisode.cStopTime);
            AddToCache(DBEpisode.cTableName, strCompositeId, objResults);
          }
        }
        int intStopTime;
        Int32.TryParse(strEpisodeStopTime, out intStopTime);
        return intStopTime;
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
      return 0;
    }

    public void SetEpisodeStopTime(string strCompositeId, int intStopTime)
    {
      try
      {
        string strEpisodeStopTime = intStopTime.ToString();
        if (CheckCache(DBEpisode.cTableName, strCompositeId, DBEpisode.cStopTime, ref strEpisodeStopTime) == false)
        {
          string strSQL = String.Format("UPDATE {0} SET {1} = {2} WHERE {3} = '{4}'", DBEpisode.cTableName, DBEpisode.cStopTime, intStopTime, DBEpisode.cCompositeID, strCompositeId);
          Log.Debug("WatchedSynchronizer: (SetEpisodeStopTime) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          mDatabase.Execute(strSQL);
        }
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    public bool GetEpisodeWatchedStatus(string strCompositeId)
    {
      try
      {
        string strEpisodeWatchedStatus = string.Empty;
        if (CheckCache(DBOnlineEpisode.cTableName, strCompositeId, DBOnlineEpisode.cWatched, ref strEpisodeWatchedStatus) == false)
        {
          string strSQL = String.Format("SELECT * FROM {0} WHERE {1} = '{2}'", DBOnlineEpisode.cTableName, DBOnlineEpisode.cCompositeID, strCompositeId);
          Log.Debug("WatchedSynchronizer: (GetEpisodeWatchedStatus) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          SQLiteResultSet objResults = mDatabase.Execute(strSQL);
          if (objResults.Rows.Count > 0)
          {
            strEpisodeWatchedStatus = DatabaseUtility.Get(objResults, 0, DBOnlineEpisode.cWatched);
            AddToCache(DBOnlineEpisode.cTableName, strCompositeId, objResults);
          }
        }
        return (strEpisodeWatchedStatus == "1");
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
      return true;
    }

    public void SetEpisodeWatchedStatus(string strCompositeId, bool bolWatchedStatus)
    {
      try
      {
        int intWatchedStatus;
        if (bolWatchedStatus == true)
        {
          intWatchedStatus = 1;
        }
        else
        {
          intWatchedStatus = 0;
        }
        string strEpisodeWatchedStatus = intWatchedStatus.ToString();
        if (CheckCache(DBOnlineEpisode.cTableName, strCompositeId, DBOnlineEpisode.cWatched, ref strEpisodeWatchedStatus) == false)
        {
          string strSQL = String.Format("UPDATE {0} SET {1} = {2} WHERE {3} = '{4}'", DBOnlineEpisode.cTableName, DBOnlineEpisode.cWatched, intWatchedStatus, DBOnlineEpisode.cCompositeID, strCompositeId);
          Log.Debug("WatchedSynchronizer: (SetEpisodeWatchedStatus) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          mDatabase.Execute(strSQL);
        }
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    public int GetEpisodePlayCount(string strCompositeId)
    {
      try
      {
        string strEpisodePlayCount = string.Empty;
        if (CheckCache(DBOnlineEpisode.cTableName, strCompositeId, DBOnlineEpisode.cPlayCount, ref strEpisodePlayCount) == false)
        {
          string strSQL = String.Format("SELECT * FROM {0} WHERE {1} = '{2}'", DBOnlineEpisode.cTableName, DBOnlineEpisode.cCompositeID, strCompositeId);
          Log.Debug("WatchedSynchronizer: (GetEpisodePlayCount) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          SQLiteResultSet objResults = mDatabase.Execute(strSQL);
          if (objResults.Rows.Count > 0)
          {
            strEpisodePlayCount = DatabaseUtility.Get(objResults, 0, DBOnlineEpisode.cPlayCount);
            AddToCache(DBOnlineEpisode.cTableName, strCompositeId, objResults);
          }
        }
        int intUnwatchedEpisodes;
        Int32.TryParse(strEpisodePlayCount, out intUnwatchedEpisodes);
        return intUnwatchedEpisodes;
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
      return 0;
    }

    public void SetEpisodePlayCount(string strCompositeId, int intPlayCount)
    {
      try
      {
        string strPlayCount = intPlayCount.ToString();
        if (CheckCache(DBOnlineEpisode.cTableName, strCompositeId, DBOnlineEpisode.cPlayCount, ref strPlayCount) == false)
        {
          string strSQL = String.Format("UPDATE {0} SET {1} = '{2}' WHERE {3} = '{4}'", DBOnlineEpisode.cTableName, DBOnlineEpisode.cPlayCount, strPlayCount, DBOnlineEpisode.cCompositeID, strCompositeId);
          Log.Debug("WatchedSynchronizer: (SetEpisodePlayCount) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          mDatabase.Execute(strSQL);
        }
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    public string GetEpisodeWatchedDate(string strCompositeId)
    {
      try
      {
        string strEpisodeWatchedDate = string.Empty;
        if (CheckCache(DBEpisode.cTableName, strCompositeId, DBEpisode.cDateWatched, ref strEpisodeWatchedDate) == false)
        {
          string strSQL = String.Format("SELECT * FROM {0} WHERE {1} = '{2}'", DBEpisode.cTableName, DBEpisode.cCompositeID, strCompositeId);
          Log.Debug("WatchedSynchronizer: (GetEpisodeWatchedDate) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          SQLiteResultSet objResults = mDatabase.Execute(strSQL);
          if (objResults.Rows.Count > 0)
          {
            strEpisodeWatchedDate = DatabaseUtility.Get(objResults, 0, DBEpisode.cDateWatched);
            AddToCache(DBEpisode.cTableName, strCompositeId, objResults);
          }
        }
        return strEpisodeWatchedDate;
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
      return string.Empty;
    }

    public void SetEpisodeWatchedDate(string strCompositeId, string strWatchedDate)
    {
      try
      {
        if (CheckCache(DBEpisode.cTableName, strCompositeId, DBEpisode.cDateWatched, ref strWatchedDate) == false)
        {
          string strSQL = String.Format("UPDATE {0} SET {1} = '{2}' WHERE {3} = '{4}'", DBEpisode.cTableName, DBEpisode.cDateWatched, strWatchedDate, DBEpisode.cCompositeID, strCompositeId);
          Log.Debug("WatchedSynchronizer: (SetEpisodeWatchedDate) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          mDatabase.Execute(strSQL);
        }
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    public string GetEpisodeFirstWatchedDate(string strCompositeId)
    {
      try
      {
        string strEpisodeFirstWatchedDate = string.Empty;
        if (CheckCache(DBOnlineEpisode.cTableName, strCompositeId, DBOnlineEpisode.cFirstWatchedDate, ref strEpisodeFirstWatchedDate) == false)
        {
          string strSQL = String.Format("SELECT * FROM {0} WHERE {1} = '{2}'", DBOnlineEpisode.cTableName, DBOnlineEpisode.cCompositeID, strCompositeId);
          Log.Debug("WatchedSynchronizer: (GetEpisodeWatchedDate) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          SQLiteResultSet objResults = mDatabase.Execute(strSQL);
          if (objResults.Rows.Count > 0)
          {
            strEpisodeFirstWatchedDate = DatabaseUtility.Get(objResults, 0, DBOnlineEpisode.cFirstWatchedDate);
            AddToCache(DBOnlineEpisode.cTableName, strCompositeId, objResults);
          }
        }
        return strEpisodeFirstWatchedDate;
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
      return string.Empty;
    }

    public void SetEpisodeFirstWatchedDate(string strCompositeId, string strFirstWatchedDate)
    {
      try
      {
        if (CheckCache(DBOnlineEpisode.cTableName, strCompositeId, DBOnlineEpisode.cFirstWatchedDate, ref strFirstWatchedDate) == false)
        {
          string strSQL = String.Format("UPDATE {0} SET {1} = '{2}' WHERE {3} = '{4}'", DBOnlineEpisode.cTableName, DBOnlineEpisode.cFirstWatchedDate, strFirstWatchedDate, DBOnlineEpisode.cCompositeID, strCompositeId);
          Log.Debug("WatchedSynchronizer: (SetEpisodeWatchedDate) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          mDatabase.Execute(strSQL);
        }
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    public string GetEpisodeLastWatchedDate(string strCompositeId)
    {
      try
      {
        string strEpisodeLastWatchedDate = string.Empty;
        if (CheckCache(DBOnlineEpisode.cTableName, strCompositeId, DBOnlineEpisode.cLastWatchedDate, ref strEpisodeLastWatchedDate) == false)
        {
          string strSQL = String.Format("SELECT * FROM {0} WHERE {1} = '{2}'", DBOnlineEpisode.cTableName, DBOnlineEpisode.cCompositeID, strCompositeId);
          Log.Debug("WatchedSynchronizer: (GetEpisodeWatchedDate) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          SQLiteResultSet objResults = mDatabase.Execute(strSQL);
          if (objResults.Rows.Count > 0)
          {
            strEpisodeLastWatchedDate = DatabaseUtility.Get(objResults, 0, DBOnlineEpisode.cLastWatchedDate);
            AddToCache(DBOnlineEpisode.cTableName, strCompositeId, objResults);
          }
        }
        return strEpisodeLastWatchedDate;
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
      return string.Empty;
    }

    public void SetEpisodeLastWatchedDate(string strCompositeId, string strLastWatchedDate)
    {
      try
      {
        if (CheckCache(DBOnlineEpisode.cTableName, strCompositeId, DBOnlineEpisode.cLastWatchedDate, ref strLastWatchedDate) == false)
        {
          string strSQL = String.Format("UPDATE {0} SET {1} = '{2}' WHERE {3} = '{4}'", DBOnlineEpisode.cTableName, DBOnlineEpisode.cLastWatchedDate, strLastWatchedDate, DBOnlineEpisode.cCompositeID, strCompositeId);
          Log.Debug("WatchedSynchronizer: (SetEpisodeWatchedDate) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          mDatabase.Execute(strSQL);
        }
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    public string GetSeasonId(string strCompositeId)
    {
      try
      {
        string strSeriesId = string.Empty;
        string strSeasonId = string.Empty;
        if (CheckCache(DBOnlineEpisode.cTableName, strCompositeId, DBOnlineEpisode.cSeriesID, ref strSeriesId) == false || CheckCache(DBOnlineEpisode.cTableName, strCompositeId, DBOnlineEpisode.cSeasonIndex, ref strSeasonId))
        {
          string strSQL = String.Format("SELECT * FROM {0} WHERE {1} = '{2}'", DBOnlineEpisode.cTableName, DBOnlineEpisode.cCompositeID, strCompositeId);
          Log.Debug("WatchedSynchronizer: (GetSeasonId) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          SQLiteResultSet objResults = mDatabase.Execute(strSQL);
          if (objResults.Rows.Count > 0)
          {
            strSeriesId = (string)DatabaseUtility.Get(objResults, 0, DBOnlineEpisode.cSeriesID);
            strSeasonId = (string)DatabaseUtility.Get(objResults, 0, DBOnlineEpisode.cSeasonIndex);
            AddToCache(DBOnlineEpisode.cTableName, strCompositeId, objResults);
          }
        }
        return (strSeriesId + "_s" + strSeasonId);
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
      return string.Empty;
    }

    public bool GetSeasonWatchedStatus(string strSeasonId)
    {
      try
      {
        string strSeasonWatchedStatus = string.Empty;
        if (CheckCache(DBSeason.cTableName, strSeasonId, DBSeason.cUnwatchedItems, ref strSeasonWatchedStatus) == false)
        {
          string strSQL = String.Format("SELECT * FROM {0} WHERE {1} = '{2}'", DBSeason.cTableName, DBSeason.cID, strSeasonId);
          Log.Debug("WatchedSynchronizer: (GetSeasonWatchedStatus) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          SQLiteResultSet objResults = mDatabase.Execute(strSQL);
          if (objResults.Rows.Count > 0)
          {
            strSeasonWatchedStatus = DatabaseUtility.Get(objResults, 0, DBSeason.cUnwatchedItems);
            AddToCache(DBSeason.cTableName, strSeasonId, objResults);
          }
        }
        return (strSeasonWatchedStatus == "0");
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
      return true;
    }

    public void SetSeasonWatchedStatus(string strSeasonId, bool bolWatchedStatus)
    {
      try
      {
        int intUnwatchedItems;
        if (bolWatchedStatus == true)
        {
          intUnwatchedItems = 0;
        }
        else
        {
          intUnwatchedItems = 1;
        }
        string strSeasonWatchedStatus = intUnwatchedItems.ToString();
        if (CheckCache(DBSeason.cTableName, strSeasonId, DBSeason.cUnwatchedItems, ref strSeasonWatchedStatus) == false)
        {
          string strSQL = String.Format("UPDATE {0} SET {1} = {2} WHERE {3} = '{4}'", DBSeason.cTableName, DBSeason.cUnwatchedItems, intUnwatchedItems, DBSeason.cID, strSeasonId);
          Log.Debug("WatchedSynchronizer: (SetSeasonWatchedStatus) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          mDatabase.Execute(strSQL);
        }
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    public int GetSeasonUnwatchedEpisodes(string strSeasonId)
    {
      try
      {
        string strSeasonUnwatchedEpisodes = string.Empty;
        if (CheckCache(DBSeason.cTableName, strSeasonId, DBSeason.cEpisodesUnWatched, ref strSeasonUnwatchedEpisodes) == false)
        {
          string strSQL = String.Format("SELECT * FROM {0} WHERE {1} = '{2}'", DBSeason.cTableName, DBSeason.cID, strSeasonId);
          Log.Debug("WatchedSynchronizer: (GetSeasonUnwatchedEpisodes) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          SQLiteResultSet objResults = mDatabase.Execute(strSQL);
          if (objResults.Rows.Count > 0)
          {
            strSeasonUnwatchedEpisodes = DatabaseUtility.Get(objResults, 0, DBSeason.cEpisodesUnWatched);
            AddToCache(DBSeason.cTableName, strSeasonId, objResults);
          }
        }
        int intUnwatchedEpisodes;
        Int32.TryParse(strSeasonUnwatchedEpisodes, out intUnwatchedEpisodes);
        return intUnwatchedEpisodes;
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
      return 0;
    }

    public void SetSeasonUnwatchedEpisodes(string strSeasonId, int intUnwatchedEpisodes)
    {
      try
      {
        string strSeasonUnwatchedEpisodes = intUnwatchedEpisodes.ToString();
        if (CheckCache(DBSeason.cTableName, strSeasonId, DBSeason.cEpisodesUnWatched, ref strSeasonUnwatchedEpisodes) == false)
        {
          string strSQL = String.Format("UPDATE {0} SET {1} = {2} WHERE {3} = '{4}'", DBSeason.cTableName, DBSeason.cEpisodesUnWatched, intUnwatchedEpisodes, DBSeason.cID, strSeasonId);
          Log.Debug("WatchedSynchronizer: (SetSeasonUnwatchedEpisodes) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          mDatabase.Execute(strSQL);
        }
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    public string GetSeriesId(string strCompositeId)
    {
      try
      {
        string strSeriesId = string.Empty;
        if (CheckCache(DBOnlineEpisode.cTableName, strCompositeId, DBOnlineEpisode.cSeriesID, ref strSeriesId) == false)
        {
          string strSQL = String.Format("SELECT * FROM {0} WHERE {1} = '{2}'", DBOnlineEpisode.cTableName, DBOnlineEpisode.cCompositeID, strCompositeId);
          Log.Debug("WatchedSynchronizer: (GetSeriesId) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          SQLiteResultSet objResults = mDatabase.Execute(strSQL);
          if (objResults.Rows.Count > 0)
          {
            strSeriesId = DatabaseUtility.Get(objResults, 0, DBOnlineEpisode.cSeriesID);
            AddToCache(DBOnlineEpisode.cTableName, strCompositeId, objResults);
          }
        }
        return strSeriesId;
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
      return string.Empty;
    }

    public bool GetSeriesWatchedStatus(string strSeriesId)
    {
      try
      {
        string strSeriesWatchedStatus = string.Empty;
        if (CheckCache(DBOnlineSeries.cTableName, strSeriesId, DBOnlineSeries.cUnwatchedItems, ref strSeriesWatchedStatus) == false)
        {
          string strSQL = String.Format("SELECT * FROM {0} WHERE {1} = '{2}'", DBOnlineSeries.cTableName, DBOnlineSeries.cID, strSeriesId);
          Log.Debug("WatchedSynchronizer: (GetSeriesWatchedStatus) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          SQLiteResultSet objResults = mDatabase.Execute(strSQL);
          if (objResults.Rows.Count > 0)
          {
            strSeriesWatchedStatus = DatabaseUtility.Get(objResults, 0, DBOnlineSeries.cUnwatchedItems);
            AddToCache(DBOnlineSeries.cTableName, strSeriesId, objResults);
          }
        }
        return (strSeriesWatchedStatus == "0");
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
      return true;
    }

    public void SetSeriesWatchedStatus(string strSeriesId, bool bolWatchedStatus)
    {
      try
      {
        int intUnwatchedItems;
        if (bolWatchedStatus == true)
        {
          intUnwatchedItems = 0;
        }
        else
        {
          intUnwatchedItems = 1;
        }
        string strSeriesWatchedStatus = intUnwatchedItems.ToString();
        if (CheckCache(DBOnlineSeries.cTableName, strSeriesId, DBOnlineSeries.cUnwatchedItems, ref strSeriesWatchedStatus) == false)
        {
          string strSQL = String.Format("UPDATE {0} SET {1} = {2} WHERE {3} = '{4}'", DBOnlineSeries.cTableName, DBOnlineSeries.cUnwatchedItems, intUnwatchedItems, DBOnlineSeries.cID, strSeriesId);
          Log.Debug("WatchedSynchronizer: (SetSeriesWatchedStatus) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          mDatabase.Execute(strSQL);
        }
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    public int GetSeriesUnwatchedEpisodes(string strSeriesId)
    {
      try
      {
        string strSeriesUnwatchedEpisodes = string.Empty;
        if (CheckCache(DBOnlineSeries.cTableName, strSeriesId, DBOnlineSeries.cEpisodesUnWatched, ref strSeriesUnwatchedEpisodes) == false)
        {
          string strSQL = String.Format("SELECT * FROM {0} WHERE {1} = '{2}'", DBOnlineSeries.cTableName, DBOnlineSeries.cID, strSeriesId);
          Log.Debug("WatchedSynchronizer: (GetSeriesUnwatchedEpisodes) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          SQLiteResultSet objResults = mDatabase.Execute(strSQL);
          if (objResults.Rows.Count > 0)
          {
            strSeriesUnwatchedEpisodes = DatabaseUtility.Get(objResults, 0, DBOnlineSeries.cEpisodesUnWatched);
            AddToCache(DBOnlineSeries.cTableName, strSeriesId, objResults);
          }
        }
        int intUnwatchedEpisodes;
        Int32.TryParse(strSeriesUnwatchedEpisodes, out intUnwatchedEpisodes);
        return intUnwatchedEpisodes;
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
      return 0;
    }

    public void SetSeriesUnwatchedEpisodes(string strSeriesId, int intUnwatchedEpisodes)
    {
      try
      {
        string strSeriesUnwatchedEpisodes = intUnwatchedEpisodes.ToString();
        if (CheckCache(DBOnlineSeries.cTableName, strSeriesId, DBOnlineSeries.cEpisodesUnWatched, ref strSeriesUnwatchedEpisodes) == false)
        {
          string strSQL = String.Format("UPDATE {0} SET {1} = {2} WHERE {3} = '{4}'", DBOnlineSeries.cTableName, DBOnlineSeries.cEpisodesUnWatched, intUnwatchedEpisodes, DBOnlineSeries.cID, strSeriesId);
          Log.Debug("WatchedSynchronizer: (SetSeriesUnwatchedEpisodes) SQL statement '" + strSQL + "' is going to be executed in database '" + mDatabase.DatabaseName + "'.");
          mDatabase.Execute(strSQL);
        }
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: TVSeries database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    #endregion

    #region Private methods

    private void Open()
    {
      mCache = new Dictionary<string, SQLiteResultCacheItem>();
      Log.Info("WatchedSynchronizer: Opening tv series database " + mDatabaseFile);
      try
      {
        if (mDatabase != null)
        {
          Log.Info("WatchedSynchronizer: Tv series database already opened.");
          return;
        }
        mDatabase = new SQLiteClient(mDatabaseFile);
        DatabaseUtility.SetPragmas(mDatabase);
        Log.Info("WatchedSynchronizer: Tv series database opened.");
      }
      catch (Exception ex)
      {
        Log.Error("WatchedSynchronizer: Tv series database exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    private bool CheckCache(string strTableName, string strKey, string strField, ref string strValue)
    {
      if (mCache.ContainsKey(strTableName) == true)
      {
        if (mCache[strTableName].Key == strKey && mCache[strTableName].IsDirty == false)
        {
          if (strValue == string.Empty)
          {
            strValue = (string)DatabaseUtility.Get(mCache[strTableName].ResultSet, 0, strField);
            return true;
          }
          else if (strValue == (string)DatabaseUtility.Get(mCache[strTableName].ResultSet, 0, strField))
          {
            return true;
          }
          else
          {
            mCache[strTableName].IsDirty = true;
          }
        }
      }
      return false;
    }

    private void AddToCache(string strTableName, string strKey, SQLiteResultSet objResultSet)
    {
      if (mCache.ContainsKey(strTableName) == true)
      {
        mCache.Remove(strTableName);
      }
      SQLiteResultCacheItem objCacheItem = new SQLiteResultCacheItem(strKey, objResultSet);
      mCache.Add(strTableName, objCacheItem);
    }

    #endregion

  }
}