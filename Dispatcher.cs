using GServer.Messages;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GServer
{
    public class Dispatcher : MonoBehaviour
    {
        private static Dispatcher _instance;
        private static volatile bool _queued = false;
        private static List<HadlerAction> _backlog = new List<HadlerAction>();
        private static List<HadlerAction> _actions = new List<HadlerAction>();
        private static Dictionary<int, Coroutine> netViewCoroutines = new Dictionary<int, Coroutine>();

        public static bool IsInitialized { get => _instance != null; }


        private class HadlerAction
        {
            public ReceiveHandler action;
            public Message message;
            public Connection.Connection connection;

            public void Invoke()
            {
                action.Invoke(message, connection);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_instance == null)
            {
                _instance = new GameObject("Dispatcher").AddComponent<Dispatcher>();
                DontDestroyOnLoad(_instance.gameObject);
            }
        }
        private void Update()
        {
            if (_queued)
            {
                lock (_backlog)
                {
                    var tmp = _actions;
                    _actions = _backlog;
                    _backlog = tmp;
                    _queued = false;
                }

                foreach (var action in _actions)
                    action.Invoke();

                _actions.Clear();
            }
        }
        private IEnumerator SyncCoroutine(NetView netView)
        {
            var yieldInstruction = new WaitForSeconds(netView.GetSyncPeriod());
            while (true)
            {
                netView.SyncNow();
                yield return yieldInstruction;
            }
        }

        public static void RunOnMainThread(ReceiveHandler action, Message message, Connection.Connection connection)
        {
            lock (_backlog)
            {
                _backlog.Add(new HadlerAction() { action = action, connection = connection, message = message });
                _queued = true;
            }
        }
        internal static void StartSync(NetView netView)
        {
            var coroutine = _instance.StartCoroutine(_instance.SyncCoroutine(netView));
            netViewCoroutines.Add(netView.GetHashCode(), coroutine);
        }
        internal static void StopSync(NetView netView)
        {
            var hash = netView.GetHashCode();

            if (netViewCoroutines.ContainsKey(hash))
            {
                var coroutine = netViewCoroutines[hash];
                _instance.StopCoroutine(coroutine);
                netViewCoroutines.Remove(hash); 
            }
        }
    }
}