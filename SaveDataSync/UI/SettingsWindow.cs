﻿using System;
using System.Windows.Forms;

namespace SaveDataSync.UI
{
    internal partial class SettingsWindow : Form
    {
        private SaveDataSyncEngine engine;
        private Settings settingsCopy;

        public SettingsWindow(SaveDataSyncEngine engine)
        {
            this.engine = engine;
            this.settingsCopy = engine.Settings.Clone();
            InitializeComponent();
        }

        private void closeBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void saveBtn_Click(object sender, EventArgs e)
        {
            // Get the new settings from the UI here
            engine.Settings = settingsCopy;
            await engine.SaveAllData();
            Close();
        }
    }
}