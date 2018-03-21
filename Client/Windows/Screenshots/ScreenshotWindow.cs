﻿using LunaClient.Base;
using LunaClient.Localization;
using LunaClient.Systems.Screenshot;
using LunaCommon.Enums;
using System;
using System.Collections.Generic;
using UniLinq;
using UnityEngine;

namespace LunaClient.Windows.Screenshots
{
    public partial class ScreenshotsWindow : SystemWindow<ScreenshotsWindow, ScreenshotSystem>
    {
        #region Fields

        private const float UpdateIntervalMs = 1500;

        protected const float FoldersWindowHeight = 300;
        protected const float FoldersWindowWidth = 200;
        protected const float LibraryWindowHeight = 600;
        protected const float LibraryWindowWidth = 600;
        protected const float ImageWindowHeight = 762;
        protected const float ImageWindowWidth = 1024;

        private static Rect _libraryWindowRect;
        private static Rect _imageWindowRect;

        private static GUILayoutOption[] _foldersLayoutOptions;
        private static GUILayoutOption[] _libraryLayoutOptions;
        private static GUILayoutOption[] _imageLayoutOptions;

        private static Vector2 _foldersScrollPos;
        private static Vector2 _libraryScrollPos;
        private static Vector2 _imageScrollPos;

        private static string _selectedFolder;
        private static long _selectedImage;

        private static DateTime _lastGuiUpdateTime = DateTime.MinValue;
        private static readonly List<Screenshot> Miniatures = new List<Screenshot>();

        #endregion

        private static bool _display;
        public override bool Display
        {
            get => base.Display && _display&& MainSystem.NetworkState >= ClientState.Running && HighLogic.LoadedScene >= GameScenes.SPACECENTER;
            set
            {
                if (!value) Reset();

                if (value && !_display && System.MiniatureImages.Count == 0)
                    System.MessageSender.RequestFolders();
                base.Display = _display = value;
            }
        }

        public override void Update()
        {
            base.Update();
            if (!Display) return;

            if (DateTime.Now - _lastGuiUpdateTime > TimeSpan.FromMilliseconds(UpdateIntervalMs))
            {
                _lastGuiUpdateTime = DateTime.Now;
                
                Miniatures.Clear();
                if (!string.IsNullOrEmpty(_selectedFolder) && System.MiniatureImages.TryGetValue(_selectedFolder, out var miniatures))
                {
                    Miniatures.AddRange(miniatures.Values.OrderBy(v => v.DateTaken));
                }
            }
        }

        public override void OnGui()
        {
            base.OnGui();
            if (Display)
            {
                WindowRect = FixWindowPos(GUILayout.Window(6719 + MainSystem.WindowOffset,
                    WindowRect, DrawContent, LocalizationContainer.ScreenshotWindowText.Folders, WindowStyle, _foldersLayoutOptions));
                
                if (!string.IsNullOrEmpty(_selectedFolder) && System.MiniatureImages.ContainsKey(_selectedFolder))
                {
                    _libraryWindowRect = FixWindowPos(GUILayout.Window(6720 + MainSystem.WindowOffset, _libraryWindowRect, 
                        DrawLibraryContent, $"{_selectedFolder} {LocalizationContainer.ScreenshotWindowText.Screenshots}", WindowStyle, _libraryLayoutOptions));
                }

                if (_selectedImage > 0 && System.DownloadedImages.ContainsKey(_selectedFolder))
                {
                    _imageWindowRect = FixWindowPos(GUILayout.Window(6721 + MainSystem.WindowOffset, _imageWindowRect, 
                        DrawImageContent, $"{DateTime.FromBinary(_selectedImage).ToLongTimeString()}", WindowStyle, _imageLayoutOptions));
                }
            }

            CheckWindowLock();
        }

        public override void SetStyles()
        {
            WindowRect = new Rect(50, Screen.height / 2f - FoldersWindowHeight / 2f, FoldersWindowWidth, FoldersWindowHeight);
            _libraryWindowRect = new Rect(Screen.width / 2f - LibraryWindowWidth / 2f, Screen.height / 2f - LibraryWindowHeight / 2f, LibraryWindowWidth, LibraryWindowHeight);
            MoveRect = new Rect(0, 0, 10000, 20);

            _foldersLayoutOptions = new GUILayoutOption[4];
            _foldersLayoutOptions[0] = GUILayout.MinWidth(FoldersWindowWidth);
            _foldersLayoutOptions[1] = GUILayout.MaxWidth(FoldersWindowWidth);
            _foldersLayoutOptions[2] = GUILayout.MinHeight(FoldersWindowHeight);
            _foldersLayoutOptions[3] = GUILayout.MaxHeight(FoldersWindowHeight);

            _libraryLayoutOptions = new GUILayoutOption[4];
            _libraryLayoutOptions[0] = GUILayout.MinWidth(LibraryWindowWidth);
            _libraryLayoutOptions[1] = GUILayout.MaxWidth(LibraryWindowWidth);
            _libraryLayoutOptions[2] = GUILayout.MinHeight(LibraryWindowHeight);
            _libraryLayoutOptions[3] = GUILayout.MaxHeight(LibraryWindowHeight);

            _imageLayoutOptions = new GUILayoutOption[4];
            _imageLayoutOptions[0] = GUILayout.MinWidth(ImageWindowWidth);
            _imageLayoutOptions[1] = GUILayout.MaxWidth(ImageWindowWidth);
            _imageLayoutOptions[2] = GUILayout.MinHeight(ImageWindowHeight);
            _imageLayoutOptions[3] = GUILayout.MaxHeight(ImageWindowHeight);
        }

        public override void RemoveWindowLock()
        {
            if (IsWindowLocked)
            {
                IsWindowLocked = false;
                InputLockManager.RemoveControlLock("LMP_ScreenshotLock");
            }
        }

        public void CheckWindowLock()
        {
            if (Display)
            {
                if (MainSystem.NetworkState < ClientState.Running || HighLogic.LoadedSceneIsFlight)
                {
                    RemoveWindowLock();
                    return;
                }

                Vector2 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;

                var shouldLock = WindowRect.Contains(mousePos) || _libraryWindowRect.Contains(mousePos);
                if (shouldLock && !IsWindowLocked)
                {
                    InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "LMP_ScreenshotLock");
                    IsWindowLocked = true;
                }
                if (!shouldLock && IsWindowLocked)
                    RemoveWindowLock();
            }

            if (!Display && IsWindowLocked)
                RemoveWindowLock();
        }

        private void Reset()
        {
            _selectedFolder = null;
            _selectedImage = 0;
        }
    }
}