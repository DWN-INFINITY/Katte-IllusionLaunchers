﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using MessageBox = System.Windows.Forms.MessageBox;

namespace InitSetting
{
    public partial class MainWindow : Window
    {
        // Game-specific constants -------------------------------------------------------------------
        private const string RegistryKeyGame = "Software\\illusion\\HoneySelect";
        private const string RegistryKeyStudio = "Software\\illusion\\HoneySelect\\HoneyStudio";
        private const string RegistryKeyStudioNeo = "Software\\illusion\\HoneySelect\\StudioNEO";
        private const string RegistryKeyBattleArena = "Software\\illusion\\HoneySelect\\BattleArena";
        private const string RegistryKeyVR = "";
        private const string ExecutableGame = "HoneySelect_64.exe";
        private const string ExecutableGame32 = "HoneySelect_32.exe";
        private const string ExecutableStudio = "HoneyStudio_64.exe";
        private const string ExecutableStudio32 = "HoneyStudio_32.exe";
        private const string ExecutableStudioNeo = "StudioNEO_64.exe";
        private const string ExecutableStudioNeo32 = "StudioNEO_32.exe";
        private const string ExecutableBattleArena = "BattleArena_64.exe";
        private const string ExecutableBattleArena32 = "BattleArena_32.exe";
        private const string ExecutableVR = "HoneySelectVR.exe";
        private const string ExecutableVRVive = "HoneySelectVR_Vive.exe";
        private const string SupportDiscord = "https://discord.gg/F3bDEFE";
        // Languages built into the game itself
        private static readonly string[] _builtinLanguages = { "ja-JP" };
        private bool _is32;

        // Normal fields, don't fill in --------------------------------------------------------------
        private bool _suppressEvents;
        private readonly bool _mainGameExists;
        private readonly bool _studioExists;

        public MainWindow()
        {
            CloseWindow.WinObject = (Window)this;
            try
            {
                _suppressEvents = true;

                // Initialize code -------------------------------------
                EnvironmentHelper.Initialize(_builtinLanguages);

                _mainGameExists = File.Exists(EnvironmentHelper.GameRootDirectory + ExecutableGame);
                _studioExists = File.Exists(EnvironmentHelper.GameRootDirectory + ExecutableStudio);

                if (_studioExists)
                    SettingManager.Initialize(EnvironmentHelper.GetConfigFilePath(), RegistryKeyGame, RegistryKeyStudio, RegistryKeyStudioNeo, RegistryKeyBattleArena);
                else
                    SettingManager.Initialize(EnvironmentHelper.GetConfigFilePath(), RegistryKeyGame);

                SettingManager.LoadSettings();

                // Initialize interface --------------------------------
                InitializeComponent();

                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                CustomRes.Visibility = Visibility.Hidden;

                if (string.IsNullOrEmpty((string)labelTranslated.Content))
                {
                    labelTranslated.Visibility = Visibility.Hidden;
                    labelTranslatedBorder.Visibility = Visibility.Hidden;
                }

                if (!EnvironmentHelper.KKmanExist)
                    gridUpdate.Visibility = Visibility.Hidden;

                // Launcher Customization: Defining Warning, background and character
                if (!string.IsNullOrEmpty(EnvironmentHelper.VersionString))
                    labelDist.Content = EnvironmentHelper.VersionString;

                if (!string.IsNullOrEmpty(EnvironmentHelper.WarningString))
                    warningText.Text = EnvironmentHelper.WarningString;

                if (EnvironmentHelper.CustomCharacterImage != null)
                    PackChara.Source = EnvironmentHelper.CustomCharacterImage;
                if (EnvironmentHelper.CustomBgImage != null)
                    appBG.ImageSource = EnvironmentHelper.CustomBgImage;
                
                /*
                if (string.IsNullOrEmpty(EnvironmentHelper.PatreonUrl))
                {
                    linkPatreon.Visibility = Visibility.Collapsed;
                    patreonBorder.Visibility = Visibility.Collapsed;
                    patreonIMG.Visibility = Visibility.Collapsed;
                }
                */
                var primaryDisplay = Localizable.PrimaryDisplay;
                var subDisplay = Localizable.SubDisplay;
                for (var i = 0; i < Screen.AllScreens.Length; i++)
                {
                    // 0 is primary
                    var newItem = i == 0 ? primaryDisplay : $"{subDisplay} : " + i;
                    dropDisplay.Items.Add(newItem);
                }

                HoneySelectStartup();
                PluginToggleManager.CreatePluginToggles(Toggleables);

                _suppressEvents = false;

                UpdateDisplaySettings(SettingManager.CurrentSettings.FullScreen);

                Closed += (sender, args) => SettingManager.SaveSettings();
                MouseDown += (sender, args) => { if (args.ChangedButton == MouseButton.Left) DragMove(); };
                buttonClose.Click += (sender, args) => Close();
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to start the launcher, please consider reporting this error to the developers.\n\nError that caused the crash: " + e, "Launcher crash", MessageBoxButtons.OK, MessageBoxIcon.Error);
                File.WriteAllText(Path.Combine(EnvironmentHelper.GameRootDirectory, "launcher_crash.log"), e.ToString());
                Close();
            }
        }

