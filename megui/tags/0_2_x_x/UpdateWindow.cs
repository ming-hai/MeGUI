using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.BZip2;

using System.Text;
using MeGUI.core.util;


namespace MeGUI
{

    public partial class UpdateWindow : Form
    {
        private string[] serverList;
        
        private MainForm mainForm = null;
        public static MeGUISettings meGUISettings = null;
        private bool continueUpdate = true;
        private iUpgradeableCollection upgradeData = null;
        private Thread updateThread = null;
        private StringBuilder logBuilder = new StringBuilder();
        private System.Threading.ManualResetEvent webUpdate = new ManualResetEvent(false);
        private XmlDocument upgradeXml = null;
        public bool needsRestart = false;
        private bool isOrHasDownloadedUpgradeData = false;
        private string ServerAddress;

        #region Classes
        /// <summary>
        /// Helper method to parse a version numbers. Takes in a string and returns the numerical
        /// equivilent of it.
        /// </summary>
        /// <param name="str_version">The string containing the version number</param>
        /// <returns>a double indicating the version number</returns>
        public static int CompareVersionNumber(Version version1, Version version2)
        {
            if (version1 == null && version2 == null)
                return 0;
            else if (version1 == null)
                return 1;
            else if (version2 == null)
                return -1;
            return CompareVersionNumber(version1.FileVersion, version2.FileVersion);
        }
        public static int CompareVersionNumber(string version1, string version2)
        {
            if (string.IsNullOrEmpty(version1) && string.IsNullOrEmpty(version2))
                return 0;
            else if (string.IsNullOrEmpty(version1))
                return 1;
            else if (string.IsNullOrEmpty(version2))
                return -1;

            string[] versionStrings = { version1, version2 };
            int[] versionNumbers = new int[2];
            string[] smallVersionStrings = new string[2];

            for (int iVersion = 0; iVersion < versionStrings.Length; iVersion++)
            {
                char[] versionString = versionStrings[iVersion].ToCharArray();
                int numberStartIndex = -1;
                int textStartIndex = -1;
                int textStopIndex = -1;
                for (int iString = 0; iString < versionString.Length; iString++)
                {
                    if (numberStartIndex == -1)
                    {
                        if (char.IsDigit(versionString[iString]))
                            numberStartIndex = iString;
                    }
                    else if (textStartIndex == -1)
                    {
                        if (!char.IsDigit(versionString[iString]))
                            textStartIndex = iString;
                    }
                    else if (textStartIndex != -1)
                    {
                        if (char.IsDigit(versionString[iString]))
                        {
                            textStopIndex = iString;
                            break;
                        }
                    }
                }
                if (numberStartIndex >= 0 && textStartIndex > 0)
                    versionNumbers[iVersion] = int.Parse(versionStrings[iVersion].Substring(numberStartIndex, textStartIndex-numberStartIndex));
                else if (numberStartIndex >= 0)
                    versionNumbers[iVersion] = int.Parse(versionStrings[iVersion].Substring(numberStartIndex));
                if (textStopIndex > 0)
                    smallVersionStrings[iVersion] = versionStrings[iVersion].Substring(textStopIndex);
            }
            if (versionNumbers[0] != versionNumbers[1])
                return versionNumbers[1] - versionNumbers[0];
            return CompareVersionNumber(smallVersionStrings[0], smallVersionStrings[1]);
        }
        public abstract class iUpgradeable
        {
            /// <summary>
            /// May be overridden in a special init is to be done after the xmlserializer
            /// </summary>
            public virtual void init() { }
            
            internal iUpgradeable()
            {
                availableVersions = new Versions(2);

            } // constructor

            // Overrideable methods
            public Version GetLatestVersion()
            {
            
                Version latest = new Version();
                foreach (Version v in this.availableVersions)
                    if ( CompareVersionNumber(v, latest) < 0)
                        latest = v;

                return latest;
            }

            public bool HasAvailableVersions
            {
                get
                {
                    Version latest = GetLatestVersion();
                    return !(latest == null || latest.FileVersion == null ||
                   (latest != null && this.CurrentVersion != null &&
                   CompareVersionNumber(latest.FileVersion, currentVersion.FileVersion) >= 0));
                }
            }


            public ListViewItem CreateListViewItem()
            {
                ListViewItem myitem = new ListViewItem();
                ListViewItem.ListViewSubItem name = new ListViewItem.ListViewSubItem();
                ListViewItem.ListViewSubItem existingVersion = new ListViewItem.ListViewSubItem();
                ListViewItem.ListViewSubItem latestVersion = new ListViewItem.ListViewSubItem();
                ListViewItem.ListViewSubItem status = new ListViewItem.ListViewSubItem();

                myitem.Name = this.Name;

                name.Name = "Name";
                existingVersion.Name = "Existing Version";
                latestVersion.Name = "Latest Version";
                status.Name = "Status";

                name.Text = this.Name;

                Version v = GetLatestVersion();
                if (v != null)
                    latestVersion.Text = v.FileVersion;

                if (this.CurrentVersion != null)
                    existingVersion.Text = this.CurrentVersion.FileVersion;
                else
                    existingVersion.Text = "N/A";

                if (!HasAvailableVersions)
                {
                    status.Text = "No Updates Available";
                }
                else
                {
                    if (this.AllowUpdate)
                    {
                        status.Text = "Updates Available";
                        if (this.DownloadChecked)
                            myitem.Checked = true;
                        else
                            myitem.Checked = false;
                    }
                    else
                        status.Text = "Updates Ignored";
                }

                myitem.SubItems.Add(name);
                myitem.SubItems.Add(existingVersion);
                myitem.SubItems.Add(latestVersion);
                myitem.SubItems.Add(status);
                return myitem;
            }
            public abstract ErrorState Upgrade();

            
            private bool downloadChecked;
            public bool DownloadChecked
            {
                get { return downloadChecked; }
                set { downloadChecked = value; }
            }

