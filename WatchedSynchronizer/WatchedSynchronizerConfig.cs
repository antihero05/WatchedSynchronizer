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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MediaPortal.Configuration;
using MediaPortal.Profile;
using WatchedSynchronizer.Configuration;

namespace WatchedSynchronizer
{
  public partial class WatchedSynchronizerConfig : Form
  {

    #region Declaration

    private List<int> mValidRows = new List<int>();
    public WatchedSynchronizerConfiguration mConfiguration;

    #endregion

    #region Constructors

    public WatchedSynchronizerConfig()
    {
      InitializeComponent();
      InitializeDataGridView();
      LoadConfiguration();
    }

    #endregion

    #region Private methods

    private void InitializeDataGridView()
    {
      if (this.dgvDatabases.Columns.Count == 0)
      {
        DataGridViewCheckBoxColumn colEnabled = new DataGridViewCheckBoxColumn();
        colEnabled.Name = "Enabled";
        colEnabled.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
        colEnabled.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        this.dgvDatabases.Columns.Add(colEnabled);
        DataGridViewComboBoxColumn colDatabaseType = new DataGridViewComboBoxColumn();
        colDatabaseType.Name = "Database type";
        colDatabaseType.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        colDatabaseType.Items.Add("Video database");
        ArrayList alsLoadedPlugins = Plugins.LoadedPlugins;
        foreach (ItemTag objLoop in alsLoadedPlugins)
        {
          if (objLoop.WindowId == 9811)
          {
            colDatabaseType.Items.Add("TVSeries database");
          }
        }
        colDatabaseType.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        this.dgvDatabases.Columns.Add(colDatabaseType);
        DataGridViewButtonColumn colPath = new DataGridViewButtonColumn();
        colPath.Name = "Database path";
        colPath.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        colPath.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        this.dgvDatabases.Columns.Add(colPath);
        this.dgvDatabases.EditMode = DataGridViewEditMode.EditOnEnter;
        this.dgvDatabases.AllowUserToOrderColumns = false;
        this.dgvDatabases.AllowUserToResizeRows = false;
      }
    }

    private void LoadConfiguration()
    {
      MPSettings objSettings = MPSettings.Instance;
      string strConfiguration = objSettings.GetValueAsString("watchedsynchronizer", "configuration", string.Empty);
      if (string.IsNullOrEmpty(strConfiguration))
      {
        this.mConfiguration = new WatchedSynchronizerConfiguration();
      }
      else
      {
        this.mConfiguration = Utils.Deserialize(strConfiguration);
        DataGridViewComboBoxColumn colDatabaseType = this.dgvDatabases.Columns["Database type"] as DataGridViewComboBoxColumn;
        foreach (WatchedSynchronizerConfigurationEntry objLoop in this.mConfiguration.mConfigurationEntries)
        {
          if (colDatabaseType.Items.Contains(objLoop.mDatabaseType))
          {
            this.dgvDatabases.Rows.Add(objLoop.mEnabled, objLoop.mDatabaseType, objLoop.mDatabasePath);
          }
        }
      }   
    }

    private void ValidateDataGridView()
    {
      this.mValidRows.Clear();
      foreach (DataGridViewRow objLoop in this.dgvDatabases.Rows)
      {
        if (!string.IsNullOrEmpty(objLoop.Cells[1].FormattedValue.ToString()) && !string.IsNullOrEmpty(objLoop.Cells[2].FormattedValue.ToString()))
          this.mValidRows.Add(objLoop.Index);
      }
    }

    private void ValidateDataGridView(int intColumnIndex, int intRowIndex, bool bolKeepState = false)
    {
      if (string.IsNullOrEmpty(this.dgvDatabases[1, intRowIndex].FormattedValue.ToString()) || string.IsNullOrEmpty(this.dgvDatabases[2, intRowIndex].FormattedValue.ToString()))
      {
        if (bolKeepState == false)
        {
          this.dgvDatabases.AllowUserToAddRows = false;
        }
      }
      else
      {
        if (this.mValidRows.Contains(intRowIndex))
        {
          this.mValidRows.Add(intRowIndex);
        }
        if (this.dgvDatabases.Rows.Count - 1 == intRowIndex)
        {
          this.dgvDatabases.AllowUserToAddRows = true;
        }
        else
        {
          if (this.dgvDatabases.AllowUserToAddRows == false)
          {
            ValidateDataGridView(intColumnIndex, this.dgvDatabases.Rows.Count - 1);
          }
        }
      }
    }

