﻿using NTMiner.Core.Profiles;
using NTMiner.Core.Profiles.Impl;
using NTMiner.Profile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NTMiner.Core.Impl {
    public class LocalJson {
        public static readonly LocalJson Instance = new LocalJson();

        public static string Export(IWorkProfile workProfile) {
            INTMinerRoot root = NTMinerRoot.Current;
            LocalJson data = new LocalJson() {
                CoinKernelProfiles = workProfile.GetCoinKernelProfiles().Cast<CoinKernelProfileData>().ToArray(),
                CoinProfiles = workProfile.GetCoinProfiles().Cast<CoinProfileData>().ToArray(),
                GpuOverClocks = workProfile.GetGpuOverClocks().Cast<GpuProfileData>().ToArray(),
                MinerProfile = new MinerProfileData(workProfile),
                Pools = workProfile.GetPools().Cast<PoolData>().ToArray(),
                PoolProfiles = workProfile.GetPoolProfiles().Cast<PoolProfileData>().ToArray(),
                Users = workProfile.GetUsers().Cast<UserData>().ToArray(),
                Wallets = workProfile.GetWallets().Cast<WalletData>().ToArray()
            };
            string json = VirtualRoot.JsonSerializer.Serialize(data);
            string workJsonFileFullName = Path.Combine(VirtualRoot.GlobalDirFullName, "MineWorks", workProfile.GetId() + ".json");
            File.WriteAllText(workJsonFileFullName, json);
            return Path.GetFileName(workJsonFileFullName);
        }

        // 私有构造函数不影响序列化反序列化
        private LocalJson() {
            this.CoinKernelProfiles = new CoinKernelProfileData[0];
            this.CoinProfiles = new CoinProfileData[0];
            this.GpuOverClocks = new GpuProfileData[0];
            this.MinerProfile = new MinerProfileData();
            this.Pools = new PoolData[0];
            this.PoolProfiles = new PoolProfileData[0];
            this.Users = new UserData[0];
            this.Wallets = new WalletData[0];
            this.TimeStamp = Timestamp.GetTimestamp();
        }

        private readonly object _locker = new object();
        private bool _inited = false;
        public void Init(string rawJson) {
            if (!_inited) {
                lock (_locker) {
                    if (!_inited) {
                        if (!string.IsNullOrEmpty(rawJson)) {
                            try {
                                LocalJson data = VirtualRoot.JsonSerializer.Deserialize<LocalJson>(rawJson);
                                this.CoinKernelProfiles = data.CoinKernelProfiles ?? new CoinKernelProfileData[0];
                                this.CoinProfiles = data.CoinProfiles ?? new CoinProfileData[0];
                                this.GpuOverClocks = data.GpuOverClocks ?? new GpuProfileData[0];
                                this.MinerProfile = data.MinerProfile ?? new MinerProfileData();
                                this.Pools = data.Pools ?? new PoolData[0];
                                this.PoolProfiles = data.PoolProfiles ?? new PoolProfileData[0];
                                this.Users = data.Users ?? new UserData[0];
                                this.Wallets = data.Wallets ?? new WalletData[0];
                                this.TimeStamp = data.TimeStamp;
                                File.WriteAllText(SpecialPath.LocalJsonFileFullName, rawJson);
                            }
                            catch (Exception e) {
                                Logger.ErrorDebugLine(e.Message, e);
                            }
                        }
                        _inited = true;
                    }
                }
            }
        }

        public void ReInit(string rawJson) {
            _inited = false;
            Init(rawJson);
        }

        public bool Exists<T>(Guid key) where T : IDbEntity<Guid> {
            return GetAll<T>().Any(a => a.GetId() == key);
        }

        public T GetByKey<T>(Guid key) where T : IDbEntity<Guid> {
            return GetAll<T>().FirstOrDefault(a => a.GetId() == key);
        }

        public IEnumerable<T> GetAll<T>() where T : IDbEntity<Guid> {
            string typeName = typeof(T).Name;
            switch (typeName) {
                case nameof(CoinKernelProfileData):
                    return this.CoinKernelProfiles.Cast<T>();
                case nameof(CoinProfileData):
                    return this.CoinProfiles.Cast<T>();
                case nameof(GpuProfileData):
                    return this.GpuOverClocks.Cast<T>();
                case nameof(PoolData):
                    return this.Pools.Cast<T>();
                case nameof(PoolProfileData):
                    return this.PoolProfiles.Cast<T>();
                case nameof(UserData):
                    return this.Users.Cast<T>();
                case nameof(WalletData):
                    return this.Wallets.Cast<T>();
                default:
                    return new List<T>();
            }
        }

        public ulong TimeStamp { get; set; }

        public CoinKernelProfileData[] CoinKernelProfiles { get; set; }

        public CoinProfileData[] CoinProfiles { get; set; }

        public GpuProfileData[] GpuOverClocks { get; set; }

        public MinerProfileData MinerProfile { get; set; }

        public PoolData[] Pools { get; set; }

        public PoolProfileData[] PoolProfiles { get; set; }

        public UserData[] Users { get; set; }

        public WalletData[] Wallets { get; set; }
    }
}