            private string savePath;
            public string SavePath
            {
                get { return savePath; }
                set { savePath = value; }
            }
            private string saveFolder;
            public string SaveFolder
            {
                get { return saveFolder; }
                set { saveFolder = value; }
            }
	
            protected Version currentVersion;
            public virtual Version CurrentVersion
            {
                get
                {
                    if (currentVersion == null)
                        currentVersion = new Version();
                    return currentVersion;
                }
                set { currentVersion = value; }
            }

            private Versions availableVersions;
            public Versions AvailableVersions
            {
                get { return availableVersions; }
                set { availableVersions = value; }
            }

            private bool allowUpdate;
            public bool AllowUpdate
            {
                get { return allowUpdate; }
                set
                {
                    if (!value)
                        downloadChecked = false;
                    allowUpdate = value;
                }
            }

            public bool Reinstall;

            private string name;
            public string Name
            {
                get { return this.name; }
                set { this.name = value; }
            }

            internal string treeViewID;
            public string TreeViewID
            {
                get { return this.treeViewID; }
                set { this.treeViewID = value; }
            }

            private bool needsRestartedCopying = false;
            public bool NeedsRestartedCopying
            {
                get { return needsRestartedCopying; }
                set { needsRestartedCopying = value; }
            }

            private uint installPriority = 0;

            public uint InstallPriority
            {
                get { return installPriority; }
                set { installPriority = value; }
            }


            public virtual bool NeedsInstalling
            {
                get { return false; }
            }

            public virtual ErrorState Install(Stream fileData)
            {
                throw new Exception("This kind of file cannot be installed.");
            }
        }
        public class iUpgradeableCollection : CollectionBase
        {
            public iUpgradeableCollection() { }
            public iUpgradeableCollection(int capacity)
            {
                this.InnerList.Capacity = capacity;
            }

            public iUpgradeable this[int index]
            {
                get { return (iUpgradeable)this.List[index]; }
                set { this.List[index] = value; }
            }
            public void Add(iUpgradeable item)
            {
                if (FindByName(item.Name) != null)
                    throw new Exception("Can't have multiple upgradeable items with the same name");
                this.InnerList.Add(item);
            }
            public void Remove(iUpgradeable item)
            {
                this.InnerList.Remove(item);
            }
            public iUpgradeable FindByName(string name)
            {
                foreach (iUpgradeable file in this.InnerList)
                {
                    if (file.Name.Equals(name))
                        return file;
                }
                return null;
            }
            public int CountCheckedFiles()
            {
                int count=0;
                foreach (iUpgradeable file in this.InnerList)
                {
                    if (file.DownloadChecked)
                        count++;
                }
                return count;
            }
        }
        public class ProfilesFile : iUpgradeable
        {
            public ProfilesFile()
            {
            }

            public ProfilesFile(string treeviewid, string name, MainForm mainForm)
            {
                this.treeViewID = treeviewid;
                this.Name = name;
                this.AllowUpdate = true;
                this.mainForm = mainForm;
            }
            
            private MainForm mainForm;

            public MainForm MainForm
            {
                set { mainForm = value; }
            }

            public override bool NeedsInstalling
            {
                get
                {
                    return true;
                }
            }

            public override ErrorState Install(Stream fileData)
            {
                try 
                {
                    mainForm.importProfiles(fileData);
                }
                catch (IOException)
                {
                    return ErrorState.CouldNotInstall;
                }
                return ErrorState.Successful;
            }

            public override ErrorState Upgrade()
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }
        public class AviSynthFile : iUpgradeable
        {
            private AviSynthFile()
            {
                this.SaveFolder = MeGUISettings.AvisynthPluginsPath;
            }
            public AviSynthFile(string treeviewid, string name)
            {
                this.Name = name;
                this.AllowUpdate = true;
                this.treeViewID = treeviewid;
                this.SaveFolder = MeGUISettings.AvisynthPluginsPath;
            }

            public override ErrorState Upgrade()
            {
                return ErrorState.CouldNotDownloadFile;
            }
        }
        public class MeGUIFile : iUpgradeable
        {
            private MeGUIFile()
            {
            }

            public MeGUIFile(string treeViewID, string name)
            {
                this.Name = name;
                this.AllowUpdate = true;
                this.treeViewID = treeViewID;
                this.SaveFolder = Application.StartupPath;
            }

            public override Version CurrentVersion
            {
                get
                {
                    if (Name == "core")
                    {
                        base.CurrentVersion.FileVersion = Application.ProductVersion;
                    }
                    return base.CurrentVersion;
                }
                set
                {
                    base.CurrentVersion = value;
                }
            }


            public override ErrorState Upgrade()
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }
        public class ProgramFile : iUpgradeable
        {
            private ProgramFile()
            {
            }

            public override void init()
            {
                if (MeGUIFilePath == null)
                    throw new FileNotRegisteredYetException(Name);
                SavePath = Path.Combine(Application.StartupPath, MeGUIFilePath);
                // If the file doesn't exist, assume it isn't set up, so put it in the standard install location
                if (!File.Exists(SavePath))
                {
                    string extension = Path.GetExtension(SavePath);
                    SavePath = Path.Combine(Path.Combine(System.Windows.Forms.Application.StartupPath, "tools\\" + Name),
                        Name + extension);
                    MeGUIFilePath = SavePath;
                }
            }
            