        #region HoneySelect Speciffic code

        private void StartupFilter(string exeFile)
        {
            if (_is32)
            {
                switch (exeFile)
                {
                    case ExecutableGame:
                        exeFile = ExecutableGame32;
                        break;
                    case ExecutableStudio:
                        exeFile = ExecutableStudio32;
                        break;
                }
            }

            StartGame(exeFile);
        }

        private void toggle32_Checked(object sender, RoutedEventArgs e)
        {
            _is32 = true;
            if (File.Exists($"{EnvironmentHelper.GameRootDirectory}/UserData/LauncherEN/toggle32.txt")) return;
            using (var writetext = new StreamWriter($"{EnvironmentHelper.GameRootDirectory}/UserData/LauncherEN/toggle32.txt"))
            {
                writetext.WriteLine("x86");
            }
        }

        private void toggle32_Unchecked(object sender, RoutedEventArgs e)
        {
            _is32 = false;
            if (File.Exists($"{EnvironmentHelper.GameRootDirectory}/UserData/LauncherEN/toggle32.txt"))
                File.Delete($"{EnvironmentHelper.GameRootDirectory}/UserData/LauncherEN/toggle32.txt");
        }

        public static class CloseWindow
        {
            public static Window WinObject;

            public static void CloseParent()
            {
                try
                {
                    ((Window)WinObject).Close();
                }
                catch (Exception e)
                {
                    string value = e.Message.ToString(); // do whatever with this
                }
            }
        }

        private void HoneySelectStartup()
        {
            if (!File.Exists($"{EnvironmentHelper.GameRootDirectory}/{ExecutableGame32}"))
                toggle32.Visibility = Visibility.Collapsed;
            if (File.Exists($"{EnvironmentHelper.GameRootDirectory}/UserData/LauncherEN/toggle32.txt") &&
                File.Exists($"{EnvironmentHelper.GameRootDirectory}/{ExecutableGame32}"))
                toggle32.IsChecked = true;
        }

        #endregion

        #region Display settings

        private void ResolutionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (-1 == dropRes.SelectedIndex) return;

            var comboBoxCustomItem = (ComboBoxCustomItem)dropRes.SelectedItem;
            SettingManager.CurrentSettings.Size = comboBoxCustomItem.text;
            SettingManager.CurrentSettings.Width = comboBoxCustomItem.width;
            SettingManager.CurrentSettings.Height = comboBoxCustomItem.height;
        }

        private void QualityChanged(object sender, SelectionChangedEventArgs e)
        {
            SettingManager.CurrentSettings.Quality = dropQual.SelectedIndex;
        }

        private void FullscreenUnChecked(object sender, RoutedEventArgs e)
        {
            UpdateDisplaySettings(false);
        }

        private void FullscreenChecked(object sender, RoutedEventArgs e)
        {
            UpdateDisplaySettings(true);
        }

