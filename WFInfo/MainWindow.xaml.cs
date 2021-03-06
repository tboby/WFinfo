using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WFInfo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly Main main; //subscriber
        public static MainWindow INSTANCE;
        public static WelcomeDialogue hai;
        public static LowLevelListener listener;

        public MainWindow()
        {
            string thisprocessname = Process.GetCurrentProcess().ProcessName;
            if (Process.GetProcesses().Count(p => p.ProcessName == thisprocessname) > 1)
            {
                Main.AddLog("Duplicate process found");
                Close();
            }

            INSTANCE = this;
            main = new Main();

            listener = new LowLevelListener(); //publisher
            try
            {
                if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo\settings.json"))
                {
                    Settings.settingsObj = JObject.Parse(File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WFInfo\settings.json"));

                }
                else
                {
                    Settings.settingsObj = new JObject();
                    hai = new WelcomeDialogue();
                }
                if (!Settings.settingsObj.TryGetValue("Display", out _))
                    Settings.settingsObj["Display"] = "Overlay";
                Settings.isOverlaySelected = Settings.settingsObj.GetValue("Display").ToString() == "Overlay";

                if (!Settings.settingsObj.TryGetValue("MainWindowLocation_X", out _))
                    Settings.settingsObj["MainWindowLocation_X"] = 300;
                if (!Settings.settingsObj.TryGetValue("MainWindowLocation_Y", out _))
                    Settings.settingsObj["MainWindowLocation_Y"] = 300;
                Settings.mainWindowLocation = new Point(Settings.settingsObj.GetValue("MainWindowLocation_X").ToObject<Int32>(), Settings.settingsObj.GetValue("MainWindowLocation_Y").ToObject<Int32>());

                if (!Settings.settingsObj.TryGetValue("ActivationKey", out _))
                    Settings.settingsObj["ActivationKey"] = "Snapshot";
                try
                {
                    Settings.ActivationKey = (Key)Enum.Parse(typeof(Key), Settings.settingsObj.GetValue("ActivationKey").ToString());
                }
                catch
                {
                    try
                    {
                        Settings.ActivationMouseButton = (MouseButton)Enum.Parse(typeof(MouseButton), Settings.settingsObj.GetValue("ActivationKey").ToString());
                    }
                    catch
                    {
                        Main.AddLog("Couldn't Parse Activation Key -- Defaulting to PrintScreen");
                        Settings.settingsObj["ActivationKey"] = "Snapshot";
                        Settings.ActivationKey = Key.Snapshot;
                    }
                }

                if (!Settings.settingsObj.TryGetValue("Debug", out _))
                    Settings.settingsObj["Debug"] = false;
                Settings.debug = (bool)Settings.settingsObj.GetValue("Debug");

                if (!Settings.settingsObj.TryGetValue("Clipboard", out _))
                    Settings.settingsObj["Clipboard"] = false;
                Settings.clipboard = (bool)Settings.settingsObj.GetValue("Clipboard");

                if (!Settings.settingsObj.TryGetValue("Auto", out _))
                    Settings.settingsObj["Auto"] = false;
                Settings.auto = (bool)Settings.settingsObj.GetValue("Auto");

                if (!Settings.settingsObj.TryGetValue("CuttingEdge", out _))
                    Settings.settingsObj["CuttingEdge"] = false;
                Settings.autoScaling = (bool)Settings.settingsObj.GetValue("CuttingEdge");

                if (!Settings.settingsObj.TryGetValue("AutoDelay", out _))
                    Settings.settingsObj["AutoDelay"] = 250L;
                Settings.autoDelay = (long)Settings.settingsObj.GetValue("AutoDelay");

                if (!Settings.settingsObj.TryGetValue("Scaling", out _))
                    Settings.settingsObj["Scaling"] = 100.0;
                Settings.scaling = Convert.ToInt32(Settings.settingsObj.GetValue("Scaling"));

                if (!Settings.settingsObj.TryGetValue("ImageRetentionTime", out _))
                    Settings.settingsObj["ImageRetentionTime"] = 12;
                Settings.imageRetentionTime = Convert.ToInt32(Settings.settingsObj.GetValue("ImageRetentionTime"));

                if (!Settings.settingsObj.TryGetValue("ClipboardTemplate", out _))
                    Settings.settingsObj["ClipboardTemplate"] = "-- by WFInfo (smart OCR with pricecheck)";
                Settings.ClipboardTemplate = Convert.ToString(Settings.settingsObj.GetValue("ClipboardTemplate"));

                if (!Settings.settingsObj.TryGetValue("SnapitExport", out _))
                    Settings.settingsObj["SnapitExport"] = false;
                Settings.SnapitExport = Convert.ToBoolean(Settings.settingsObj.GetValue("SnapitExport"));

                if (!Settings.settingsObj.TryGetValue("Highlight", out _))
                    Settings.settingsObj["Highlight"] = false;
                Settings.Highlight = Convert.ToBoolean(Settings.settingsObj.GetValue("Highlight"));

                Settings.Save();

                LowLevelListener.KeyEvent += main.OnKeyAction;
                LowLevelListener.MouseEvent += main.OnMouseAction;
                listener.Hook();
                InitializeComponent();
                Version.Content = "v" + Main.BuildVersion;

                Left = 300;
                Top = 300;

                System.Drawing.Rectangle winBounds = new System.Drawing.Rectangle(Convert.ToInt32(Settings.mainWindowLocation.X), Convert.ToInt32(Settings.mainWindowLocation.Y), Convert.ToInt32(Width), Convert.ToInt32(Height));
                foreach (System.Windows.Forms.Screen scr in System.Windows.Forms.Screen.AllScreens)
                {
                    if (scr.Bounds.Contains(winBounds))
                    {
                        Left = Settings.mainWindowLocation.X;
                        Top = Settings.mainWindowLocation.Y;
                        break;
                    }
                }
                Settings.settingsObj["MainWindowLocation_X"] = Left;
                Settings.settingsObj["MainWindowLocation_Y"] = Top;
                Settings.Save();

            }
            catch (Exception e)
            {
                Main.AddLog("An error occured while loading the main window: " + e.Message);
            }
        }

        public void OnContentRendered(object sender, EventArgs e)
        {
            if (hai != null)
            {
                hai.Left = Left + Width + 30;
                hai.Top = Top + Height / 2 - hai.Height / 2;
                hai.Show();
            }
        }

        public void ChangeStatus(string status, int serverity)
        {
            Console.WriteLine("Status message: " + status);
            Status.Text = status;
            switch (serverity)
            {
                case 0: //default, no problem
                    Status.Foreground = new SolidColorBrush(Color.FromRgb(177, 208, 217));
                    break;
                case 1: //severe, red text
                    Status.Foreground = Brushes.Red;
                    break;
                case 2: //warning, orange text
                    Status.Foreground = Brushes.Orange;
                    break;
                default: //Uncaught, big problem
                    Status.Foreground = Brushes.Yellow;
                    break;
            }
        }

        public void Exit(object sender, RoutedEventArgs e)
        {
            notifyIcon.Dispose();
            Application.Current.Shutdown();
        }

        private void Minimise(object sender, RoutedEventArgs e)
        {
            Visibility = Visibility.Hidden;
        }

        private void websiteClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/N8S5zfw");
        }

        private void relicsClick(object sender, RoutedEventArgs e)
        {
            if (Main.dataBase.relicData == null) { ChangeStatus("Relic data not yet loaded in", 2); return; }
            Main.relicWindow.Show();
            Main.relicWindow.Focus();
        }

        private void equipmentClick(object sender, RoutedEventArgs e)
        {
            if (Main.dataBase.equipmentData == null) { ChangeStatus("Equipment data not yet loaded in", 2); return; }
            Main.equipmentWindow.Show();
        }

        private void Settings_click(object sender, RoutedEventArgs e)
        {
            if (Main.settingsWindow == null) { ChangeStatus("Settings window not yet loaded in", 2); return; }
            Main.settingsWindow.populate();
            Main.settingsWindow.Left = Left + 320;
            Main.settingsWindow.Top = Top;
            Main.settingsWindow.Show();
        }

        private void ReloadMarketClick(object sender, RoutedEventArgs e)
        {
            ReloadDrop.IsEnabled = false;
            ReloadMarket.IsEnabled = false;
            Market_Data.Content = "Loading...";
            Main.StatusUpdate("Forcing Market Update", 0);
            Task.Factory.StartNew(Main.dataBase.ForceMarketUpdate);
        }

        private void ReloadDropClick(object sender, RoutedEventArgs e)
        {
            ReloadDrop.IsEnabled = false;
            ReloadMarket.IsEnabled = false;
            Drop_Data.Content = "Loading...";
            Main.StatusUpdate("Forcing Prime Update", 0);
            Task.Factory.StartNew(Main.dataBase.ForceEquipmentUpdate);
        }

        // Allows the draging of the window
        private new void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                try
                {
                    DragMove();
                }
                catch (Exception)
                {
                    Main.AddLog("Error in Mouse down in mainwindow");
                }
        }

        private void OnLocationChanged(object sender, EventArgs e)
        {

            if (Settings.settingsObj.TryGetValue("MainWindowLocation_X", out _))
            {
                Settings.mainWindowLocation = new Point(Left, Top);
                Settings.settingsObj["MainWindowLocation_X"] = Left;
                Settings.settingsObj["MainWindowLocation_Y"] = Top;
                Settings.Save();
            }
            else
            {
                Settings.mainWindowLocation = new Point(100, 100);
                Settings.settingsObj["MainWindowLocation_X"] = 100;
                Settings.settingsObj["MainWindowLocation_Y"] = 100;
                Settings.Save();
            }
        }

        public void ToForeground(object sender, RoutedEventArgs e)
        {
            MainWindow.INSTANCE.Visibility = Visibility.Visible;
            MainWindow.INSTANCE.Activate();
            MainWindow.INSTANCE.Topmost = true;  // important
            MainWindow.INSTANCE.Topmost = false; // important
            MainWindow.INSTANCE.Focus();         // important
        }
    }
}