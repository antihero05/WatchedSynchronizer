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
using System.Collections.ObjectModel;
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;
using MediaPortal.Player;
using MediaPortal.Video.Database;
using MediaPortal.Configuration;
using MediaPortal.Profile;
using WindowPlugins.GUITVSeries;
using WatchedSynchronizer.Configuration;


namespace WatchedSynchronizer
{
  [PluginIcons("WatchedSynchronizer.Resources.WatchedSynchronizerEnabled.png", "WatchedSynchronizer.Resources.WatchedSynchronizerDisabled.png")]

  public class WatchedSynchronizer : ISetupForm, IPlugin
  {

    #region Enumerations

    /// <summary>
    /// Enumeration of media types which are used to determine the correct databases to be updated.
    /// </summary>

    public enum MediaType
    {
      MPTVSeries,
      MPVideo,
      Unknown
    };

    #endregion

    #region Declaration

    /// <summary>
    /// Instantiate attributes on object construction.
    /// mCurrentMediaType is set to "Unknown" because on plugin startup no playback should be running.
    /// </summary>

    private WatchedSynchronizerConfiguration mConfiguration;
    private Collection<AdditionalVideoDatabase> mVideoDatabases = new Collection<AdditionalVideoDatabase>();
    private int mWatchedPercentageVideoDatabase;
    private bool mMarkWatchedFilesVideoDatabase;
    private Collection<AdditionalTVSeriesDatabase> mTVSeriesDatabases = new Collection<AdditionalTVSeriesDatabase>();
    private int mWatchedPercentageTVSeriesDatabase;
    private WatchedSynchronizer.MediaType mCurrentMediaType = MediaType.Unknown;

    #endregion

    #region ISetupForm Members

    /// <summary>
    /// This region is providing the necessary attributes/methods required by Mediaportal for the plugin configuration.
    /// </summary>
    
    public string Author()
    {
      return "Max Wimmelbacher";
    }

    public bool CanEnable()
    {
      return true;
    }

    public string Description()
    {
      return "Process plugin to update the watched information for media across several databases.";
    }

    public bool DefaultEnabled()
    {
      return false;
    }

    public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
    {
      strButtonText = string.Empty;
      strButtonImage = string.Empty;
      strButtonImageFocus = string.Empty;
      strPictureImage = string.Empty;
      return false;
    }

    public int GetWindowId()
    {
      return -1;
    }

    public bool HasSetup()
    {
      return true;
    }

    public string PluginName()
    {
      return "WatchedSynchronizer";
    }

    public void ShowPlugin()
    {
      var Config = new WatchedSynchronizerConfig();
      Config.Show();
    }

    #endregion
   
    #region IPlugin Members

    /// <summary>
    /// This region is providing the necessary methods required by Mediaportal for interaction with the plugin.
    /// Configuration Loading and hooking on the events necessary to trigger plugin actions is done here.
    /// </summary>
    
    public void Start()
    {
      Log.Info("WatchedSynchronizer: Starting");
      LoadConfiguration();
      GUIWindowManager.Receivers += new SendMessageHandler(GUIWindowManager_Receivers);
      g_Player.PlayBackEnded += OnPlayBackEnded;
      g_Player.PlayBackChanged += OnPlayBackChanged;
      g_Player.PlayBackStopped += OnPlayBackStopped;
    }

    public void Stop()
    {
      Log.Info("WatchedSynchronizer: Stopping");
      GUIWindowManager.Receivers -= new SendMessageHandler(GUIWindowManager_Receivers);
      g_Player.PlayBackEnded -= OnPlayBackEnded;
      g_Player.PlayBackChanged -= OnPlayBackChanged;
      g_Player.PlayBackStopped -= OnPlayBackStopped;
    }

    #endregion

    #region Mediaportal.GUI.Library.GUIWindowManager events

    /// <summary>
    /// The method GUIWindowManager_Receivers is listening for messages indicating that a media playback started and by whom.
    /// This information is used to determine which database needs to be updated after the media playback stopped.
    /// </summary>

