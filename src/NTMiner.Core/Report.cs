﻿using NTMiner.Core;
using NTMiner.Core.Gpus;
using NTMiner.Core.Kernels;
using NTMiner.Profile;
using System;
using System.Collections.Generic;

namespace NTMiner {
    public static class Report {
        private class CoinShareData {
            public int TotalShareCount { get; set; }
        }

        public static void Init(INTMinerRoot root) {
            if (Design.IsInDesignMode) {
                return;
            }

            VirtualRoot.Access<HasBoot2SecondEvent>(
                Guid.Parse("b4efba26-1f02-42f2-822a-dd38cffd466d"),
                "登录服务器并报告一次0算力",
                LogEnum.Console,
                action: message => {
                    // 报告0算力从而告知服务器该客户端当前在线的币种
                    ReportSpeed(root);
                });

            VirtualRoot.Access<Per2MinuteEvent>(
                Guid.Parse("0b16bbd3-329b-4b46-9ebe-c403cae26018"),
                "每两分钟上报一次",
                LogEnum.Console,
                action: message => {
                    ReportSpeed(root);
                });

            VirtualRoot.Access<MineStartedEvent>(
                Guid.Parse("6e8ab8a2-7d08-41bd-9410-107793639f7f"),
                "开始挖矿后报告状态",
                LogEnum.Console,
                action: message => {
                    ReportSpeed(root);
                });

            VirtualRoot.Access<MineStopedEvent>(
                Guid.Parse("5ff27468-2a01-4ce4-91a3-aa354791d0eb"),
                "停止挖矿后报告状态",
                LogEnum.Console,
                action: message => {
                    ReportState(root);
                });
        }

