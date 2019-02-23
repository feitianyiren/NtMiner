﻿using System.IO;

namespace NTMiner {
    public static class SpecialPath {
        static SpecialPath() {
            string daemonDirFullName = Path.Combine(VirtualRoot.GlobalDirFullName, "Daemon");
            if (!Directory.Exists(daemonDirFullName)) {
                Directory.CreateDirectory(daemonDirFullName);
            }
            DaemonFileFullName = Path.Combine(daemonDirFullName, "NTMinerDaemon.exe");
            DevConsoleFileFullName = Path.Combine(daemonDirFullName, "DevConsole.exe");

            TempDirFullName = Path.Combine(VirtualRoot.GlobalDirFullName, "Temp");
            if (!Directory.Exists(TempDirFullName)) {
                Directory.CreateDirectory(TempDirFullName);
            }
            ServerDbFileName = "server.litedb";
            ServerDbFileFullName = Path.Combine(VirtualRoot.GlobalDirFullName, ServerDbFileName);
            ServerJsonFileName = "server.json";
            ServerJsonFileFullName = Path.Combine(VirtualRoot.GlobalDirFullName, ServerJsonFileName);

            LocalDbFileFullName = Path.Combine(VirtualRoot.GlobalDirFullName, "local.litedb");
        }

        public static string LocalDbFileFullName { get; private set; }
        public static string ServerDbFileName { get; private set; }
        public static string ServerDbFileFullName { get; private set; }

        public static string ServerJsonFileName { get; private set; }
        public static string ServerJsonFileFullName { get; private set; }

        public static string DaemonFileFullName { get; private set; }

        public static string DevConsoleFileFullName { get; private set; }

        public static string TempDirFullName { get; private set; }

        private static bool _isFirstCallPackageDirFullName = true;
        public static string PackagesDirFullName {
            get {
                string dirFullName = Path.Combine(VirtualRoot.GlobalDirFullName, "Packages");
                if (_isFirstCallPackageDirFullName) {
                    if (!Directory.Exists(dirFullName)) {
                        Directory.CreateDirectory(dirFullName);
                    }
                    _isFirstCallPackageDirFullName = false;
                }

                return dirFullName;
            }
        }

        private static bool _isFirstCallDownloadDirFullName = true;
        public static string DownloadDirFullName {
            get {
                string dirFullName = Path.Combine(TempDirFullName, "Download");
                if (_isFirstCallDownloadDirFullName) {
                    if (!Directory.Exists(dirFullName)) {
                        Directory.CreateDirectory(dirFullName);
                    }
                    _isFirstCallDownloadDirFullName = false;
                }

                return dirFullName;
            }
        }

        private static bool _isFirstCallKernelsDirFullName = true;
        public static string KernelsDirFullName {
            get {
                string dirFullName = Path.Combine(TempDirFullName, "Kernels");
                if (_isFirstCallKernelsDirFullName) {
                    if (!Directory.Exists(dirFullName)) {
                        Directory.CreateDirectory(dirFullName);
                    }
                    _isFirstCallKernelsDirFullName = false;
                }

                return dirFullName;
            }
        }

        private static bool _isFirstCallLogsDirFullName = true;
        public static string LogsDirFullName {
            get {
                string dirFullName = Path.Combine(TempDirFullName, "logs");
                if (_isFirstCallLogsDirFullName) {
                    if (!Directory.Exists(dirFullName)) {
                        Directory.CreateDirectory(dirFullName);
                    }
                    _isFirstCallLogsDirFullName = false;
                }

                return dirFullName;
            }
        }
        private static bool _isFirstCallPicturesDirFullName = true;
        public static string PicturesDirFullName {
            get {
                string dirFullName = Path.Combine(VirtualRoot.GlobalDirFullName, "Pictures");
                if (_isFirstCallPicturesDirFullName) {
                    if (!Directory.Exists(dirFullName)) {
                        Directory.CreateDirectory(dirFullName);
                    }
                    _isFirstCallPicturesDirFullName = false;
                }

                return dirFullName;
            }
        }

        private static bool _isFirstCallScreenshotsDirFullName = true;
        public static string ScreenshotsDirFullName {
            get {
                string dirFullName = Path.Combine(PicturesDirFullName, "Screenshots");
                if (_isFirstCallScreenshotsDirFullName) {
                    if (!Directory.Exists(dirFullName)) {
                        Directory.CreateDirectory(dirFullName);
                    }
                    _isFirstCallScreenshotsDirFullName = false;
                }

                return dirFullName;
            }
        }
    }
}
