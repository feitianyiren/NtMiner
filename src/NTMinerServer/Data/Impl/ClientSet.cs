﻿using LiteDB;
using NTMiner.MinerServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NTMiner.Data.Impl {
    public class ClientSet : IClientSet {
        // 内存中保留20分钟内活跃的客户端
        private readonly Dictionary<Guid, ClientData> _dicById = new Dictionary<Guid, ClientData>();

        private readonly IHostRoot _root;
        internal ClientSet(IHostRoot root) {
            _root = root;
            VirtualRoot.On<Per20SecondEvent>(
                "周期性将内存中的ClientData列表刷入磁盘",
                LogEnum.Console,
                action: message => {
                    InitOnece();
                    lock (_locker) {
                        DateTime time = message.Timestamp.AddMinutes(-20);
                        // 移除20分钟内未活跃的客户端缓存
                        List<Guid> toRemoves = _dicById.Where(a => a.Value.ModifiedOn < time).Select(a => a.Key).ToList();
                        foreach (var clientId in toRemoves) {
                            _dicById.Remove(clientId);
                        }
                        time = message.Timestamp.AddSeconds(-message.Seconds);
                        using (LiteDatabase db = HostRoot.CreateLocalDb()) {
                            var col = db.GetCollection<ClientData>();
                            // 更新一个周期内活跃的客户端
                            col.Upsert(_dicById.Values.Where(a => a.ModifiedOn > time));
                        }
                    }
                });
        }

        private bool _isInited = false;
        private object _locker = new object();

        private void InitOnece() {
            if (_isInited) {
                return;
            }
            Init();
        }

        private void Init() {
            lock (_locker) {
                if (!_isInited) {
                    using (LiteDatabase db = HostRoot.CreateLocalDb()) {
                        var col = db.GetCollection<ClientData>();
                        DateTime time = DateTime.Now.AddMinutes(-20);
                        foreach (var item in col.Find(Query.GT(nameof(ClientData.ModifiedOn), time))) {
                            _dicById.Add(item.Id, item);
                        }
                    }
                    _isInited = true;
                }
            }
        }

        public ClientCount Count() {
            InitOnece();
            // 因为客户端每120秒上报一次数据所以将140秒内活跃的客户端视为在线
            DateTime time = DateTime.Now.AddSeconds(-140);
            int onlineCount = 0;
            int miningCount = 0;
            lock (_locker) {
                foreach (var clientData in _dicById.Values) {
                    if (clientData.ModifiedOn > time) {
                        onlineCount++;
                        if (clientData.IsMining) {
                            miningCount++;
                        }
                    }
                }
            }
            return new ClientCount {
                OnlineCount = onlineCount,
                MiningCount = miningCount
            };
        }

        public ClientCoinCount Count(string coinCode) {
            InitOnece();
            DateTime time = DateTime.Now.AddSeconds(-140);
            int mainCoinOnlineCount = 0;
            int mainCoinMiningCount = 0;
            int dualCoinOnlineCount = 0;
            int dualCoinMiningCount = 0;
            lock (_locker) {
                foreach (var clientData in _dicById.Values) {
                    if (clientData.ModifiedOn > time) {
                        if (clientData.MainCoinCode == coinCode) {
                            mainCoinOnlineCount++;
                            if (clientData.IsMining) {
                                mainCoinMiningCount++;
                            }
                        }
                        if (clientData.DualCoinCode == coinCode) {
                            dualCoinOnlineCount++;
                            if (clientData.IsMining) {
                                dualCoinMiningCount++;
                            }
                        }
                    }
                }
            }
            return new ClientCoinCount {
                MainCoinOnlineCount = mainCoinOnlineCount,
                MainCoinMiningCount = mainCoinMiningCount,
                DualCoinOnlineCount = dualCoinOnlineCount,
                DualCoinMiningCount = dualCoinMiningCount
            };
        }

        public void Add(ClientData clientData) {
            InitOnece();
            if (!_dicById.ContainsKey(clientData.Id)) {
                lock (_locker) {
                    if (!_dicById.ContainsKey(clientData.Id)) {
                        _dicById.Add(clientData.Id, clientData);
                    }
                }
            }
        }

        public List<ClientData> QueryClients(
            int pageIndex,
            int pageSize,
            bool isPull,
            DateTime? timeLimit,
            Guid? groupId,
            Guid? workId,
            string minerIp,
            string minerName,
            MineStatus mineState,
            string mainCoin,
            string mainCoinPool,
            string mainCoinWallet,
            string dualCoin,
            string dualCoinPool,
            string dualCoinWallet,
            string version,
            string kernel,
            out int total) {
            InitOnece();
            lock (_locker) {
                IQueryable<ClientData> query;
                if (timeLimit.HasValue && timeLimit.Value.AddSeconds(20 * 60 + Timestamp.DesyncSeconds) > DateTime.Now) {
                    query = _dicById.Values.AsQueryable();
                }
                else {
                    using (LiteDatabase db = HostRoot.CreateLocalDb()) {
                        var col = db.GetCollection<ClientData>();
                        query = col.FindAll().AsQueryable();
                    }
                }
                if (timeLimit.HasValue) {
                    query = query.Where(a => a.ModifiedOn > timeLimit.Value);
                }
                if (groupId != null && groupId.Value != Guid.Empty) {
                    query = query.Where(a => a.GroupId == groupId.Value);
                }
                if (workId != null && workId.Value != Guid.Empty) {
                    query = query.Where(a => a.WorkId == workId.Value);
                }
                else {
                    if (workId != null) {
                        query = query.Where(a => a.WorkId == workId.Value);
                    }
                    if (!string.IsNullOrEmpty(mainCoin)) {
                        query = query.Where(a => a.MainCoinCode == mainCoin);
                    }
                    if (!string.IsNullOrEmpty(mainCoinPool)) {
                        query = query.Where(a => a.MainCoinPool == mainCoinPool);
                    }
                    if (!string.IsNullOrEmpty(dualCoin)) {
                        if (dualCoin == "*") {
                            query = query.Where(a => a.IsDualCoinEnabled);
                        }
                        else {
                            query = query.Where(a => a.DualCoinCode == dualCoin);
                        }
                    }
                    if (!string.IsNullOrEmpty(dualCoinPool)) {
                        query = query.Where(a => a.DualCoinPool == dualCoinPool);
                    }
                    if (!string.IsNullOrEmpty(mainCoinWallet)) {
                        query = query.Where(a => a.MainCoinWallet == mainCoinWallet);
                    }
                    if (!string.IsNullOrEmpty(dualCoinWallet)) {
                        query = query.Where(a => a.DualCoinWallet == dualCoinWallet);
                    }
                }
                if (!string.IsNullOrEmpty(minerIp)) {
                    query = query.Where(a => a.MinerIp == minerIp);
                }
                if (!string.IsNullOrEmpty(minerName)) {
                    query = query.Where(a => a.MinerName.IndexOf(minerName, StringComparison.OrdinalIgnoreCase) >= 0);
                }
                if (mineState != MineStatus.All) {
                    if (mineState == MineStatus.Mining) {
                        query = query.Where(a => a.IsMining == true);
                    }
                    else {
                        query = query.Where(a => a.IsMining == false);
                    }
                }
                if (!string.IsNullOrEmpty(version)) {
                    query = query.Where(a => a.Version != null && a.Version.StartsWith(version, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrEmpty(kernel)) {
                    query = query.Where(a => a.Kernel != null && a.Kernel.StartsWith(kernel, StringComparison.OrdinalIgnoreCase));
                }
                total = query.Count();
                var results = query.OrderBy(a => a.MinerName).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
                DateTime time = DateTime.Now.AddMinutes(-3);
                // 3分钟未上报算力视为0算力
                foreach (var clientData in results) {
                    if (clientData.ModifiedOn < time) {
                        clientData.DualCoinSpeed = 0;
                        clientData.MainCoinSpeed = 0;
                    }
                }
                if (isPull) {
                    Task[] pullTasks = results.Select(a => CreatePullTask(a)).ToArray();
                    Task.WaitAll(pullTasks, 3 * 1000);
                }
                return results;
            }
        }

        public List<ClientData> LoadClients(IEnumerable<Guid> clientIds, bool isPull) {
            InitOnece();
            List<ClientData> results = new List<ClientData>();
            DateTime time = DateTime.Now.AddMinutes(-3);
            foreach (var clientId in clientIds) {
                ClientData clientData = LoadClient(clientId);
                if (clientData != null) {
                    results.Add(clientData);
                }
            }
            if (isPull) {
                Task[] pullTasks = results.Select(a => CreatePullTask(a)).ToArray();
                Task.WaitAll(pullTasks, 3 * 1000);
            }
            return results;
        }

        public ClientData LoadClient(Guid clientId) {
            InitOnece();
            ClientData clientData = null;
            lock (_locker) {
                _dicById.TryGetValue(clientId, out clientData);
            }
            if (clientData == null) {
                using (LiteDatabase db = HostRoot.CreateLocalDb()) {
                    var col = db.GetCollection<ClientData>();
                    clientData = col.FindById(clientId);
                    if (clientData != null) {
                        Add(clientData);
                    }
                }
            }
            DateTime time = DateTime.Now.AddMinutes(-3);
            // 3分钟未上报算力视为0算力
            if (clientData != null && clientData.ModifiedOn < time) {
                clientData.DualCoinSpeed = 0;
                clientData.MainCoinSpeed = 0;
            }
            return clientData;
        }

        public void UpdateClient(Guid clientId, string propertyName, object value) {
            InitOnece();
            ClientData clientData = LoadClient(clientId);
            if (clientData != null) {
                PropertyInfo propertyInfo = typeof(ClientData).GetProperty(propertyName);
                if (propertyInfo != null) {
                    if (propertyInfo.PropertyType == typeof(Guid)) {
                        value = DictionaryExtensions.ConvertToGuid(value);
                    }
                    propertyInfo.SetValue(clientData, value, null);
                    clientData.ModifiedOn = DateTime.Now;
                }
            }
        }

        public void UpdateClientProperties(Guid clientId, Dictionary<string, object> values) {
            InitOnece();
            ClientData clientData = LoadClient(clientId);
            if (clientData != null) {
                foreach (var kv in values) {
                    object value = kv.Value;
                    PropertyInfo propertyInfo = typeof(ClientData).GetProperty(kv.Key);
                    if (propertyInfo != null) {
                        if (propertyInfo.PropertyType == typeof(Guid)) {
                            value = DictionaryExtensions.ConvertToGuid(value);
                        }
                        propertyInfo.SetValue(clientData, value, null);
                    }
                }
                clientData.ModifiedOn = DateTime.Now;
            }
        }

        public static Task CreatePullTask(ClientData clientData) {
            return Task.Factory.StartNew(() => {
                Client.MinerClientService.GetSpeed(clientData.MinerIp, (speedData, exception) => {
                    if (exception != null) {
                        // TODO:根据错误类型更新矿工状态
                    }
                    else {
                        clientData.Update(speedData);
                    }
                });
            });
        }
    }
}
