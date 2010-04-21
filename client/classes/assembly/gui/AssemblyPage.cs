﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using com.jds.GUpdater.classes.forms;
using com.jds.GUpdater.classes.language;
using com.jds.GUpdater.classes.language.enums;
using com.jds.GUpdater.classes.listloader;
using com.jds.GUpdater.classes.listloader.enums;
using com.jds.GUpdater.classes.task_manager;
using com.jds.GUpdater.classes.task_manager.tasks;
using com.jds.GUpdater.classes.utils;
using com.jds.GUpdater.classes.windows.windows7;
using log4net;

namespace com.jds.GUpdater.classes.assembly.gui
{
    public partial class AssemblyPage : UserControl
    {
        #region Instance & Constructor & Variables

        private readonly GUListLoaderTask _listLoaderTask = new GUListLoaderTask();
        private readonly ILog _log = LogManager.GetLogger(typeof (AssemblyPage));
        private readonly LinkedList<DelegateCall> _notifys = new LinkedList<DelegateCall>();

        private static AssemblyPage _instance;
       
        public  static  AssemblyPage Instance
        {
            get
            {
                return _instance ?? (_instance = new AssemblyPage());
            }
        }

        private AssemblyPage()
        {
            InitializeComponent();
            ChangeLanguage();
            VersionType = VersionType.UNKNOWN;
            _version.Text = AssemblyInfo.Instance.AssemblyVersion;
           // 
        }

        public void Instance_Shown(object sender, EventArgs e)
        {
            lock(_notifys)
            {
                foreach (DelegateCall call in _notifys)
                {
                    Invoke(call);   
                }   
            }
        }
        #endregion
       

        #region Check Method
        private void _checkButton_Click(object sender, EventArgs e)
        {
            switch(FState)
            {
                case MainFormState.NONE:
                    TaskManager.Instance.AddTask(_listLoaderTask);
                    break;
                case MainFormState.CHECKING:
                    TaskManager.Instance.Close(false);
                    break;
            }
        }

        private void _updateBtn_Click(object sender, EventArgs e)
        {
            switch (FState)
            {
               case MainFormState.NONE:
                    TaskManager.Instance.AddTask(new GUAnalyzerTask());
                   break;
                case MainFormState.CHECKING:

                    break;
                case MainFormState.DONE:
                   
                    foreach (ListFile f in _listLoaderTask.Items)
                    {
                        var fileName = Directory.GetCurrentDirectory() + f.FileName;
                        var oldFileName = Directory.GetCurrentDirectory() + f.FileName + ".old";
                        var newFileName = Directory.GetCurrentDirectory() + f.FileName + ".new";
                        try
                        {

                            if (File.Exists(newFileName))
                            {
                                if (File.Exists(fileName))
                                {
                                    if (File.Exists(oldFileName))
                                    {
                                        File.Delete(oldFileName);
                                    }

                                    File.Move(fileName, fileName + ".old");
                                }

                                File.Move(newFileName, fileName);
                            }
                        }
                        catch(Exception e1)
                        {
                            _log.Info("Exception: "+ e1.Message, e1);   
                        }
                    }
                    
                    Application.Restart();;
                    break;
            }
        }
        #endregion

        #region Update Current Version
        private void SetCurrentVersionUnsafe(String v)
        {
            calcType(v);
            _currentVersion.Text = v;
        }

        public void SetCurrentVersion(String a)
        {
            var d = new DelegateCall
                        {
                            DELEGATE = new MainForm.UpdateStatusLabelDelegate(SetCurrentVersionUnsafe),
                            OBJECTS = new object[] {a}
                        };

            Invoke(d);
        }

        #endregion

        #region Update Status Label
        
        public void UpdateStatusLabelUnsafe(String a)
        {
            _statusLabel.Text = a;
        }
        
        public void UpdateStatusLabel(WordEnum a)
        {
            UpdateStatusLabel(LanguageHolder.Instance[a]);
        }

        public void UpdateStatusLabel(String a)
        {
            var delegateCall = new DelegateCall
                                            {
                                                DELEGATE =
                                                    new MainForm.UpdateStatusLabelDelegate(UpdateStatusLabelUnsafe),
                                                OBJECTS = new object[] {a}
                                            };

            Invoke(delegateCall);    
        }
        #endregion