    private void GUIWindowManager_Receivers(GUIMessage message)
    {
      if (message.Message == GUIMessage.MessageType.GUI_MSG_PLAYBACK_STARTED)
      {
        Log.Debug("WatchedSynchronizer: Encountered message 'GUI_MSG_PLAYBACK_STARTED' for WindowId '" + GUIWindowManager.GetPreviousActiveWindow() + "'.");
        switch (GUIWindowManager.GetPreviousActiveWindow())
        {
          case 25:
          case 2003:
            mCurrentMediaType = MediaType.MPVideo;
            break;
          case 9811:
            mCurrentMediaType = MediaType.MPTVSeries;
            break;
          default:
            {
              mCurrentMediaType = MediaType.Unknown;
              Log.Warn("WatchedSynchronizer: Encountered WindowId '" + GUIWindowManager.GetPreviousActiveWindow() + "' which has no mediatype assigned.");
              break;
            }
        }
      }
    }

    #endregion

    #region MediaPortal.Player.g_player events

    /// <summary>
    /// This region defindes the methods to be run on triggering of hooked events.
    /// Based on the object which started the media playback it is decided which update method needs to be called.
    /// </summary>

    private void OnPlayBackEnded(g_Player.MediaType enuType, string strFileName)
    {
      if (enuType == g_Player.MediaType.Video)
      {
        Log.Debug("WatchedSynchronizer: 'g_Player.PlayBackEnded' event with media type '" + mCurrentMediaType + "' occured for file '" + strFileName + "'.");
        switch (mCurrentMediaType)
        {
          case MediaType.MPVideo:
            {
              OnPlayBackEndedVideoDataBases(strFileName);
              break;
            }
          case MediaType.MPTVSeries:
            {
              OnPlayBackEndedTVSeriesDataBases(strFileName);
              break;
            }
        }
      }
    }

    private void OnPlayBackChanged(g_Player.MediaType enuType, int intStopTime, string strFileName)
    {
      if (enuType == g_Player.MediaType.Video)
      {
        Log.Debug("WatchedSynchronizer: 'g_Player.PlayBackChanged' event with media type '" + mCurrentMediaType + "' occured for file '" + strFileName + "'.");
        switch (mCurrentMediaType)
        {
          case MediaType.MPVideo:
            {
              OnPlayBackStoppedOrChangedVideoDatabases(enuType, intStopTime, strFileName);
              break;
            }
          case MediaType.MPTVSeries:
            {
              OnPlayBackStoppedOrChangedTVSeriesDatabases(enuType, intStopTime, strFileName);
              break;
            }
        }
      }
    }