        private void DisplayChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dropDisplay.SelectedIndex < 0) return;

            SettingManager.CurrentSettings.Display = dropDisplay.SelectedIndex;
            UpdateDisplaySettings(SettingManager.CurrentSettings.FullScreen);
        }

        private void UpdateDisplaySettings(bool bFullScreen)
        {
            if (_suppressEvents) return;
            _suppressEvents = true;

            toggleFullscreen.IsChecked = bFullScreen;
            if (!SettingManager.SetFullScreen(bFullScreen))
            {
                toggleFullscreen.IsChecked = false;
                MessageBox.Show("This monitor doesn't support fullscreen.");
            }

            dropRes.Items.Clear();
            foreach (var displayMode in SettingManager.GetCurrentDisplayModes())
            {
                var newItem = new ComboBoxCustomItem
                {
                    text = displayMode.text,
                    width = displayMode.Width,
                    height = displayMode.Height
                };
                dropRes.Items.Add(newItem);
            }

            dropRes.Text = SettingManager.CurrentSettings.Size;

            dropDisplay.SelectedIndex = SettingManager.CurrentSettings.Display;
            dropQual.SelectedIndex = Math.Max(Math.Min(SettingManager.CurrentSettings.Quality, dropQual.Items.Count), 0);

            _suppressEvents = false;
        }

        #endregion

        #region Start game buttons and manuals

        private void StartGame(string strExe)
        {
            SettingManager.SaveSettings();
            if (EnvironmentHelper.StartGame(strExe))
                Close();
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            StartupFilter(ExecutableGame);
        }

        private void buttonStartS_Click(object sender, RoutedEventArgs e)
        {
            new BootChoice().SetupWindow(Localizable.StartVR, "studio", _is32);
        }

        private void buttonStartV_Click(object sender, RoutedEventArgs e)
        {
            new BootChoice().SetupWindow(Localizable.StartVR, "vr", _is32);
        }

        private void buttonStartB_Click(object sender, RoutedEventArgs e)
        {
            StartupFilter(ExecutableBattleArena);
        }

        private void buttonManual_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentHelper.ShowManual($"{EnvironmentHelper.GameRootDirectory}\\manual\\");
        }

        private void buttonManualS_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentHelper.ShowManual($"{EnvironmentHelper.GameRootDirectory}\\manual_s\\");
        }

        private void buttonManualV_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentHelper.ShowManual($"{EnvironmentHelper.GameRootDirectory}\\manual_v\\");
        }

        private void buttonManualB_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentHelper.ShowManual($"{EnvironmentHelper.GameRootDirectory}\\manual_b\\");
        }

        #endregion

        #region Discord button block

        private void discord_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            EnvironmentHelper.StartProcess(SupportDiscord);
        }

        private void patreon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            EnvironmentHelper.StartProcess(EnvironmentHelper.PatreonUrl);
        }

        private void update_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            EnvironmentHelper.StartUpdate();
        }

        #endregion

        #region Language buttons

        private void LangEnglish(object sender, MouseButtonEventArgs e)
        {
            EnvironmentHelper.SetLanguage("en-US");
        }

        private void LangJapanese(object sender, MouseButtonEventArgs e)
        {
            EnvironmentHelper.SetLanguage("ja-JP");
        }

        private void LangChinese(object sender, MouseButtonEventArgs e)
        {
            EnvironmentHelper.SetLanguage("zh-CN");
        }

        private void LangChineseTW(object sender, MouseButtonEventArgs e)
        {
            EnvironmentHelper.SetLanguage("zh-TW");
        }

        private void LangKorean(object sender, MouseButtonEventArgs e)
        {
            EnvironmentHelper.SetLanguage("ko-KR");
        }

        private void LangSpanish(object sender, MouseButtonEventArgs e)
        {
            EnvironmentHelper.SetLanguage("es-ES");
        }

        private void LangBrazil(object sender, MouseButtonEventArgs e)
        {
            EnvironmentHelper.SetLanguage("pt-PT");
        }

        private void LangFrench(object sender, MouseButtonEventArgs e)
        {
            EnvironmentHelper.SetLanguage("fr-FR");
        }

        private void LangGerman(object sender, MouseButtonEventArgs e)
        {
            EnvironmentHelper.SetLanguage("de-DE");
        }

        private void LangNorwegian(object sender, MouseButtonEventArgs e)
        {
            EnvironmentHelper.SetLanguage("no-NB");
        }

        private void LangRussian(object sender, MouseButtonEventArgs e)
        {
            EnvironmentHelper.SetLanguage("ru-RU");
        }

        #endregion

        #region Directory open buttons

        private void buttonInst_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentHelper.OpenDirectory("");
        }

        private void buttonScenes_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentHelper.OpenDirectory("UserData\\StudioNEO\\scene");
        }

        private void buttonUserData_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentHelper.OpenDirectory("UserData");
        }

        private void buttonCCenes_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentHelper.OpenDirectory("UserData\\studio");
        }

        private void buttonScreenshot_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentHelper.OpenDirectory("UserData\\cap");
        }

        private void buttonFemaleCard_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentHelper.OpenDirectory("UserData\\chara\\female");
        }

        private void buttonMaleCard_Click(object sender, RoutedEventArgs e)
        {
            EnvironmentHelper.OpenDirectory("UserData\\chara\\male");
        }

        private void bepisdb_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://db.bepis.io/honeyselect");
        }

        private void ilbooru_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://illusioncards.booru.org/index.php?page=post&s=list&tags=honey_select");
        }

        private void kenzato_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://kenzato.uk/booru/category/HS");
        }
        #endregion
    }
}