    private void DisplayFileDialog(int intColumnIndex, int intRowIndex)
    {
      OpenFileDialog dlgFileBrowser = new OpenFileDialog();
      dlgFileBrowser.CheckPathExists = true;
      dlgFileBrowser.CheckFileExists = true;
      dlgFileBrowser.Multiselect = false;
      dlgFileBrowser.Title = "Choose database file";
      if (File.Exists((string)this.dgvDatabases[intColumnIndex, intRowIndex].Value))
      {
        dlgFileBrowser.FileName = this.dgvDatabases[intColumnIndex, intRowIndex].Value.ToString();
      }
      DialogResult resDialogResult = dlgFileBrowser.ShowDialog();
      if (resDialogResult == DialogResult.OK)
      {
        if (ValidateDatabaseFile(dlgFileBrowser.FileName) == true)
        {
          this.dgvDatabases[intColumnIndex, intRowIndex].Value = dlgFileBrowser.FileName;
          this.dgvDatabases.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
        else
        {
          MessageBox.Show("Selected file is not a valid SQLite database. Please choose a valid SQLite database file.", "Invalid selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
          DisplayFileDialog(intColumnIndex, intRowIndex);
        }
      }
    }

    private bool ValidateDatabaseFile(string strFileName)
    {
      FileStream fstDatabaseFile = new FileStream(strFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      if (fstDatabaseFile.Length >= 16)
      {
        byte[] bteDatabaseFileHeader = new byte[16];
        fstDatabaseFile.Read(bteDatabaseFileHeader, 0, 16);
        fstDatabaseFile.Close();
        string strDatabaseFileHeader = UnicodeEncoding.UTF8.GetString(bteDatabaseFileHeader).Trim('\0');
        if (strDatabaseFileHeader == "SQLite format 3")
        {
          return true;
        }
      }
      return false;
    }

    private void SaveConfiguration()
    {
      this.mConfiguration.mConfigurationEntries = new List<WatchedSynchronizerConfigurationEntry>();
      foreach (DataGridViewRow objLoop in this.dgvDatabases.Rows)
      {
        if (!string.IsNullOrEmpty(objLoop.Cells[1].FormattedValue.ToString()) || !string.IsNullOrEmpty(objLoop.Cells[2].FormattedValue.ToString()))
        {
          WatchedSynchronizerConfigurationEntry objEntry = new WatchedSynchronizerConfigurationEntry();
          objEntry.mEnabled = (bool)objLoop.Cells[0].FormattedValue;
          objEntry.mDatabaseType = (string)objLoop.Cells[1].FormattedValue;
          objEntry.mDatabasePath = (string)objLoop.Cells[2].FormattedValue;
          this.mConfiguration.mConfigurationEntries.Add(objEntry);
        }
      }
      string strConfiguration = Utils.Serialize(mConfiguration);
      MPSettings objSettings = MPSettings.Instance;
      objSettings.SetValue("watchedsynchronizer", "configuration", strConfiguration);
    }

    #endregion

    #region Events

    private void btnOk_Click(object sender, EventArgs e)
    {
      SaveConfiguration();
      this.Dispose();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
      this.Dispose();
    }

    private void dgvDatabases_CellClick(object sender, DataGridViewCellEventArgs e)
    {
      if (e.ColumnIndex == -1)
      {
        return;
      }
      if (dgvDatabases.Columns[e.ColumnIndex] is DataGridViewComboBoxColumn)
      {
        ComboBox cmbComboBox = this.dgvDatabases.EditingControl as ComboBox;
        cmbComboBox.DroppedDown = true;
      }
      else if (this.dgvDatabases.Columns[e.ColumnIndex] is DataGridViewButtonColumn)
      {
        if (e.ColumnIndex == 2)
        {
          DisplayFileDialog(e.ColumnIndex, e.RowIndex);
        }
        ValidateDataGridView(e.ColumnIndex, e.RowIndex,true);
      }
    }

    private void dgvDatabases_CurrentCellDirtyStateChanged(object sender, EventArgs e)
    {
      if (this.dgvDatabases.IsCurrentCellDirty == true)
      {
        this.dgvDatabases.CommitEdit(DataGridViewDataErrorContexts.Commit);
        ValidateDataGridView(this.dgvDatabases.CurrentCell.ColumnIndex, this.dgvDatabases.CurrentCell.RowIndex);
      }
    }

    private void dgvDatabases_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
    {
      ValidateDataGridView();
    }

    #endregion

  }
}