    private void OnPlayBackStopped(g_Player.MediaType enuType, int intStopTime, string strFileName)
    {
      if (enuType == g_Player.MediaType.Video)
      {
        Log.Debug("WatchedSynchronizer: 'g_Player.PlayBackStopped' event with media type '" + mCurrentMediaType + "' occured for file '" + strFileName + "'.");
        switch (mCurrentMediaType)
        {
          case MediaType.MPVideo:
            {
              OnPlayBackStoppedOrChangedVideoDatabases(enuType, intStopTime, strFileName);
              break;
            }
          case MediaType.MPTVSeries:
            {
              OnPlayBackStoppedOrChangedTVSeriesDatabases(enuType, intStopTime, strFileName);
              break;
            }
        }
      }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// The method LoadConfiguration is called on plugin start and is deserializing the saved configuration.
    /// Database objects are created based on the configuration and stored in collections for the process lifetime.
    /// </summary>

    private void LoadConfiguration()
    {

      //Loading the plugins configuration.

      MPSettings objSettings = MPSettings.Instance;
      string strConfiguration = objSettings.GetValueAsString("watchedsynchronizer", "configuration", string.Empty);
      if (string.IsNullOrEmpty(strConfiguration))
      {
        mConfiguration.mConfigurationEntries = new List<WatchedSynchronizerConfigurationEntry>();
      }
      else
      {
        mConfiguration = Utils.Deserialize(strConfiguration);
      }

      //Loading the configured values when a media item is considered watched.

      mWatchedPercentageVideoDatabase = objSettings.GetValueAsInt("movies", "playedpercentagewatched", 95);
      mMarkWatchedFilesVideoDatabase = objSettings.GetValueAsBool("movies", "markwatched", true);
      mWatchedPercentageTVSeriesDatabase = (int)DBOption.GetOptions(DBOption.cWatchedAfter);

      //Create object collections containing all configured databases which needs to be updated.

      foreach (WatchedSynchronizerConfigurationEntry objLoop in mConfiguration.mConfigurationEntries)
      {
        if (objLoop.mEnabled == true)
        {
          switch (objLoop.mDatabaseType)
          {
            case "Video database":
              AdditionalVideoDatabase objVideoDatabase = new AdditionalVideoDatabase(objLoop.mDatabasePath);
              objVideoDatabase.Dispose();
              mVideoDatabases.Add(objVideoDatabase);
              break;
            case "TVSeries database":
              AdditionalTVSeriesDatabase objTVSeriesDatabase = new AdditionalTVSeriesDatabase(objLoop.mDatabasePath);
              objTVSeriesDatabase.Dispose();
              mTVSeriesDatabases.Add(objTVSeriesDatabase);
              break;
          }
        }
      }
    }

    #region Video databases

    /// <summary>
    /// Methods in this region are run if the media type is "MPVideo".
    /// The defined methods run the necessary commands to update all configured video databases.
    /// </summary>

    //Method OnPlayBackEndedVideoDataBases is run when movie was watched to the end.

    private void OnPlayBackEndedVideoDataBases(string strFileName)
    {
      bool bolStacked = false;
      foreach (AdditionalVideoDatabase objLoop in mVideoDatabases)
      {
        objLoop.ReOpen();
        bool bolUpdated = false;
        ArrayList alsFiles = new ArrayList();
        int intDuration = 0;
        int intTotalDuration = 0;
        int intPlayTimePercentage = 0;

        //Get MovieID and all its correlated media file(s).

        int intMovieId = objLoop.GetMovieId(strFileName);
        if (intMovieId == -1)
        {
          Log.Warn("WatchedSynchronizer: File '" + strFileName + "' is not available in database '" + objLoop.DatabaseName + "'.");
          return;
        }
        else
        {
          objLoop.GetFilesForMovie(intMovieId, ref alsFiles);
        }

        //Check if MovieID consists of several media files.

        if (alsFiles.Count <= 0)
        {
          Log.Warn("WatchedSynchronizer: File '" + strFileName + "' is not available in database '" + objLoop.DatabaseName + "'.");
          return;
        }
        else if (alsFiles.Count > 1)
        {
          bolStacked = true;
        }

        //Calculates the total duration of all media files of the MovieID.

        foreach (string strLoop in alsFiles)
        {
          intTotalDuration += objLoop.GetVideoDuration(objLoop.GetFileId(strLoop));
        }

        //Resets the MovieStopTime and the ResumeData for the media file which was last played.

        for (int intLoop = 0; intLoop < alsFiles.Count; intLoop++)
        {
          string strFilePath = (string)alsFiles[intLoop];
          byte[] bteResumeData = null;
          int intFileId = objLoop.GetFileId(strFilePath);
          if (intFileId < 0)
          {
            break;
          }
          objLoop.GetMovieStopTimeAndResumeData(intFileId, out bteResumeData, g_Player.SetResumeBDTitleState);
          objLoop.SetMovieStopTimeAndResumeData(intFileId, 0, bteResumeData, g_Player.SetResumeBDTitleState);
          bolUpdated = true;
        }

        //Calculates the Watched percentage of the movie based on all media items correlated.

        if (bolStacked && intTotalDuration != 0)
        {
          for (int i = 0; i < alsFiles.Count; i++)
          {
            int intFileId = objLoop.GetFileId((string)alsFiles[i]);
            if (strFileName != (string)alsFiles[i])
            {
              intDuration += objLoop.GetVideoDuration(intFileId);
              continue;
            }
            intPlayTimePercentage = (int)(100 * (intDuration + g_Player.Player.CurrentPosition) / intTotalDuration);
            break;
          }
        }
        else
        {
          intPlayTimePercentage = 100;
        }

        //Updates the Watched status based on the configuration done for the video database.

        if (mMarkWatchedFilesVideoDatabase)
        {
          IMDBMovie objIMDBMovie = new IMDBMovie();
          objLoop.GetMovieInfoById(intMovieId, ref objIMDBMovie);
          if (!objIMDBMovie.IsEmpty)
          {

            //Updates the Watched status for all media files based on the configuration done for the video database. 

            if (intPlayTimePercentage >= mWatchedPercentageVideoDatabase)
            {
              objIMDBMovie.Watched = 1;
              objLoop.SetWatched(objIMDBMovie);
              objLoop.SetMovieWatchedStatus(intMovieId, true, intPlayTimePercentage);
              objLoop.MovieWatchedCountIncrease(intMovieId);
              objLoop.SetDateWatched(objIMDBMovie);
              bolUpdated = true;
            }
            else
            {
              int intPercent = 0;
              int intTimesWatched = 0;
              bool bolWatched = objLoop.GetMovieWatchedStatus(intMovieId, out intPercent, out intTimesWatched);
              objLoop.SetMovieWatchedStatus(intMovieId, bolWatched, intPlayTimePercentage);
              bolUpdated = true;
            }
          }
        }

        //Check if update was successfull and write to media portal log.

        if (bolUpdated == true)
        {
          Log.Info("WatchedSynchronizer: Information for file '" + strFileName + "' was updated in database '" + objLoop.DatabaseName + "'.");
        }
        objLoop.Dispose();
      }
    }


    //Method OnPlayBackStoppedOrChangedVideoDatabases is run when movie playback stopped before the end.

    private void OnPlayBackStoppedOrChangedVideoDatabases(g_Player.MediaType enuType, int intStopTime, string strFileName)
    {
      bool bolStacked = false;
      foreach (AdditionalVideoDatabase objLoop in mVideoDatabases)
      {
        objLoop.ReOpen();
        bool bolUpdated = false;
        ArrayList alsFiles = new ArrayList();
        int intDuration = 0;
        int intTotalDuration = 0;
        int intPlayTimePercentage = 0;

        //Get MovieID and all its correlated media file(s).

        int intMovieId = objLoop.GetMovieId(strFileName);
        if (intMovieId == -1)
        {
          Log.Warn("WatchedSynchronizer: File '" + strFileName + "' is not available in database '" + objLoop.DatabaseName + "'.");
          return;
        }
        else
        {
          objLoop.GetFilesForMovie(intMovieId, ref alsFiles);
        }

        //Check if MovieID consists of several media files.

        if (alsFiles.Count <= 0)
        {
          Log.Warn("WatchedSynchronizer: File '" + strFileName + "' is not available in database '" + objLoop.DatabaseName + "'.");
          return;
        }
        else if (alsFiles.Count > 1)
        {
          bolStacked = true;
        }

        //Calculates the total duration of all media files of the MovieID.

        foreach (string strLoop in alsFiles)
        {
          intTotalDuration += objLoop.GetVideoDuration(objLoop.GetFileId(strLoop));
        }

        //Calculates the Watched percentage of the movie based on all media items correlated.

        if (bolStacked && intTotalDuration != 0)
        {
          for (int intLoop = 0; intLoop < alsFiles.Count; intLoop++)
          {
            if (g_Player.CurrentFile != (string)alsFiles[intLoop])
            {
              intDuration += objLoop.GetVideoDuration(objLoop.GetFileId((string)alsFiles[intLoop]));
              continue;
            }
            intPlayTimePercentage = (100 * (intDuration + intStopTime) / intTotalDuration);
            break;
          }
        }
        else
        {
          if (g_Player.Player.Duration >= 1)
          {
            intPlayTimePercentage = (int)Math.Ceiling((intStopTime / g_Player.Player.Duration) * 100);
          }
        }

        //Sets the Watched status and correlated for the media file which was last played.

        for (int intLoop = 0; intLoop < alsFiles.Count; intLoop++)
        {
          string strFilePath = (string)alsFiles[intLoop];
          int intFileId = objLoop.GetFileId(strFilePath);
          intMovieId = objLoop.GetMovieId(strFilePath);

          //Check if a DVD/Blueray was played and set the Watched status and correlated according to that.

          if (g_Player.IsDVDMenu)
          {
            objLoop.SetMovieStopTimeAndResumeData(intFileId, 0, null, g_Player.SetResumeBDTitleState);
            objLoop.SetMovieWatchedStatus(intMovieId, true, 100);
            objLoop.MovieWatchedCountIncrease(intMovieId);
          }

          //Check if the media file currently played is the one in the loop and set the Watched status and correlated according to that.

          else if ((strFileName.Trim().ToLowerInvariant().Equals(strFilePath.Trim().ToLowerInvariant())) && (intStopTime > 0))
          {
            byte[] bteResumeData = null;
            g_Player.Player.GetResumeState(out bteResumeData);
            objLoop.GetMovieStopTimeAndResumeData(intFileId, out bteResumeData, g_Player.SetResumeBDTitleState);
            objLoop.SetMovieStopTimeAndResumeData(intFileId, intStopTime, bteResumeData, g_Player.SetResumeBDTitleState);

            //Updates the Watched status for all media files based on the configuration done for the video database.

            if (intPlayTimePercentage >= mWatchedPercentageVideoDatabase)
            {
              objLoop.SetMovieWatchedStatus(intMovieId, true, intPlayTimePercentage);
              objLoop.MovieWatchedCountIncrease(intMovieId);
              bolUpdated = true;
            }
            else
            {
              int intPercent = 0;
              int intTimesWatched = 0;
              bool bolWatched = objLoop.GetMovieWatchedStatus(intMovieId, out intPercent, out intTimesWatched);
              objLoop.SetMovieWatchedStatus(intMovieId, bolWatched, intPlayTimePercentage);
              bolUpdated = true;
            }
          }
          else
          {
            objLoop.DeleteMovieStopTime(intFileId);
          }
        }

        //Updates the Watched status based on the configuration done for the video database.

        if (mMarkWatchedFilesVideoDatabase)
        {
          IMDBMovie objIMDBMovie = new IMDBMovie();
          objLoop.GetMovieInfoById(intMovieId, ref objIMDBMovie);
          if (!objIMDBMovie.IsEmpty)
          {
            if (intPlayTimePercentage >= mWatchedPercentageVideoDatabase)
            {
              objIMDBMovie.Watched = 1;
              objIMDBMovie.DateWatched = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
              objLoop.SetMovieInfoById(intMovieId, ref objIMDBMovie);
              bolUpdated = true;
            }
          }
        }

        //Check if update was successfull and write to media portal log.

        if (bolUpdated == true)
        {
          Log.Info("WatchedSynchronizer: Information for file '" + strFileName + "' was updated in database '" + objLoop.DatabaseName + "'.");
        }
        objLoop.Dispose();
      }
    }

    #endregion

    #region TVSeries databases

    /// <summary>
    /// Methods in this region are run if the media type is "MPTVSeries".
    /// The defined methods run the necessary commands to update all configured video databases.
    /// </summary>

    //Method OnPlayBackEndedTVSeriesDataBases is run when tvseries was watched to the end.

    private void OnPlayBackEndedTVSeriesDataBases(string strFileName)
    {
      int intPlayTimePercentage = 100;
      foreach (AdditionalTVSeriesDatabase objLoop in mTVSeriesDatabases)
      {
        objLoop.ReOpen();
        bool bolUpdated = false;
        ArrayList alsCompositeIds = new ArrayList();

        //Get CompositeId(s).

        string strCompositeId = objLoop.GetCompositeId(strFileName);
        if (strCompositeId == string.Empty)
        {
          Log.Warn("WatchedSynchronizer: File '" + strFileName + "' is not available in database '" + objLoop.DatabaseName + "'.");
          return;
        }
        else
        {
          objLoop.GetCompositeIdsForEpisode(strCompositeId, ref alsCompositeIds);
        }
        if (alsCompositeIds.Count <= 0)
        {
          Log.Warn("WatchedSynchronizer: File '" + strFileName + "' is not available in database '" + objLoop.DatabaseName + "'.");
          return;
        }

        //Updates the Watched status for all CompositeIds.

        for (int intLoop = 0; intLoop < alsCompositeIds.Count; intLoop++)
        {
          strCompositeId = (string)alsCompositeIds[intLoop];
          bool bolWatched = false;
          string strWatchedDate;
          int intStopTime;

          //Check if episode was already watched.

          objLoop.GetEpisodeWatchedStatus(strCompositeId, out bolWatched, out strWatchedDate, out intStopTime);

          //Updates the Watched status based on the configuration done for the tvseries database.

          if (intPlayTimePercentage >= mWatchedPercentageTVSeriesDatabase)
          {
            if (bolWatched == false)
            {

              //Update the Watched status for the episodes and correlated it is unwatched.

              objLoop.SetEpisodeWatchedStatus(strCompositeId, true, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 0, true);
              string strSeasonId;
              bool bolSeasonWatched = false;
              objLoop.GetSeasonWatchedStatus(strCompositeId, out strSeasonId, out bolSeasonWatched);
              if (bolSeasonWatched == false)
              {

                //Update the Unwatched episodes count for the season if it is unwatched.

                objLoop.SeasonUnWatchedCountDecrease(strSeasonId);
                string strSeriesId;
                bool bolSeriesWatched = false;
                objLoop.GetSeriesWatchedStatus(strCompositeId, out strSeriesId, out bolSeriesWatched);
                if (bolSeriesWatched == false)
                {

                  //Update the Unwatched episodes count for the series if it is unwatched.

                  objLoop.SeriesUnWatchedCountDecrease(strSeriesId);
                }
              }
            }
            else
            {
              objLoop.SetEpisodeWatchedStatus(strCompositeId, bolWatched, strWatchedDate, 0);
            }
            bolUpdated = true;
          }
          else
          {
            objLoop.SetEpisodeWatchedStatus(strCompositeId, bolWatched, strWatchedDate, 0);

            bolUpdated = true;
          }
        }

        //Check if update was successfull and write to media portal log.

        if (bolUpdated == true)
        {
          Log.Info("WatchedSynchronizer: Information for file '" + strFileName + "' was updated in database '" + objLoop.DatabaseName + "'.");
        }
        objLoop.Dispose();
      }
    }


    //Method OnPlayBackStoppedOrChangedTVSeriesDatabases is run when tvseries playback stopped before the end.

    private void OnPlayBackStoppedOrChangedTVSeriesDatabases(g_Player.MediaType enuType, int intStopTime, string strFileName)
    {
      foreach (AdditionalTVSeriesDatabase objLoop in mTVSeriesDatabases)
      {
        objLoop.ReOpen();
        bool bolUpdated = false;
        ArrayList alsCompositeIds = new ArrayList();
        int intPlayTimePercentage = 0;

        //Get CompositeId(s).

        string strCompositeId = objLoop.GetCompositeId(strFileName);
        if (strCompositeId == string.Empty)
        {
          Log.Warn("WatchedSynchronizer: File '" + strFileName + "' is not available in database '" + objLoop.DatabaseName + "'.");
          return;
        }
        else
        {
          objLoop.GetCompositeIdsForEpisode(strCompositeId, ref alsCompositeIds);
        }
        if (alsCompositeIds.Count <= 0)
        {
          Log.Warn("WatchedSynchronizer: File '" + strFileName + "' is not available in database '" + objLoop.DatabaseName + "'.");
          return;
        }

        //Calculates the Watched percentage of the episode.

        if (g_Player.Player.Duration >= 1)
        {
          intPlayTimePercentage = (int)Math.Ceiling((intStopTime / g_Player.Player.Duration) * 100);
        }

        //Updates the Watched status for all CompositeIds.

        for (int intLoop = 0; intLoop < alsCompositeIds.Count; intLoop++)
        {
          strCompositeId = (string)alsCompositeIds[intLoop];
          bool bolWatched = false;
          string strWatchedDate;
          int intStopTimeDummy;
          objLoop.GetEpisodeWatchedStatus(strCompositeId, out bolWatched, out strWatchedDate, out intStopTimeDummy);

          //Updates the Watched status based on the configuration done for the tvseries database.

          if (intPlayTimePercentage >= mWatchedPercentageTVSeriesDatabase)
          {
            if (bolWatched == false)
            {

              //Update the Watched status for the episodes and correlated it is unwatched.

              objLoop.SetEpisodeWatchedStatus(strCompositeId, true, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 0, true);
              string strSeasonId;
              bool bolSeasonWatched = false;
              objLoop.GetSeasonWatchedStatus(strCompositeId, out strSeasonId, out bolSeasonWatched);
              if (bolSeasonWatched == false)
              {

                //Update the Unwatched episodes count for the season if it is unwatched.

                objLoop.SeasonUnWatchedCountDecrease(strSeasonId);
                string strSeriesId;
                bool bolSeriesWatched = false;
                objLoop.GetSeriesWatchedStatus(strCompositeId, out strSeriesId, out bolSeriesWatched);
                if (bolSeriesWatched == false)
                {

                  //Update the Unwatched episodes count for the series if it is unwatched.

                  objLoop.SeriesUnWatchedCountDecrease(strSeriesId);
                }
              }
            }
            else
            {
              objLoop.SetEpisodeWatchedStatus(strCompositeId, true, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 0);
            }
            bolUpdated = true;
          }
          else
          {
            objLoop.SetEpisodeWatchedStatus(strCompositeId, bolWatched, strWatchedDate, intStopTime);
            bolUpdated = true;
          }
        }

        //Check if update was successfull and write to media portal log.

        if (bolUpdated == true)
        {
          Log.Info("WatchedSynchronizer: Information for file '" + strFileName + "' was updated in database '" + objLoop.DatabaseName + "'.");
        }
        objLoop.Dispose();
      }
    }

    #endregion

    #endregion

  }
}