        #region State
        public MainFormState FState { get; set; }

        public void SetState(MainFormState type)
        {
            var d = new DelegateCall
            {
                DELEGATE = new MainForm.SetFormStateDelegate(SetStateUnsafe),
                OBJECTS = new object[] { type}
            };

            Invoke(d);
        }

        private void SetStateUnsafe(MainFormState s)
        {
            switch (s)
            {
                case MainFormState.NONE:
                    _checkButton.Text = LanguageHolder.Instance[WordEnum.CHECK];  
                    //_updateBtn.Enabled = true;
                    _updateBtn.Text = LanguageHolder.Instance[WordEnum.UPDATE];  
                    break;
                case MainFormState.CHECKING:
                    _checkButton.Text = LanguageHolder.Instance[WordEnum.CANCEL];  
                    _updateBtn.Enabled = false;
                    break;
                case MainFormState.DONE:
                    _updateBtn.Enabled = true;
                    _updateBtn.Text = LanguageHolder.Instance[WordEnum.RESTART];  
                    break;
            }
            FState = s;
        }
        #endregion

        #region Version Type

        private VersionType _versionType;

        public VersionType VersionType
        {
            get
            {
                return _versionType;
            }
            set
            {
                _versionType = value;

                //_versionTypeLabel.Text = _versionType.ToString();
                _versionTypeLabel.Text = LanguageHolder.Instance[(WordEnum)Enum.Parse(typeof(WordEnum), string.Format("{0}_VERSION", _versionType))];

                switch (value)
                {
                    case VersionType.BIGGER:
                    case VersionType.SAME:
                        _updateBtn.Enabled = true;
                        break;
                }
            }
        }

        private void calcType(String current)
        {
            String[] a = AssemblyInfo.Instance.AssemblyVersion.Split('.');
            String[] cv = current.Split('.');
            if (a.Length != cv.Length)
            {
                VersionType = VersionType.UNKNOWN;
                return;
            }

            int[] thisV = new int[a.Length];
            int[] curV = new int[a.Length];
            try
            {

                for (int i = 0; i < a.Length; i++)
                {
                    thisV[i] = int.Parse(a[i].Trim());
                    curV[i] = int.Parse(cv[i].Trim());
                }
            }
            catch
            {
                VersionType = VersionType.UNKNOWN;
                return;
            }

            bool isSame = true;

           for (int i = 0; i < a.Length; i++)
           {
               if (thisV[i] != curV[i])
                   isSame = false;
           }

            if(isSame)
            {
                VersionType = VersionType.SAME;
                return;    
            }

            bool isBigger = true;
            for (int i = (a.Length - 1); i != 0; i--)
            {
                if (thisV[i] > curV[i])
                {
                    isBigger = false;
                }
            }

            if (isBigger)
            {
                VersionType = VersionType.BIGGER;
                return;
            }

            VersionType = VersionType.LOWER;
        }

        #endregion

        #region Progress Bar's

        public void UpdateProgressBar(int persent, bool total)
        {
            var d = new DelegateCall
            {
                DELEGATE = new MainForm.UpdateProgressBarDelegate(updateProgressBarUnsafe),
                OBJECTS = new object[] { persent, total }
            };

            Invoke(d);
        }

        private void updateProgressBarUnsafe(int pe, bool total)
        {
            if (total)
            {

                if (pe == 0)
                {
                    Windows7Taskbar.SetProgressState(Handle, ThumbnailProgressState.NoProgress);
                }
                else
                {
                    Windows7Taskbar.SetProgressValue(Handle, pe, 100);
                }

                _totalProgress.Value = pe;
                _totalProgress.Refresh();
            }
            else
            {
                _fileProgressBar.Value = pe;
                _fileProgressBar.Refresh();
            }
        }

        #endregion

        public bool IsCanInvoke
        {
            get
            {
                return MainForm.Instance.IsCanInvoke && !IsDisposed && !Disposing && Visible;
            }
        }

        private void Invoke(DelegateCall a)
        {
            lock (_notifys)
            {
                if (IsCanInvoke)
                {
                    Invoke(a.DELEGATE, a.OBJECTS);
                }
                else
                {
                    _notifys.AddLast(a);
                }
            }
        }

        public GUListLoaderTask ListLoader
        {
            get
            {
                return _listLoaderTask;
            }
        }
    }
}