﻿using NimbusApp.Models;
using NimbusApp.Saves;
using NimbusApp.Server;
using NimbusApp.Settings;
using NimbusApp.UI;
using NimbusApp.Utils;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NimbusApp.Controllers
{
    /// <summary>
    /// Class for connecting the UI to the back end
    /// </summary>
    public class NimbusAppEngine
    {
        public LocalSaveList LocalSaveList { get; set; }
        public ServerBase Server { get; set; }
        public AppSettings Settings { get; set; }

        [JsonConstructor]
        public NimbusAppEngine()
        {
            LocalSaveList = new LocalSaveList();
            Settings = new AppSettings();
        }

        /// <summary>
        /// Gets the save manager used to manage save game data
        /// </summary>
        /// <returns>The instance save manager</returns>
        public SaveManager GetSaveManager()
        {
            return new SaveManager(this);
        }

        /// <summary>
        /// Adds a save to the save list and saves the new data
        /// </summary>
        /// <param name="name">The name of the game save</param>
        /// <param name="location">The file/folder location</param>
        /// <returns>Task representing asynchronous operation</returns>
        public void AddSave(string name, string location) => GetSaveManager().AddSave(name, location);

        public async Task<string[]> ExportSaves(string[] saves, ProgressBarControl progress) =>
            await GetSaveManager().ExportSaves(saves, progress);

        public async Task<string[]> ImportSaves(string[] saves, ProgressBarControl progress) =>
            await GetSaveManager().ImportSaves(saves, progress);

        /// <summary>
        /// Gets the local save hash for comparing files
        /// </summary>
        /// <param name="save">The name of the save game</param>
        /// <returns>An asynchronous task resulting in the local save hash</returns>
        public async Task<string> GetLocalHash(string save)
        {
            using var tmpFile = new FileUtils.TemporaryFile();
            await LocalSaveList.ArchiveSaveData(save, tmpFile.FilePath);
            var saveHash = await Server.GetLocalSaveHash(tmpFile.FilePath);
            return saveHash;
        }

        public async Task<string> GetRemoteHash(string save) => await Server.GetRemoteSaveHash(save);

        public async Task Serialize()
        {
            await Serialize(DefaultLocations.DataFile);
        }

        /// <summary>
        /// Saves settings, local saves, and server data to storage
        /// </summary>
        /// <returns>Task representing asynchronous operation</returns>
        public async Task Serialize(string DataFile)
        {
            CreateDataFile(DataFile);

            // Serialize the data as JSON
            var json = JsonSerializer.Serialize(this);

            // Encrypt and write data 
            var jsonAsBytes = Encoding.ASCII.GetBytes(json.ToString());
            using var fsStream = File.Open(DataFile, FileMode.OpenOrCreate);
            var encData = ProtectedData.Protect(jsonAsBytes, null, DataProtectionScope.CurrentUser);
            await fsStream.WriteAsync(encData);
        }

        /// <summary>
        /// Loads the engine with the default data file
        /// </summary>
        /// <returns>The engine for the data</returns>
        public static async Task<NimbusAppEngine> Deserialize()
        {
            return await Deserialize(DefaultLocations.DataFile);
        }

        /// <summary>
        /// Attempts to load the engine given the data file
        /// </summary>
        /// <returns>The engine instance loaded</returns>
        public static async Task<NimbusAppEngine> Deserialize(string dataFile)
        {
            if (!File.Exists(dataFile))
            {
                return await CreateNew(dataFile);
            }

            try
            {
                var encBytes = File.ReadAllBytes(dataFile);
                if (encBytes.Length == 0)
                {
                    return await CreateNew(dataFile);
                }
                var rawBytes = ProtectedData.Unprotect(encBytes, null, DataProtectionScope.CurrentUser);
                var rawString = Encoding.ASCII.GetString(rawBytes);
                var engine = JsonSerializer.Deserialize<NimbusAppEngine>(rawString);
                return engine;
            }
            catch (Exception ex)
            {
                var res = PopupDialog.ErrorPrompt($"Data is corrupted or out of date. Would you like to reset it?\nError: {ex.Message}");
                if (res == DialogResult.Yes)
                {
                    return await CreateNew(dataFile);
                }

                Application.Exit();
                return null; // Doesn't matter the app is closed
            }


        }

        private static void CreateDataFile(string DataFile)
        {
            if (File.Exists(DataFile))
            {
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(DataFile));
            using var _ = File.Open(DataFile, FileMode.OpenOrCreate);
        }

        private static async Task<NimbusAppEngine> CreateNew(string DataFile)
        {
            CreateDataFile(DataFile);
            var engine = new NimbusAppEngine();
            await engine.Serialize(DataFile);
            return engine;
        }
    }
}