            public ProgramFile(string treeViewID, string name) // Constructor
            {
                this.Name = name;
                this.AllowUpdate = true;
                this.treeViewID = treeViewID;
                init();
            }

            private string MeGUIFilePath
            {
                get
                {
                    switch (this.Name)
                    {
                        case ("dgindex"):
                            return meGUISettings.DgIndexPath;
                        case ("faac"):
                            return meGUISettings.FaacPath;
                        case ("mencoder"):
                            return meGUISettings.MencoderPath;
                        case ("mkvmerge"):
                            return meGUISettings.MkvmergePath;
                        case ("lame"):
                            return meGUISettings.LamePath;
                        case ("mp4box"):
                            return meGUISettings.Mp4boxPath;
                        case ("neroaacenc"):
                            return meGUISettings.NeroAacEncPath;
                        case ("avimux_gui"):
                            return meGUISettings.AviMuxGUIPath;
                        case ("x264"):
                            return meGUISettings.X264Path;
                        case ("xvid_encraw"):
                            return meGUISettings.XviDEncrawPath;
                        case ("ffmpeg"):
                            return meGUISettings.FFMpegPath;
                        case ("encaudxcli"):
                            return meGUISettings.EncAudXPath;
                        case ("enc_aacplus"):
                            return meGUISettings.EncAacPlusPath;
                        case ("oggenc2"):
                            return meGUISettings.OggEnc2Path;
                        case ("avc2avi"):
                            return meGUISettings.Avc2aviPath;
                        case ("yadif"):
                            return meGUISettings.YadifPath;
                        default:
                            return null;
                    }
                }
                set
                {
                    switch (this.Name)
                    {
                        case ("oggenc2"):
                            meGUISettings.OggEnc2Path = value;
                            return;
                        case ("avc2avi"):
                            meGUISettings.Avc2aviPath = value;
                            return;
                        case ("dgindex"):
                            meGUISettings.DgIndexPath = value;
                            break;
                        case ("faac"):
                            meGUISettings.FaacPath = value;
                            break;
                        case ("lame"):
                            meGUISettings.LamePath = value;
                            break;
                        case ("mencoder"):
                            meGUISettings.MencoderPath = value;
                            break;
                        case ("mkvmerge"):
                            meGUISettings.MkvmergePath = value;
                            break;
                        case ("mp4box"):
                            meGUISettings.Mp4boxPath = value;
                            break;
                        case ("neroaacenc"):
                            meGUISettings.NeroAacEncPath = value;
                            break;
                        case ("avimux_gui"):
                            meGUISettings.AviMuxGUIPath = value;
                            break;
                        case ("x264"):
                            meGUISettings.X264Path = value;
                            break;
                        case ("xvid_encraw"):
                            meGUISettings.XviDEncrawPath = value;
                            break;
                        case ("ffmpeg"):
                            meGUISettings.FFMpegPath = value;
                            return;
                        case ("encaudxcli"):
                            meGUISettings.EncAudXPath = value;
                            return;
                        case ("enc_aacplus"):
                            meGUISettings.EncAacPlusPath = value;
                            return;
                        case ("yadif"):
                            meGUISettings.YadifPath = value;
                            return;
                    }
                }
            }

            public override ErrorState Upgrade()
            {
                return ErrorState.CouldNotDownloadFile;
            }
        }
        public class Version
        {
            public Version()
            {
            }
            public Version(string version, string url)
            {
                this.fileVersion = version;
                this.url = url;
            }
            private string fileVersion;
            private string url;

            public string FileVersion
            {
                get { return fileVersion; }
                set { fileVersion = value; }
            }
            public string Url
            {
                get { return url; }
                set { url = value; }
            }
        }
        public class Versions : CollectionBase
        {
            public Versions()
            {
                this.Capacity = 2;
            }
            public Versions(int capacity)
            {
                this.Capacity = capacity;
            }
            public Version this[int index]
            {
                get { return (Version)this.List[index]; }
                set { this.List[index] = value; }
            }
            public void Add(Version item)
            {
                this.InnerList.Add(item);
            }
            public void Remove(Version item)
            {
                this.InnerList.Remove(item);
            }
        }
        #endregion
        #region Delegates and delegate methods
        delegate void BeginParseUpgradeXml(XmlNode node, XmlNode groupNode, string path);
        private delegate void SetLogText();
        private delegate void SetListView(ListViewItem item);
        private delegate void ClearItems(ListView listview);

        private delegate void UpdateProgressBar(int minValue, int maxValue, int currentValue);
        private void SetProgressBar(int minValue, int maxValue, int currentValue)
        {
            if (this.progressBar.InvokeRequired)
            {
                try
                {
                    UpdateProgressBar d = new UpdateProgressBar(SetProgressBar);
                    progressBar.Invoke(d, minValue, maxValue, currentValue);
                }
                catch (Exception) { }
            }
            else
            {
                this.progressBar.Minimum = (int)minValue;
                this.progressBar.Maximum = (int)maxValue;
                this.progressBar.Value = (int)currentValue;
            }
        }
        private void AddTextToLog(string text)
        {
            logBuilder.AppendLine(text);
            if (!this.Visible)
                return;
            SetLogText d = new SetLogText(UpdateLogText);
            if (this.txtBoxLog.InvokeRequired)
                this.Invoke(d);
            else
                d();
        }

