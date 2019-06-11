using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalRCore.Service.Util
{
    public class HubMapping
    {
         private static readonly HubMapping instance = new HubMapping();

        private readonly Dictionary<int, HashSet<string>> _connections =
            new Dictionary<int, HashSet<string>>();

        public static HubMapping Instance
        {
            get
            {
                return instance;
            }
        }

        public int Count
        {
            get
            {
                return _connections.Count;
            }
        }

        public void Add(int key, string connectionId)
        {
            lock (_connections)
            {
                HashSet<string> connections;
                if (!_connections.TryGetValue(key, out connections))
                {
                    connections = new HashSet<string>();
                    _connections.Add(key, connections);
                }

                lock (connections)
                {
                    connections.Add(connectionId);
                }
            }
        }

        public IEnumerable<string> GetConnections(int key)
        {
            HashSet<string> connections;
            if (_connections.TryGetValue(key, out connections))
            {
                return connections;
            }

            return Enumerable.Empty<string>();
        }

        public bool IsConnected(int key)
        {
            return this.GetConnections(key).Any();
        }

        public int[] GetConnectedUsers()
        {
            return this._connections.Select(t=> t.Key).ToArray();
        }

        public int GetConnectionNumber()
        {
            return this._connections.Values.Select(t => t.Count).Sum();
        }

        public void Remove(int key, string connectionId)
        {
            lock (_connections)
            {
                HashSet<string> connections;
                if (!_connections.TryGetValue(key, out connections))
                {
                    return;
                }

                lock (connections)
                {
                    connections.Remove(connectionId);

                    if (connections.Count == 0)
                    {
                        _connections.Remove(key);
                    }
                }
            }
        }

        public void RemoveAll(int key)
        {
            lock (_connections)
            {
                if (!_connections.ContainsKey(key))
                {
                    return;
                }

                _connections.Remove(key);
            }
        }
    }
}
