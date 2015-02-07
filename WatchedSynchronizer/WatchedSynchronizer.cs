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
using System.Threading;
using System.Collections.ObjectModel;
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;
using MediaPortal.Player;
using MediaPortal.Video.Database;
using MediaPortal.Configuration;
using MediaPortal.Profile;
using WindowPlugins.GUITVSeries;
using WatchedSynchronizer.Configuration;
using System.Collections.Concurrent;


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

    public enum EventType
    {
        StopOrChanged,
        Ended,
        Toggle
    };

    #endregion

    #region Declaration

    /// <summary>
    /// Instantiate attributes on object construction.
    /// mCurrentMediaType is set to "Unknown" because on plugin startup no playback should be running.
    /// </summary>

    private WatchedSynchronizerConfiguration mConfiguration;
    private Collection<AdditionalVideoDatabase> mVideoDatabases = new Collection<AdditionalVideoDatabase>();
    private static int mWatchedPercentageVideoDatabase;
    private static bool mMarkWatchedFilesVideoDatabase;
    private static int mWatchedPercentageTVSeriesDatabase;
    private Collection<AdditionalTVSeriesDatabase> mTVSeriesDatabases = new Collection<AdditionalTVSeriesDatabase>();
    private WatchedSynchronizer.MediaType mCurrentMediaType = MediaType.Unknown;


    static ConcurrentQueue<WatchedStateEvent> mWatchStateEventQueue = new ConcurrentQueue<WatchedStateEvent>();

    // Create the thread object. This does not start the thread.

    private WorkerThread mWorkerObject = new WorkerThread();
    private Thread mWorkerThread = null;

    private struct gPlayer
    {
        public int SetResumeBDTitleState;
        public double CurrentPosition;
        public string CurrentFile;
        public double Duration;
        public bool IsDVDMenu;
        public byte[] bteResumeData;
    };

    private struct WatchedStateEvent
    {
        public EventType EventType;
        public DateTime dtNextCheck;
        public object objDB;
        public MediaType MediaType;
        public string strFileName;
        public int intStopTime;
        public bool bolWatched;
        public bool bolUse_gPlayer;
        public gPlayer gPlayer;
    };

    private class WorkerThread
    {

      // Volatile is used as hint to the compiler that this data member will be accessed by multiple threads. 

      private volatile bool bolStop;

      // This method will be called when the thread is started. 

      public void DoWork()
      {
        WatchedStateEvent objWatchedStateEvent;
        int intSleep = 10;
        int intEventCount = 0;
        int intResult;
        TimeSpan tsSleepInterval;

        Log.Debug("WatchedSynchronizer: mWorkerThread started!");
        while (bolStop == false)
        {
          while (mWatchStateEventQueue.TryDequeue(out objWatchedStateEvent))
          {
            intEventCount = intEventCount + 1;
            if (objWatchedStateEvent.dtNextCheck <= DateTime.Now)
            {

              //Start processing the queue.

              Log.Debug("WatchedSynchronizer: mWorkerThread: New event to process: " +
                          "EventType: '" + objWatchedStateEvent.EventType.ToString() + "' - " +
                          "dtNextCheck: '" + objWatchedStateEvent.dtNextCheck.ToString("HH:mm:ss.fff tt") + "' - " +
                          "MediaType: '" + objWatchedStateEvent.MediaType.ToString() + "' - " +
                          "strFileName: '" + objWatchedStateEvent.strFileName + "' - " +
                          "intStopTime: '" + objWatchedStateEvent.intStopTime.ToString() + "' - " +
                          "bolWatched: '" + objWatchedStateEvent.bolWatched.ToString() + "'"
                          );
              if (objWatchedStateEvent.bolUse_gPlayer)
              {
                Log.Debug("WatchedSynchronizer: mWorkerThread: gPlayer data:" +

                            "gPlayer.SetResumeBDTitleState: '" + objWatchedStateEvent.gPlayer.SetResumeBDTitleState.ToString() + "' - " +
                            "gPlayer.CurrentPosition: '" + objWatchedStateEvent.gPlayer.CurrentPosition.ToString() + "' - " +
                            "gPlayer.CurrentFile: '" + objWatchedStateEvent.gPlayer.CurrentFile.ToString() + "' - " +
                            "gPlayer.Duration: '" + objWatchedStateEvent.gPlayer.Duration.ToString() + "' - " +
                            "gPlayer.IsDVDMenu: '" + objWatchedStateEvent.gPlayer.IsDVDMenu.ToString() + "'" +
                            "gPlayer.bteResumeData: '" + objWatchedStateEvent.gPlayer.bteResumeData.ToString()
                            );
              }

              //Process the "WatchedStateEvent".

              intResult = 0;
              switch (objWatchedStateEvent.MediaType)
              {
                case MediaType.MPTVSeries:
                  intResult = ProcessMPTVSeriesWatchedStateEvent(objWatchedStateEvent);
                  break;

                case MediaType.MPVideo:
                  intResult = ProcessMPVideoWatchedStateEvent(objWatchedStateEvent);
                  break;

                default:
                  Log.Debug("WatchedSynchronizer: mWorkerThread: dropped event with unknown MediaType!");
                  break;
              }

              //Check the result and reschedule/retry if suited.

              if (intResult < 0)
              {

                //We should reschedule/retray the event

                objWatchedStateEvent.dtNextCheck = DateTime.Now.AddSeconds(5);   //Retry in 5 Seconds
                mWatchStateEventQueue.Enqueue(objWatchedStateEvent);  //Event should be resheduled
              }

            }
            else
            {
              //Event should be resheduled, readd it to queue
              mWatchStateEventQueue.Enqueue(objWatchedStateEvent);
            }
            //calculate complete millisconds to sleep
            if (objWatchedStateEvent.dtNextCheck > DateTime.Now)
            {
              tsSleepInterval = objWatchedStateEvent.dtNextCheck - DateTime.Now;
              intSleep = (int)tsSleepInterval.TotalMilliseconds;
            }
          }

          //Minimal sleep is 10ms to give other proceses CPU time. When queue is empty sleep is 100ms.

          if (intEventCount == 0)
          {
            intSleep = 100;
          }

          if (intSleep < 10)
          {
            intSleep = 10;
          }
          Thread.Sleep(intSleep);
        }
        Log.Debug("WatchedSynchronizer: WorkerThread: terminating gracefully.");
      }
      public void RequestStop()
      {
        bolStop = true;
      }
    }

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
      GUIWindowManager.Receivers += new SendMessageHandler(OnGUIMessageReceived);
      TVSeriesPlugin.ToggleWatched += new TVSeriesPlugin.ToggleWatchedEventDelegate(OnTVSeriesToggledWatched);
      g_Player.PlayBackEnded += OnPlayBackEnded;
      g_Player.PlayBackChanged += OnPlayBackChanged;
      g_Player.PlayBackStopped += OnPlayBackStopped;

      //Create the mWorkerThread object.

      mWorkerThread = new Thread(mWorkerObject.DoWork);

      // Start the worker thread.

      mWorkerThread.Start();
      Log.Info("WatchedSynchronizer: Started!");
    }

    public void Stop()
    {
      Log.Info("WatchedSynchronizer: Stopping");
      GUIWindowManager.Receivers -= new SendMessageHandler(OnGUIMessageReceived);
      TVSeriesPlugin.ToggleWatched -= new TVSeriesPlugin.ToggleWatchedEventDelegate(OnTVSeriesToggledWatched);
      g_Player.PlayBackEnded -= OnPlayBackEnded;
      g_Player.PlayBackChanged -= OnPlayBackChanged;
      g_Player.PlayBackStopped -= OnPlayBackStopped;

      // Request that the worker thread stop itself:

      mWorkerObject.RequestStop();

      // Use the Join method to block the current thread until the object's thread terminates.

      mWorkerThread.Join();

      Log.Info("WatchedSynchronizer: Stopped!");
    }

    #endregion

    #region Mediaportal.GUI.Library.GUIWindowManager events

    /// <summary>
    /// The method OnGUIMessageReceived is listening for messages indicating that a media playback started and by whom.
    /// This information is used to determine which database needs to be updated after the media playback stopped.
    /// </summary>

    private void OnGUIMessageReceived(GUIMessage objMessage)
    {
      if (objMessage.Message == GUIMessage.MessageType.GUI_MSG_PLAYBACK_STARTED)
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
      WatchedStateEvent objWatchedStateEvent = new WatchedStateEvent();

      if (enuType == g_Player.MediaType.Video)
      {
        Log.Debug("WatchedSynchronizer: 'g_Player.PlayBackEnded' event with media type '" + mCurrentMediaType + "' occured for file '" + strFileName + "'.");

        //Create Watched Event

        objWatchedStateEvent.EventType = EventType.Ended;
        objWatchedStateEvent.MediaType = mCurrentMediaType;
        objWatchedStateEvent.strFileName = strFileName;
        objWatchedStateEvent.intStopTime = 0;
        objWatchedStateEvent.bolWatched = false;
        objWatchedStateEvent.bolUse_gPlayer = true;

        //Enqueue Event

        FireWatchedEvent(objWatchedStateEvent);
      }
    }

    private void OnPlayBackChanged(g_Player.MediaType enuType, int intStopTime, string strFileName)
    {
        WatchedStateEvent objWatchedStateEvent = new WatchedStateEvent();

        if (enuType == g_Player.MediaType.Video)
        {
            Log.Debug("WatchedSynchronizer: 'g_Player.PlayBackChanged' event with media type '" + mCurrentMediaType + "' occured for file '" + strFileName + "'.");

            //Create Watched Event

            objWatchedStateEvent.EventType = EventType.StopOrChanged;
            objWatchedStateEvent.MediaType = mCurrentMediaType;
            objWatchedStateEvent.strFileName = strFileName;
            objWatchedStateEvent.intStopTime = intStopTime;
            objWatchedStateEvent.bolWatched = false;
            objWatchedStateEvent.bolUse_gPlayer = true;

            //Enqueue Event

            FireWatchedEvent(objWatchedStateEvent);
        }
    }

    private void OnPlayBackStopped(g_Player.MediaType enuType, int intStopTime, string strFileName)
    {
        WatchedStateEvent objWatchedStateEvent = new WatchedStateEvent();

      if (enuType == g_Player.MediaType.Video)
      {
        Log.Debug("WatchedSynchronizer: 'g_Player.PlayBackStopped' event with media type '" + mCurrentMediaType + "' occured for file '" + strFileName + "'.");

        //Create Watched Event

        objWatchedStateEvent.EventType = EventType.StopOrChanged;
        objWatchedStateEvent.MediaType = mCurrentMediaType;
        objWatchedStateEvent.strFileName = strFileName;
        objWatchedStateEvent.intStopTime = intStopTime;
        objWatchedStateEvent.bolWatched = false;
        objWatchedStateEvent.bolUse_gPlayer = true;

        //Enqueue Event

        FireWatchedEvent(objWatchedStateEvent);
      }
    }

    #endregion

    #region WindowPlugins.GUITVSeries.TVSeriesPlugin events

    /// <summary>
    /// The method OnTVSeriesToggledWatched is listening for messages indicating that a episod was toggled watched using the menu in MediaPortal.
    /// This information is used to set the information related to Watched
    /// </summary>
    /// 

    private void OnTVSeriesToggledWatched(DBSeries objSeries, List<DBEpisode> lstDBEpisodes, bool bolWatched)
    {
      WatchedStateEvent objWatchedStateEvent = new WatchedStateEvent();

      foreach (DBEpisode objLoop in lstDBEpisodes)
      {
        Log.Debug("WatchedSynchronizer: The Watched Status for file '" + objLoop[DBEpisode.cFilename] + "' was toggled to '" + bolWatched + "',");

        //Create Watched Event

        objWatchedStateEvent.EventType = EventType.Toggle;
        objWatchedStateEvent.MediaType = MediaType.MPTVSeries;
        objWatchedStateEvent.strFileName = objLoop[DBEpisode.cFilename];
        objWatchedStateEvent.intStopTime = 0;
        objWatchedStateEvent.bolWatched = bolWatched;
        objWatchedStateEvent.bolUse_gPlayer = false;

        //Enqueue Event

        FireWatchedEvent(objWatchedStateEvent);

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

    /// <summary>
    /// FireWatchedEvent will be triggerd on any Mediaportal stop, change or toggle event.
    /// The needed data is added to the queue "mWatchStateEventQueue".
    /// </summary>
   
    private void FireWatchedEvent(WatchedStateEvent objWatchedStateEvent)
    {
        //Add needed g_Player data if objWatchedStateEvent.bolUse_gPlayer is set.
        if (objWatchedStateEvent.bolUse_gPlayer)
        {
            try
            {
                objWatchedStateEvent.gPlayer.SetResumeBDTitleState = g_Player.SetResumeBDTitleState;
                objWatchedStateEvent.gPlayer.CurrentPosition = g_Player.Player.CurrentPosition;
                objWatchedStateEvent.gPlayer.CurrentFile = g_Player.CurrentFile;
                objWatchedStateEvent.gPlayer.Duration = g_Player.Player.Duration;
                objWatchedStateEvent.gPlayer.IsDVDMenu = g_Player.IsDVDMenu;
                g_Player.Player.GetResumeState(out objWatchedStateEvent.gPlayer.bteResumeData);
            }
            catch (Exception ex)
            {
                Log.Error("WatchedSynchronizer: FireWatchedEvent:exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
            }
        }
        else
        {
            objWatchedStateEvent.gPlayer.SetResumeBDTitleState = 0;
            objWatchedStateEvent.gPlayer.CurrentPosition = 0;
            objWatchedStateEvent.gPlayer.CurrentFile = "";
            objWatchedStateEvent.gPlayer.Duration = 0;
            objWatchedStateEvent.gPlayer.IsDVDMenu = false;
            objWatchedStateEvent.gPlayer.bteResumeData = new byte[] {0x00 };
        }

        //DateTime when the item should be processed.

        objWatchedStateEvent.dtNextCheck = DateTime.Now;

        //Enqueue Event for every configured database.

        switch (objWatchedStateEvent.MediaType)
        {
            case MediaType.MPTVSeries:
                foreach (AdditionalTVSeriesDatabase objLoop in mTVSeriesDatabases)
                {
                    objWatchedStateEvent.objDB = objLoop;
                    mWatchStateEventQueue.Enqueue(objWatchedStateEvent);
                }
                break;

            case MediaType.MPVideo:
                foreach (AdditionalVideoDatabase objLoop in mVideoDatabases)
                {
                    objWatchedStateEvent.objDB = objLoop;
                    mWatchStateEventQueue.Enqueue(objWatchedStateEvent);
                }
                break;

            default:
                Log.Debug("WatchedSynchronizer: FireWatchedEvent: unknown MediaType");
                break;
        }
    }

    #region Video databases

    /// <summary>
    /// Method "ProcessMPVideoWatchedStateEvent" is run if the media type is "MPVideo" and a gplayer event was triggered.
    /// The method runs the necessary commands to update the video databases specified in the event.
    /// Return:
    /// 1 = Infos written to given DB -> Event could be cleared from queue
    /// <0 = DB is locked by other user -> Event should not be cleared from queue -> Retry
    /// 0 = other error (ex. file not found in db, file does not exists...) -> Event could be cleared from queue
    /// </summary>

    private static int ProcessMPVideoWatchedStateEvent(WatchedStateEvent objWatchedStateEvent)
    {
        bool bolStacked = false;
        AdditionalVideoDatabase objMPVideoDB = (AdditionalVideoDatabase)objWatchedStateEvent.objDB;

        objMPVideoDB.ReOpen();
        bool bolUpdated = false;
        ArrayList alsFiles = new ArrayList();
        int intDuration = 0;
        int intTotalDuration = 0;
        int intPlayTimePercentage = 0;

        //Get MovieID and all its correlated media file(s).

        int intMovieId = objMPVideoDB.GetMovieId(objWatchedStateEvent.strFileName);
        if (intMovieId == -1)
        {
            Log.Warn("WatchedSynchronizer: ProcessMPVideoWatchedStateEvent: File '" + objWatchedStateEvent.strFileName + "' is not available in database '" + objMPVideoDB.DatabaseName + "'.");
            objMPVideoDB.Dispose();
            return 0;
        }
        else
        {
            objMPVideoDB.GetFilesForMovie(intMovieId, ref alsFiles);
        }

        //Check if MovieID consists of several media files.

        if (alsFiles.Count <= 0)
        {
            Log.Warn("WatchedSynchronizer: ProcessMPVideoWatchedStateEvent: File '" + objWatchedStateEvent.strFileName + "' is not available in database '" + objMPVideoDB.DatabaseName + "'.");
            objMPVideoDB.Dispose();
            return 0;
        }
        else if (alsFiles.Count > 1)
        {
            bolStacked = true;
        }

        //Calculates the total duration of all media files of the MovieID.

        foreach (string strLoop in alsFiles)
        {
            intTotalDuration += objMPVideoDB.GetVideoDuration(objMPVideoDB.GetFileId(strLoop));
        }

        //Does various updates depending on the EventType.
        switch(objWatchedStateEvent.EventType)
        {
        
            case EventType.Ended:

                //Resets the MovieStopTime and the ResumeData for the media file which was last played.

                for (int intLoop = 0; intLoop < alsFiles.Count; intLoop++)
                {
                    string strFilePath = (string)alsFiles[intLoop];
                    byte[] bteResumeData = null;
                    int intFileId = objMPVideoDB.GetFileId(strFilePath);
                    if (intFileId < 0)
                    {
                        break;
                    }
                    objMPVideoDB.GetMovieStopTimeAndResumeData(intFileId, out bteResumeData, objWatchedStateEvent.gPlayer.SetResumeBDTitleState);
                    objMPVideoDB.SetMovieStopTimeAndResumeData(intFileId, 0, bteResumeData, objWatchedStateEvent.gPlayer.SetResumeBDTitleState);
                    bolUpdated = true;
                }

                //Calculates the Watched percentage of the movie based on all media items correlated.

                if (bolStacked && intTotalDuration != 0)
                {
                    for (int i = 0; i < alsFiles.Count; i++)
                    {
                        int intFileId = objMPVideoDB.GetFileId((string)alsFiles[i]);
                        if (objWatchedStateEvent.strFileName != (string)alsFiles[i])
                        {
                            intDuration += objMPVideoDB.GetVideoDuration(intFileId);
                            continue;
                        }
                        intPlayTimePercentage = (int)(100 * (intDuration + objWatchedStateEvent.gPlayer.CurrentPosition) / intTotalDuration);
                        break;
                    }
                }
                else
                {
                    intPlayTimePercentage = 100;
                }

                break;

           case EventType.StopOrChanged:

                //Calculates the Watched percentage of the movie based on all media items correlated.

                if (bolStacked && intTotalDuration != 0)
                {
                    for (int intLoop = 0; intLoop < alsFiles.Count; intLoop++)
                    {
                        if (objWatchedStateEvent.gPlayer.CurrentFile != (string)alsFiles[intLoop])
                        {
                            intDuration += objMPVideoDB.GetVideoDuration(objMPVideoDB.GetFileId((string)alsFiles[intLoop]));
                            continue;
                        }
                        intPlayTimePercentage = (100 * (intDuration + objWatchedStateEvent.intStopTime) / intTotalDuration);
                        break;
                    }
                }
                else
                {
                    if (objWatchedStateEvent.gPlayer.Duration >= 1)
                    {
                        intPlayTimePercentage = (int)Math.Ceiling((objWatchedStateEvent.intStopTime / objWatchedStateEvent.gPlayer.Duration) * 100);
                    }
                }

                //Sets the Watched status and correlated for the media file which was last played.

                for (int intLoop = 0; intLoop < alsFiles.Count; intLoop++)
                {
                    string strFilePath = (string)alsFiles[intLoop];
                    int intFileId = objMPVideoDB.GetFileId(strFilePath);
                    intMovieId = objMPVideoDB.GetMovieId(strFilePath);

                    //Check if a DVD/Blueray was played and set the Watched status and correlated according to that.

                    if (objWatchedStateEvent.gPlayer.IsDVDMenu)
                    {
                        objMPVideoDB.SetMovieStopTimeAndResumeData(intFileId, 0, null, objWatchedStateEvent.gPlayer.SetResumeBDTitleState);
                        objMPVideoDB.SetMovieWatchedStatus(intMovieId, true, 100);
                        objMPVideoDB.MovieWatchedCountIncrease(intMovieId);
                    }

                    //Check if the media file currently played is the one in the loop and set the Watched status and correlated according to that.

                    else if ((objWatchedStateEvent.strFileName.Trim().ToLowerInvariant().Equals(strFilePath.Trim().ToLowerInvariant())) && (objWatchedStateEvent.intStopTime > 0))
                    {
                        objMPVideoDB.GetMovieStopTimeAndResumeData(intFileId, out objWatchedStateEvent.gPlayer.bteResumeData, objWatchedStateEvent.gPlayer.SetResumeBDTitleState);
                        objMPVideoDB.SetMovieStopTimeAndResumeData(intFileId, objWatchedStateEvent.intStopTime, objWatchedStateEvent.gPlayer.bteResumeData, objWatchedStateEvent.gPlayer.SetResumeBDTitleState);

                        //Updates the Watched status for all media files based on the configuration done for the video database.

                        if (intPlayTimePercentage >= mWatchedPercentageVideoDatabase)
                        {
                            objMPVideoDB.SetMovieWatchedStatus(intMovieId, true, intPlayTimePercentage);
                            objMPVideoDB.MovieWatchedCountIncrease(intMovieId);
                            bolUpdated = true;
                        }
                        else
                        {
                            int intPercent = 0;
                            int intTimesWatched = 0;
                            bool bolWatched = objMPVideoDB.GetMovieWatchedStatus(intMovieId, out intPercent, out intTimesWatched);
                            objMPVideoDB.SetMovieWatchedStatus(intMovieId, bolWatched, intPlayTimePercentage);
                            bolUpdated = true;
                        }
                    }
                    else
                    {
                        objMPVideoDB.DeleteMovieStopTime(intFileId);
                    }
                }

                break;

           case EventType.Toggle:
           default:
                Log.Debug("WatchedSynchronizer: ProcessMPVideoWatchedStateEvent: unkown EventType: " + objWatchedStateEvent.EventType.ToString());
                break;
        }

        //Updates the Watched status based on the configuration done for the video database.

        if (mMarkWatchedFilesVideoDatabase)
        {
            IMDBMovie objIMDBMovie = new IMDBMovie();
            objMPVideoDB.GetMovieInfoById(intMovieId, ref objIMDBMovie);
            if (!objIMDBMovie.IsEmpty)
            {

                //Updates the Watched status for all media files based on the configuration done for the video database.

                if (intPlayTimePercentage >= mWatchedPercentageVideoDatabase)
                {
                    objIMDBMovie.Watched = 1;

                    if(objWatchedStateEvent.EventType == EventType.Ended)
                    {
                        objMPVideoDB.SetWatched(objIMDBMovie);
                        objMPVideoDB.SetMovieWatchedStatus(intMovieId, true, intPlayTimePercentage);
                        objMPVideoDB.MovieWatchedCountIncrease(intMovieId);
                        objMPVideoDB.SetDateWatched(objIMDBMovie);
                    }else if(objWatchedStateEvent.EventType == EventType.Ended)
                    {
                        objIMDBMovie.DateWatched = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        objMPVideoDB.SetMovieInfoById(intMovieId, ref objIMDBMovie);
                    }
                    bolUpdated = true;
                }
                else if(objWatchedStateEvent.EventType == EventType.Ended)
                {
                    int intPercent = 0;
                    int intTimesWatched = 0;
                    bool bolWatched = objMPVideoDB.GetMovieWatchedStatus(intMovieId, out intPercent, out intTimesWatched);
                    objMPVideoDB.SetMovieWatchedStatus(intMovieId, bolWatched, intPlayTimePercentage);
                    bolUpdated = true;
                }
            }
        }

        //Check if update was successfull and write to media portal log.

        if (bolUpdated == true)
        {
            Log.Info("WatchedSynchronizer: ProcessMPVideoWatchedStateEvent: Information for file '" + objWatchedStateEvent.strFileName + "' was updated in database '" + objMPVideoDB.DatabaseName + "'.");
        }
        objMPVideoDB.Dispose();

        //Update finished succesfully -> Event could be cleared from queue

        return 1;
    }

    #endregion

    #region TVSeries databases

    /// <summary>
    /// Method "ProcessMPTVSeriesWatchedStateEvent" is run if the media type is "MPTVSeries" and a gplayer event was triggered or the trigger was an event related to "MPTVSeries".
    /// The method runs the necessary commands to update the video databases specified in the event.
    /// Return:
    /// 1 = Infos written to given DB -> Event could be cleared from queue
    /// <0 = DB is locked by other user -> Event should not be cleared from queue -> Retry
    /// 0 = other error (ex. file not found in db, file does not exists...) -> Event could be cleared from queue
    /// </summary>

    private static int ProcessMPTVSeriesWatchedStateEvent(WatchedStateEvent objWatchedStateEvent)
    {
        int intPlayTimePercentage = 0;
        AdditionalTVSeriesDatabase objTVSeriesDB = (AdditionalTVSeriesDatabase)objWatchedStateEvent.objDB;

        objTVSeriesDB.ReOpen();
        bool bolUpdated = false;
        ArrayList alsCompositeIds = new ArrayList();

        //Get CompositeId(s).

        string strCompositeId = objTVSeriesDB.GetCompositeId(objWatchedStateEvent.strFileName);
        if (strCompositeId == string.Empty)
        {
            Log.Warn("WatchedSynchronizer: File '" + objWatchedStateEvent.strFileName + "' is not available in database '" + objTVSeriesDB.DatabaseName + "'.");
            objTVSeriesDB.Dispose();
            return 0;
        }
        else
        {
            objTVSeriesDB.GetCompositeIdsForEpisode(strCompositeId, ref alsCompositeIds);
        }
        if (alsCompositeIds.Count <= 0)
        {
            Log.Warn("WatchedSynchronizer: File '" + objWatchedStateEvent.strFileName + "' is not available in database '" + objTVSeriesDB.DatabaseName + "'.");
            objTVSeriesDB.Dispose();
            return 0;
        }

        //Calculates the Watched percentage of the episode.

        if ((objWatchedStateEvent.gPlayer.Duration >= 1) && (objWatchedStateEvent.EventType == EventType.StopOrChanged))
        {
            intPlayTimePercentage = (int)Math.Ceiling((objWatchedStateEvent.intStopTime / objWatchedStateEvent.gPlayer.Duration) * 100);
        }
        else if (objWatchedStateEvent.EventType == EventType.Ended)
        {
            intPlayTimePercentage = 100;
        }

        //Updates the Watched status for all CompositeIds.

        for (int intLoop = 0; intLoop < alsCompositeIds.Count; intLoop++)
        {
            strCompositeId = (string)alsCompositeIds[intLoop];
            bool bolWatched = false;
            string strWatchedDate;
            int intStopTime;

            //Check if episode was already watched.

            objTVSeriesDB.GetEpisodeWatchedStatus(strCompositeId, out bolWatched, out strWatchedDate, out intStopTime);

            //Updates the Watched status based on the configuration done for the tvseries database

            if (((objWatchedStateEvent.EventType != EventType.Toggle) && (intPlayTimePercentage >= mWatchedPercentageTVSeriesDatabase)) || ((objWatchedStateEvent.EventType == EventType.Toggle) && (bolWatched == false && objWatchedStateEvent.bolWatched == true)))
            {
                if (((objWatchedStateEvent.EventType != EventType.Toggle) && (bolWatched == false)) || ((objWatchedStateEvent.EventType == EventType.Toggle) && (bolWatched == false && objWatchedStateEvent.bolWatched == true)))
                {

                    //Update the Watched status for the episodes and correlated it is unwatched.

                    objTVSeriesDB.SetEpisodeWatchedStatus(strCompositeId, true, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), 0, true);
                    if (objWatchedStateEvent.EventType == EventType.Toggle) objTVSeriesDB.EpisodePlayCountIncrease(strCompositeId, true);
                    string strSeasonId;
                    bool bolSeasonWatched = false;
                    objTVSeriesDB.GetSeasonWatchedStatus(strCompositeId, out strSeasonId, out bolSeasonWatched);
                    if (bolSeasonWatched == false)
                    {

                        //Update the Unwatched episodes count for the season if it is unwatched.

                        objTVSeriesDB.SeasonUnWatchedCountDecrease(strSeasonId);
                        string strSeriesId;
                        bool bolSeriesWatched = false;
                        objTVSeriesDB.GetSeriesWatchedStatus(strCompositeId, out strSeriesId, out bolSeriesWatched);
                        if (bolSeriesWatched == false)
                        {

                            //Update the Unwatched episodes count for the series if it is unwatched.

                            objTVSeriesDB.SeriesUnWatchedCountDecrease(strSeriesId);
                        }
                    }
                }
                else
                {
                    if (objWatchedStateEvent.EventType == EventType.StopOrChanged)
                    {
                        strWatchedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    objTVSeriesDB.SetEpisodeWatchedStatus(strCompositeId, bolWatched, strWatchedDate, 0);
                }
                objTVSeriesDB.EpisodePlayCountIncrease(strCompositeId);
                bolUpdated = true;
            }
            else
            {
                if (objWatchedStateEvent.EventType == EventType.Ended)
                {
                    intStopTime = 0;
                }
                if (objWatchedStateEvent.EventType == EventType.StopOrChanged)
                {
                    intStopTime = objWatchedStateEvent.intStopTime;
                }
                if (objWatchedStateEvent.EventType == EventType.Toggle)
                {
                    bolWatched = objWatchedStateEvent.bolWatched;
                }

                //Update Watched State

                objTVSeriesDB.SetEpisodeWatchedStatus(strCompositeId, bolWatched, strWatchedDate, intStopTime);

                if (objWatchedStateEvent.EventType == EventType.Ended) objTVSeriesDB.EpisodePlayCountIncrease(strCompositeId);
                bolUpdated = true;
            }
        }

        //Check if update was successfull and write to media portal log.

        if (bolUpdated == true)
        {
            Log.Info("WatchedSynchronizer: Information for file '" + objWatchedStateEvent.strFileName + "' was updated in database '" + objTVSeriesDB.DatabaseName + "'.");
        }
        objTVSeriesDB.Dispose();

        //Update finished succesfully -> Event could be cleared from queue

        return 1;
    }

    #endregion

    #endregion

  }
}