        private void UpdateLogText()
        {
            this.txtBoxLog.Text = logBuilder.ToString();
            this.txtBoxLog.SelectionStart = txtBoxLog.Text.Length;
            this.txtBoxLog.ScrollToCaret();
        }

        private void AddToListview(ListViewItem item)
        {
            if (this.listViewDetails.InvokeRequired)
            {
                SetListView d = new SetListView(AddToListview);
                this.Invoke(d, item);
            }
            else
                this.listViewDetails.Items.Add(item);
   
        }
        private void ClearListview(ListView listview)
        {
            if (listview.InvokeRequired)
            {
                ClearItems d = new ClearItems(ClearListview);
                this.Invoke(d, listview);
            }
            else
            {
                listview.Items.Clear();
            }
        }
        #endregion
        #region Enums
        public enum ErrorState
        {
            FileNotOnServer,
            ServerNotAvailable,
            CouldNotDownloadFile,
            CouldNotRemoveBackup,
            CouldNotSaveNewFile,
            CouldNotRenameExistingFile,
            CouldNotInstall,
            Successful,
            CouldNotUnzip,
            InvalidXML
        }
        #endregion
        #region con/de struction
        private void UpdateWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
        }
        
        /// <summary>
        /// Constructor for Updatewindow.
        /// </summary>
        /// <param name="savedSettings">Current MeGUI settings</param>
        public UpdateWindow(MainForm mainForm, MeGUISettings savedSettings)
        {
            InitializeComponent();
            this.mainForm = mainForm;
            this.upgradeData = new iUpgradeableCollection(32); // To avoid unnecessary resizing, start at 32.
            meGUISettings = savedSettings; // Load up the MeGUI settings so i can access filepaths

            this.serverList = shuffled(mainForm.Settings.AutoUpdateServerLists[mainForm.Settings.AutoUpdateServerSubList]);
            if (serverList.Length == 0)
            {
                MessageBox.Show("Couldn't run auto-update since there are no servers registered.", "No servers registered", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            LoadSettings();
        }

        private string[] shuffled(string[] serverList)
        {
            Random r = new Random();
            SortedList<int, string> shuffled = new SortedList<int, string>();
            // The first element is the name
            for (int i = 1; i < serverList.Length; i++)
                shuffled.Add(r.Next(),serverList[i]);

            string[] array = new string[shuffled.Count];
            shuffled.Values.CopyTo(array, 0);
            return array;
        }

        private void UpdateWindow_Load(object sender, EventArgs e)
        {
            GetUpdateData(false);
        }
        #endregion
        #region load and save
        private void LoadSettings()
        {
            if (File.Exists(Application.StartupPath + "\\AutoUpdate.xml"))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(iUpgradeableCollection), new Type[] { typeof(ProgramFile), typeof(AviSynthFile), typeof(ProfilesFile) , typeof(MeGUIFile)});
                    StreamReader settingsReader = new StreamReader(Application.StartupPath + "\\AutoUpdate.xml");
                    this.upgradeData = (iUpgradeableCollection)serializer.Deserialize(settingsReader);
                    settingsReader.Dispose();

                    foreach (iUpgradeable file in upgradeData)
                    {
                        try
                        {
                            file.init();
                        }
                        catch (FileNotRegisteredYetException) { }
                    }

                    return; //settings loaded correctly
                }
                catch(Exception)
                {
                    MessageBox.Show("Error: Could not load previous settings", "Error", MessageBoxButtons.OK);
                    return; // error loading settings
                }
            }
        }
        public void SaveSettings()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(iUpgradeableCollection), new Type[] { typeof(ProgramFile), typeof(AviSynthFile), typeof(ProfilesFile), typeof(MeGUIFile) });
                StreamWriter output = new StreamWriter(Application.StartupPath + "\\AutoUpdate.xml", false);
                serializer.Serialize(output, this.upgradeData);
                output.Dispose();
                return; //settings saved
            }
            catch (Exception)
            {
                MessageBox.Show("Error: Could not save settings", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        #endregion
        #region getting update data
        public void GetUpdateData(bool wait)
        {
            if (!isOrHasDownloadedUpgradeData)
            {
                if (treeView.InvokeRequired)
                {
                    treeView.Invoke(new MethodInvoker(delegate
                    {
                        treeView.Nodes.Clear(); // just in case, remove all nodes
                        treeView.Nodes.Add("UpdateableFiles", "UpdateableFiles");
                        treeView.SelectedNode = treeView.Nodes["UpdateableFiles"];
                    }));
                }
                else
                {
                    treeView.Nodes.Clear(); // just in case, remove all nodes
                    treeView.Nodes.Add("UpdateableFiles", "UpdateableFiles");
                    treeView.SelectedNode = treeView.Nodes["UpdateableFiles"];
                }

                Thread CreateTreeview = new Thread(new ThreadStart(ProcessUpdateXML));
                CreateTreeview.IsBackground = true;
                CreateTreeview.Start();
                if (wait)
                    webUpdate.WaitOne();
            }
        }
        /// <summary>
        /// This method is called to retrieve the update data from the webserver
        /// and then set the relevant information to the grid.
        /// </summary>
        private ErrorState GetUpdateXML()
        {
            if (upgradeXml != null) // the update file has already been downloaded and processed
                return ErrorState.Successful;

            WebClient serverClient = new WebClient();
            upgradeXml = new XmlDocument();
            string data = null;

            try
            {
                AddTextToLog("Retrieving update file from server...");
                data = serverClient.DownloadString(ServerAddress + "upgrade.xml?offCache=" + System.Guid.NewGuid().ToString("N"));
                AddTextToLog("File downloaded successfully...");
            }
            catch
            {
                AddTextToLog("Error: Couldn't connect to server.");
                upgradeXml = null;
                return ErrorState.ServerNotAvailable;
            }

            try
            {
                AddTextToLog("Loading update data...");
                upgradeXml.LoadXml(data);
                AddTextToLog("Update data loaded successfully...");
            }
            catch
            {
                AddTextToLog("Error: Invalid XML file on server. Aborting.");
                upgradeXml = null;
                return ErrorState.InvalidXML;
            }
            
            AddTextToLog("Finished parsing update file...");
            return ErrorState.Successful;
        }
        /// <summary>
        /// This function downloads the update XML file from the server and then processes it.
        /// </summary>
        private void ProcessUpdateXML()
        {
            isOrHasDownloadedUpgradeData = true;
            ErrorState value = ErrorState.ServerNotAvailable;
            int count = 0;
            foreach (string serverName in serverList)
            {
                count++;
                if (count > mainForm.Settings.MaxServersToTry)
                    break;
                ServerAddress = serverName;
                AddTextToLog("Trying server: " + serverName);
                value = GetUpdateXML();
                if (value == ErrorState.Successful)
                    break;
            }
            
            if (value != ErrorState.Successful)
            {
                AddTextToLog("Error: Could not download XML file");
                return;
            }
            // I'd prefer the main thread to parse the upgradeXML as opposed to using this
            // "downloading" thread but i didn't know a better way of doing it other than
            // using a delegate like this.
            BeginParseUpgradeXml d = new BeginParseUpgradeXml(ParseUpgradeXml);
            XmlNode node = this.upgradeXml.SelectSingleNode("/UpdateableFiles");

            if (node != null) // xml file could be dodgy.
            {
                if (listViewDetails.InvokeRequired)
                {
                    listViewDetails.Invoke(d, node, null, node.Name);
                }
                else
                {
                    d(node, null, node.Name);
                }
            }
            AddTextToLog(string.Format("There are {0} files that can be updated.", NumUpdatableFiles()));
            webUpdate.Set();
        }
        /// <summary>
        /// Parses the upgrade XML file to create both the TreeView and populate the
        /// upgradeData array. It's a recursive algorithm, so it needs to be passed
        /// the root node off the upgrade XML to start off, and it will then recurse
        /// through all the nodes in the file.
        /// </summary>
        /// <param name="currentNode">The node that the function should work on</param>
        private void ParseUpgradeXml(XmlNode currentNode, XmlNode groupNode, string path)
        {
            TreeNode selectednode = treeView.SelectedNode;

            foreach (XmlNode childnode in currentNode.ChildNodes)
            {
                if (childnode.Attributes["type"].Value.Equals("file"))
                {
                    ParseFileData(childnode, groupNode, path);
                    continue;
                }
                string displayName = childnode.Name;
                try
                {
                    displayName = childnode.Attributes["displayname"].Value;
                }
                catch (Exception) { }
    
                string newPath = path + "." + childnode.Name;
                treeView.SelectedNode = selectednode.Nodes.Add(newPath, displayName);

                if (childnode.Attributes["type"].Value.Equals("tree"))
                    ParseUpgradeXml(childnode, childnode, newPath);
                else if (childnode.Attributes["type"].Value.Equals("subtree"))
                    ParseUpgradeXml(childnode, groupNode, newPath);
            }
            treeView.SelectedNode = selectednode;
        }
        /// <summary>
        /// Once a "file" is found in the upgrade XML file, the files node is passed
        /// to this function which generates the correct iUpgradeable filetype (i.e. MeGUIFile
        /// or AviSynthFile) and then fills in all the relevant data.
        /// </summary>
        /// <param name="node"></param>
        private void ParseFileData(XmlNode node, XmlNode groupNode, string treePath)
        {
            iUpgradeable file = null;
            Version availableFile = null;
            bool fileAlreadyAdded = false;
            
            if ((file = upgradeData.FindByName(node.Name)) == null) // If this file isn't already in
            {                                                       // the upgradeData list.
                try
                {
                    if (groupNode.Name.Equals("MeGUI"))
                        file = new MeGUIFile(treePath, node.Name);
                    else if (groupNode.Name.Equals("ProgramFile"))
                        file = new ProgramFile(treePath, node.Name);
                    else if (groupNode.Name.Equals("AviSynthFile"))
                        file = new AviSynthFile(treePath, node.Name);
                    else if (groupNode.Name.Equals("ProfilesFile"))
                        file = new ProfilesFile(treePath, node.Name, mainForm);
                    else
                        return;
                }
                catch (FileNotRegisteredYetException)
                {
                    return;
                }
            }
            else
            {
                file.AvailableVersions = new Versions();
                file.DownloadChecked = false;
                file.treeViewID = treePath;
                fileAlreadyAdded = true;
                if (file is ProfilesFile)
                    (file as ProfilesFile).MainForm = mainForm;
            }
            try
            {
                if (node.Attributes["needsrestart"].Value.Equals("true"))
                    file.NeedsRestartedCopying = true;
                else
                    file.NeedsRestartedCopying = false;
            }
            catch (Exception) { }
            
            try
            {
                file.InstallPriority = uint.Parse(node.Attributes["installpriority"].Value);
            }
            catch (Exception) { }

            foreach (XmlNode filenode in node.ChildNodes) // each filenode contains the upgrade url and version
            {
                availableFile = new Version();

                availableFile.FileVersion = filenode.Attributes["version"].Value;
                availableFile.Url = filenode.FirstChild.Value;

                file.AvailableVersions.Add(availableFile);
            }
            if ( CompareVersionNumber(file.GetLatestVersion(), file.CurrentVersion) < 0 && file.AllowUpdate)
                file.DownloadChecked = true;

            if (!fileAlreadyAdded)
                upgradeData.Add(file);
        }
        #endregion
        #region GUI
        private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            DisplayItems(e.Node.Name);
        }
        private void DisplayItems(string selectednode)
        {
            ClearListview(this.listViewDetails);

            foreach (iUpgradeable file in upgradeData)
            {
                if (file.treeViewID.StartsWith(selectednode))
                {
                    AddToListview(file.CreateListViewItem());
                }
            }
        }
        private void listViewDetails_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            ListViewItem itm = this.listViewDetails.Items[e.Index];
            // Do not allow checking if there are no updates or it is set to ignore.
            if (itm.SubItems["Status"].Text.Equals("No Updates Available")
                || itm.SubItems["Status"].Text.Equals("Updates Ignored"))
                e.NewValue = CheckState.Unchecked;


            iUpgradeable file = upgradeData.FindByName(itm.Name);
            if (e.NewValue == CheckState.Checked)
                file.DownloadChecked = true;
            else
                file.DownloadChecked = false;

            if (e.NewValue == CheckState.Unchecked && itm.SubItems["Status"].Text == "Reinstalling")
                itm.SubItems["Status"].Text = file.AllowUpdate ? (file.HasAvailableVersions ? "Updates Available" : "No Updates Available") : "Updates Ignored";
        }

        private void listViewDetails_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (listViewDetails.SelectedItems.Count > 0)
                {
                    ToolStripMenuItem ts = (ToolStripMenuItem)statusToolStrip.Items[0];
                    bool allowupdate = false;
                    foreach (ListViewItem item in listViewDetails.SelectedItems)
                    {
                        allowupdate |= upgradeData.FindByName(item.Name).AllowUpdate;
                    }
                    ts.Checked = !allowupdate;
                    statusToolStrip.Show(Cursor.Position);
                }
            }
        }

        private void setIgnoreValue_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem ts = (ToolStripMenuItem)sender;
            foreach (ListViewItem item in listViewDetails.SelectedItems)
            {
                iUpgradeable file = upgradeData.FindByName(item.Name);

                file.AllowUpdate = !(ts.Checked);
                Version latest = file.GetLatestVersion();

                if (file.AllowUpdate)
                {
                    if (latest == null && file.CurrentVersion == null)
                    {
                        item.SubItems["Status"].Text = "No Updates Available";
                        item.Checked = false;
                    }
                    else if (latest != null && file.CurrentVersion == null)
                    {
                        item.SubItems["Status"].Text = "Updates Available";
                        item.Checked = true;
                    }
                    else if (CompareVersionNumber(latest, file.CurrentVersion) < 0)
                    {
                        item.SubItems["Status"].Text = "Updates Available";
                        item.Checked = true;
                    }
                    else
                        item.SubItems["Status"].Text = "No Updates Available";
                }
                else
                {
                    item.Checked = false;
                    item.SubItems["Status"].Text = "Updates Ignored";
                }
            }
        }
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (MeGUISettings.AvisynthPluginsPath == "")
            {
                MessageBox.Show("Error: Avisynth plugins path is not set (check if you have Avisynth installed and ensure to set the path to plugins in the settings).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnUpdate.Enabled = false;
            }
            else
            {
                btnUpdate.Enabled = false;
                btnAbort.Enabled = true;
                updateThread = new Thread(new ThreadStart(BeginUpdate));
                updateThread.IsBackground = true;
                updateThread.Start();
            }
        }
        #endregion
        #region updating
        private /*bool*/ void InstallFiles(SortedDictionary<uint, List<iUpgradeable>> groups /*, bool silentMode*/)
        {
            continueUpdate = true;
            int currentFile = 1; //the first file we update is file 1.
            ErrorState result;
            List<iUpgradeable> succeededFiles = new List<iUpgradeable>();
            List<iUpgradeable> failedFiles = new List<iUpgradeable>();

            
            // Count the number of files we can update before we restart
            int updateableFileCount = 0;
            uint indexOfRestart = 0;
            bool needsRestart = false;
            foreach (List<iUpgradeable> group in groups.Values)
            {
                foreach (iUpgradeable file in group)
                {
                    if (file.NeedsRestartedCopying)
                        needsRestart = true;
                }
                updateableFileCount += group.Count;
                if (needsRestart)
                {
                    indexOfRestart = group[0].InstallPriority;
                    break;
                }
            }


            // Now update the files we can
            foreach (List<iUpgradeable> group in groups.Values)
            {
                foreach (iUpgradeable file in group)
                {
                    if (!continueUpdate)
                    {
                        AddTextToLog("Update aborted by user.");
                        return /* false*/;
                    }

                    AddTextToLog(string.Format("Updating {0}. File {1}/{2}.",
                        file.Name, currentFile, updateableFileCount));

                    Stream str;

                    if ((result = UpdateCacher.DownloadFile(file.GetLatestVersion().Url, new Uri(ServerAddress),
                        out str, wc_DownloadProgressChanged)) 
                        != ErrorState.Successful)
                        failedFiles.Add(file);
                    else
                    {
                        try
                        {
                            ErrorState state;
                            if (file.NeedsInstalling)
                                state = Install(file, str);
                            else
                                state = SaveNewFile(file, str);

                            if (state != ErrorState.Successful)
                                failedFiles.Add(file);
                            else
                            {
                                succeededFiles.Add(file);
                                file.DownloadChecked = false;
                            }
                        }
                        finally { str.Close(); }
                    }
                    currentFile++;
                }

                if (currentFile >= updateableFileCount) break;
            }

            // Tell MeGUI to update the remaining files after restarting
            bool firstTime = true;
            foreach (List<iUpgradeable> group in groups.Values)
            {
                foreach (iUpgradeable file in group)
                {
                    if (file.InstallPriority > indexOfRestart)
                    {
                        if (firstTime)
                            AddTextToLog(string.Format("The following files could not be updated, since they depend on another component which will only be installed when MeGUI restarts. Please run the updater later.{0}", Environment.NewLine));
                        firstTime = false;
                        AddTextToLog(file.Name + Environment.NewLine);
                        // Maybe useful later
                        //mainForm.AddFileToInstall(file.Name);
                    }
                }
            }


            SetProgressBar(0, 1, 1); //make sure progress bar is at 100%.

            if (failedFiles.Count > 0)
                AddTextToLog(string.Format("Update completed.{2}{0} files were completed successfully{2}{1} files had problems.",
                    succeededFiles.Count, failedFiles.Count, Environment.NewLine));
            else
                AddTextToLog(string.Format("Update completed successfully. {0} files updated", succeededFiles.Count));

            List<string> files = new List<string>();
            foreach (iUpgradeable u in upgradeData)
            {
                files.Add(u.GetLatestVersion().Url);
            }
            UpdateCacher.flushOldCachedFilesAsync(files);

            if (needsRestart)
            {
                if (/*silentMode ||*/
                    MessageBox.Show("In order to finish the update, MeGUI needs to be restarted. Do you want to restart now?",
                    "Restart now?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    mainForm.Restart = true;
                    this.Invoke(new MethodInvoker(delegate { this.Close(); }));
                    mainForm.Invoke(new MethodInvoker(delegate { mainForm.Close(); }));
                    return/* true*/;
                }
            }
            treeView.Invoke(new MethodInvoker(delegate { DisplayItems(treeView.SelectedNode.Name); }));
            Invoke(new MethodInvoker(delegate
            {
                btnAbort.Enabled = false;
                btnUpdate.Enabled = true;
            }));
            /*return false;*/
        }

        /// <summary>
        /// This function iterates through all the selected files and downloads them
        /// one by one.
        /// </summary>
        private void BeginUpdate()
        {

            // Sort the files to download according to their install priority
            SortedDictionary<uint, List<iUpgradeable>> groups = new SortedDictionary<uint, List<iUpgradeable>>();
            foreach (iUpgradeable file in upgradeData)
            {
                if (file.DownloadChecked)
                {
                    if (!groups.ContainsKey(file.InstallPriority))
                        groups[file.InstallPriority] = new List<iUpgradeable>();
                    groups[file.InstallPriority].Add(file);
                }
            }

            InstallFiles(groups /*, false*/ );
        }

        private ErrorState Install(iUpgradeable file, Stream fileData)
        {
            ErrorState state = file.Install(fileData);
            if (state == ErrorState.Successful)
            {
                file.CurrentVersion = file.GetLatestVersion();
                return ErrorState.Successful;
            }

            AddTextToLog(string.Format("Could not install module '{0}'.", file.Name));
            return state;
        }

        /// <summary>
        /// This function takes in the byte array containing a downloaded file
        /// and the iUpgradeable file and saves the new file to the disk, it also upzips
        /// the file if necessary.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private ErrorState SaveNewFile(iUpgradeable file, Stream data)
        {
            string filepath = null, filename = null;
            if (file.SaveFolder != null)
                filepath = file.SaveFolder;
            else if (file.SavePath != null)
                filepath = Path.GetDirectoryName(file.SavePath);
            else
            {
                AddTextToLog("Error: The path to save " + file.Name + " to is invalid.");
                return ErrorState.CouldNotSaveNewFile;
            }
            if (file.SavePath != null)
                filename = file.SavePath;
            else
                filename = filepath + @"\" + Path.GetFileName(file.GetLatestVersion().Url);

            try
            {
                if (!Directory.Exists(filepath))
                    Directory.CreateDirectory(filepath);
            }
            catch (IOException)
            {
                AddTextToLog(string.Format("Error: Could not create directory {0}.", filepath));
                return ErrorState.CouldNotSaveNewFile;
            }

            if (file.GetLatestVersion().Url.EndsWith(".zip"))
            {
                try
                {
                    using (ZipInputStream zip = new ZipInputStream(data))
                    {
                        ZipEntry zipentry;

                        while ((zipentry = zip.GetNextEntry()) != null)
                        {
                            filename = Path.Combine(filepath, zipentry.Name);
                            if (zipentry.IsDirectory)
                            {
                                if (!Directory.Exists(filename))
                                    Directory.CreateDirectory(filename);
                                continue;
                            }
                            // create the output writer to save the file onto the harddisc
                            string oldFileName = null;
                            if (file.NeedsRestartedCopying)
                            {
                                oldFileName = filename;
                                filename += ".tempcopy";
                            }
                            else
                            {
                                ErrorState result = manageBackups(filename, file.Name);
                                if (result != ErrorState.Successful)
                                    return result;
                            }
                            using (Stream outputWriter = new FileStream(filename, FileMode.Create))
                            {
                                FileUtil.copyData(zip, outputWriter);
                            }
                            if (file.NeedsRestartedCopying)
                            {
                                mainForm.AddFileToReplace(file.Name, filename, oldFileName, file.GetLatestVersion().FileVersion);
                                needsRestart = true;
                            }
                        }
                        if (!file.NeedsRestartedCopying)
                            file.CurrentVersion = file.GetLatestVersion(); // the current installed version
                        // is now the latest available version
                    }
                }
                catch
                {
                    AddTextToLog("Error: Could not unzip " + file.Name + ". Deleting file. Please run updater again...");
                    UpdateCacher.FlushFile(file.GetLatestVersion().Url);
                    return ErrorState.CouldNotUnzip;
                }
            }
            else
            {
                string oldFileName = null;
                if (file.NeedsRestartedCopying)
                {
                    oldFileName = filename;
                    filename = filename + ".tempcopy";
                }
                else
                {
                    ErrorState result = manageBackups(filename, file.Name);
                    if (result != ErrorState.Successful)
                        return result;
                }
                try
                {
                    using (Stream output = File.OpenWrite(filename))
                    {
                        
                        //filename, data);
                        if (file.NeedsRestartedCopying)
                        {
                            mainForm.AddFileToReplace(file.Name, filename, oldFileName, file.GetLatestVersion().FileVersion);
                            needsRestart = true;
                        }
                        else
                            file.CurrentVersion = file.GetLatestVersion(); // current installed version
                        // is now the latest available version
                    }
                }
                catch
                {
                    AddTextToLog("Error: Latest version of " + file.Name + " could not be saved to disk. Check there is enough free space.");
                    return ErrorState.CouldNotSaveNewFile;
                }
            }
            return ErrorState.Successful;
        }

        private ErrorState manageBackups(string savePath, string name)
        {
            try
            {
                if (File.Exists(savePath + ".backup"))
                    File.Delete(savePath + ".backup");
            }
            catch
            {
                AddTextToLog("Error: Outdated backup version of " + name + " could not be deleted. Check if it is in use.");
                return ErrorState.CouldNotRemoveBackup;
            }
            try
            {
                if (File.Exists(savePath))
                    File.Move(savePath, (savePath + ".backup"));
            }
            catch
            {
                AddTextToLog("Error: Old version of " + name + " could not be backed up correctly. Restart MeGUI and try again.");
                return ErrorState.CouldNotRenameExistingFile;
            }
            return ErrorState.Successful;
        }

        void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (e.TotalBytesToReceive > 0)
                SetProgressBar(0, (int)e.TotalBytesToReceive, (int)e.BytesReceived);
        }

        public bool HasUpdatableFiles()
        {
            return NumUpdatableFiles() > 0;
        }

        public int NumUpdatableFiles()
        {
            int numUpdateableFiles = 0;
            foreach (iUpgradeable upgradeable in upgradeData)
            {
                if (upgradeable.AllowUpdate && 
                    CompareVersionNumber(upgradeable.GetLatestVersion(), upgradeable.CurrentVersion)< 0)
                    numUpdateableFiles++;
            }
            return numUpdateableFiles;
        }

        public void UpdateVersionNumber(string name, string version)
        {
            iUpgradeable up = upgradeData.FindByName(name);
            if (up == null)
                return;
            up.CurrentVersion.FileVersion = version;
        }

        private void checkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem ts = (ToolStripMenuItem)sender;
            foreach (ListViewItem item in listViewDetails.SelectedItems)
                item.Checked = true;
        }

        private void uncheckToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem ts = (ToolStripMenuItem)sender;
            foreach (ListViewItem item in listViewDetails.SelectedItems)
                item.Checked = false;
        }

        private void reinstallToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewDetails.SelectedItems)
            {
                item.SubItems["Status"].Text = "Reinstalling";
                item.Checked = true;
            }
        }

        private void btnAbort_Click(object sender, EventArgs e)
        {
            updateThread.Abort();
            btnUpdate.Enabled = true;
            btnAbort.Enabled = false;
        }

        /// This function is intended to silently install the list of files, restarting if necessary. 
        /// It doesn't work yet, and was left out because it was too much work for this point in time.
        /*
        public bool InstallFiles(List<string> list)
        {
            SortedDictionary<uint, List<iUpgradeable>> files = new SortedDictionary<uint,List<iUpgradeable>>();
            foreach (string name in list)
            {
                iUpgradeable file = upgradeData.FindByName(name);
                if (file != null)
                {
                    if (!files.ContainsKey(file.InstallPriority))
                        files[file.InstallPriority] = new List<iUpgradeable>();
                    files[file.InstallPriority].Add(file);
                }
            }
            return InstallFiles(files, true);
        }*/
    }
    public class UpdateOptions : MeGUI.core.plugins.interfaces.IOption
    {

        #region IOption Members

        public string Name
        {
            get { return "Update"; }
        }

        public void Run(MainForm info)
        {
            UpdateWindow update = new UpdateWindow(info, info.Settings);
            update.ShowDialog();
        }

        public Shortcut[] Shortcuts
        {
            get { return new Shortcut[] { Shortcut.CtrlU }; }
        }

        #endregion

        #region IIDable Members

        public string ID
        {
            get { return "update_window"; }
        }

        #endregion
    }

    public class FileNotRegisteredYetException : MeGUIException
    {
        private string name;

        public string Name { get { return name; } }
        public FileNotRegisteredYetException(string name) : base("AutoUpdate file '" + name + "' not registered with MeGUI.")
        {
            this.name = name;
        }
    }
        #endregion
}