        private static readonly Dictionary<Guid, CoinShareData> _coinShareDic = new Dictionary<Guid, CoinShareData>();
        private static ICoin _lastSpeedMainCoin;
        private static ICoin _lastSpeedDualCoin;
        public static SpeedData CreateSpeedData(INTMinerRoot root) {
            SpeedData data = new SpeedData {
                MessageId = Guid.NewGuid(),
                WorkId = CommandLineArgs.WorkId,
                Version = NTMinerRoot.CurrentVersion.ToString(4),
                MinerName = root.MinerProfile.MinerName,
                GpuInfo = root.GpuSetInfo,
                ClientId = ClientId.Id,
                Timestamp = DateTime.Now,
                MainCoinCode = string.Empty,
                MainCoinShareDelta = 0,
                MainCoinSpeed = 0,
                DualCoinCode = string.Empty,
                DualCoinShareDelta = 0,
                DualCoinSpeed = 0,
                IsMining = root.IsMining,
                DualCoinPool = string.Empty,
                DualCoinWallet = string.Empty,
                IsDualCoinEnabled = false,
                Kernel = string.Empty,
                MainCoinPool = string.Empty,
                MainCoinWallet = string.Empty,
            };
            #region 当前选中的币种是什么
            ICoin mainCoin;
            if (root.CoinSet.TryGetCoin(root.MinerProfile.CoinId, out mainCoin)) {
                data.MainCoinCode = mainCoin.Code;
                ICoinProfile coinProfile = root.CoinProfileSet.GetCoinProfile(mainCoin.GetId());
                IPool mainCoinPool;
                if (root.PoolSet.TryGetPool(coinProfile.PoolId, out mainCoinPool)) {
                    data.MainCoinPool = mainCoinPool.Server;
                }
                else {
                    data.MainCoinPool = string.Empty;
                }
                data.MainCoinWallet = coinProfile.Wallet;
                ICoinKernel coinKernel;
                if (root.CoinKernelSet.TryGetCoinKernel(coinProfile.CoinKernelId, out coinKernel)) {
                    IKernel kernel;
                    if (root.KernelSet.TryGetKernel(coinKernel.KernelId, out kernel)) {
                        data.Kernel = kernel.FullName;
                        ICoinKernelProfile coinKernelProfile = root.CoinKernelProfileSet.GetCoinKernelProfile(coinProfile.CoinKernelId);
                        data.IsDualCoinEnabled = coinKernelProfile.IsDualCoinEnabled;
                        if (coinKernelProfile.IsDualCoinEnabled) {
                            ICoin dualCoin;
                            if (root.CoinSet.TryGetCoin(coinKernelProfile.DualCoinId, out dualCoin)) {
                                data.DualCoinCode = dualCoin.Code;
                                ICoinProfile dualCoinProfile = root.CoinProfileSet.GetCoinProfile(dualCoin.GetId());
                                data.DualCoinWallet = dualCoinProfile.DualCoinWallet;
                                IPool dualCoinPool;
                                if (root.PoolSet.TryGetPool(dualCoinProfile.DualCoinPoolId, out dualCoinPool)) {
                                    data.DualCoinPool = dualCoinPool.Server;
                                }
                                else {
                                    data.DualCoinPool = string.Empty;
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            if (root.IsMining) {
                // 判断上次报告的算力币种和本次报告的是否相同，否则说明刚刚切换了币种默认第一次报告0算力
                if (_lastSpeedMainCoin == null || _lastSpeedMainCoin == root.CurrentMineContext.MainCoin) {
                    _lastSpeedMainCoin = root.CurrentMineContext.MainCoin;
                    CoinShareData preCoinShare;
                    Guid coinId = root.CurrentMineContext.MainCoin.GetId();
                    IGpusSpeed gpuSpeeds = NTMinerRoot.Current.GpusSpeed;
                    IGpuSpeed totalSpeed = gpuSpeeds.CurrentSpeed(NTMinerRoot.GpuAllId);
                    data.MainCoinSpeed = (int)totalSpeed.MainCoinSpeed.Value;
                    ICoinShare share = root.CoinShareSet.GetOrCreate(coinId);
                    if (!_coinShareDic.TryGetValue(coinId, out preCoinShare)) {
                        preCoinShare = new CoinShareData() {
                            TotalShareCount = share.TotalShareCount
                        };
                        _coinShareDic.Add(coinId, preCoinShare);
                        data.MainCoinShareDelta = share.TotalShareCount;
                    }
                    else {
                        data.MainCoinShareDelta = share.TotalShareCount - preCoinShare.TotalShareCount;
                    }
                }
                else {
                    _lastSpeedMainCoin = root.CurrentMineContext.MainCoin;
                }
                if (root.CurrentMineContext is IDualMineContext dualMineContext) {
                    // 判断上次报告的算力币种和本次报告的是否相同，否则说明刚刚切换了币种默认第一次报告0算力
                    if (_lastSpeedDualCoin == null || _lastSpeedDualCoin == dualMineContext.DualCoin) {
                        _lastSpeedDualCoin = dualMineContext.DualCoin;
                        CoinShareData preCoinShare;
                        Guid coinId = dualMineContext.DualCoin.GetId();
                        IGpusSpeed gpuSpeeds = NTMinerRoot.Current.GpusSpeed;
                        IGpuSpeed totalSpeed = gpuSpeeds.CurrentSpeed(NTMinerRoot.GpuAllId);
                        data.DualCoinSpeed = (int)totalSpeed.DualCoinSpeed.Value;
                        ICoinShare share = root.CoinShareSet.GetOrCreate(coinId);
                        if (!_coinShareDic.TryGetValue(coinId, out preCoinShare)) {
                            preCoinShare = new CoinShareData() {
                                TotalShareCount = share.TotalShareCount
                            };
                            _coinShareDic.Add(coinId, preCoinShare);
                            data.DualCoinShareDelta = share.TotalShareCount;
                        }
                        else {
                            data.DualCoinShareDelta = share.TotalShareCount - preCoinShare.TotalShareCount;
                        }
                    }
                    else {
                        _lastSpeedDualCoin = dualMineContext.DualCoin;
                    }
                }
            }
            return data;
        }

        private static void ReportSpeed(INTMinerRoot root) {
            try {
                SpeedData data = CreateSpeedData(root);
                Server.ReportService.ReportSpeedAsync(data);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
            }
        }

        private static void ReportState(INTMinerRoot root) {
            try {
                Server.ReportService.ReportStateAsync(ClientId.Id, root.IsMining);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
            }
        }
    }
}
