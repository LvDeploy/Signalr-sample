using Microsoft.AspNetCore.SignalR;
using SignalRCore.Hubs;
using SignalRCore.Models;
using SignalRCore.Service.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalRCore.Service
{
    public class SignalRManager<T> where T : Hub
    {
        private static readonly SignalRManager<T> instance = new SignalRManager<T>();

        private ConcurrentDictionary<int, List<PendingAction>> pendingActions;

        private readonly IHubContext<T> _hubContext;

        public SignalRManager(IHubContext<T> hubContext)
        {
            _hubContext = hubContext;
        }

        private SignalRManager()
        {
            this.pendingActions = new ConcurrentDictionary<int, List<PendingAction>>();
        }

        public static SignalRManager<T> Instance
        {
            get
            {
                return instance;
            }
        }

        private IHubContext<T> GetMainHub()
        {
            return _hubContext;
        }

        public bool ForcarDeslogar(ChatMessage model, int usuarioId)
        {
            var hubContext = GetMainHub();

            var conexoes = HubMapping.Instance.GetConnections(usuarioId);

            foreach (var item in conexoes)
            {
               // hubContext.Clients.Client(item).ForcarDeslogar(model);
            }


            HubMapping.Instance.RemoveAll(usuarioId);


            return conexoes != null && conexoes.Any();
        }

        #region Chat

        public void ChatNovaMensagem(ChatMessage model)
        {
            var hubContext = GetMainHub();

            if (HubMapping.Instance.IsConnected(model.IdUsuario))
            {
                var conexoes = HubMapping.Instance.GetConnections(model.IdUsuario);

                foreach (var item in conexoes)
                {
                  //  hubContext.Clients.Client(item).novaMensagem(model);
                }
            }
            else
            {
                //Guardar na lista de chamadas pendentes
                var action = new PendingAction()
                {
                    MethodName = "novaMensagem",
                    HubName = "SignalRHub",
                    Data = model
                };

                AdicionarChamadaPendente(model.IdUsuario, action);
            }
        }

        public void ChatAlteracaoStatusMensagem(ChatMessage model, int usuarioId)
        {
            var hubContext = GetMainHub();

            if (HubMapping.Instance.IsConnected(usuarioId))
            {
                var conexoes = HubMapping.Instance.GetConnections(usuarioId);

                foreach (var item in conexoes)
                {
                    //hubContext.Clients.Client(item).alteracaoStatus(model);
                }

            }
        }


        #endregion

        #region Discador

        public bool DiscadorAoAtender(ChatMessage model, int usuarioId)
        {
            var hubContext = GetMainHub();

            var conexoes = HubMapping.Instance.GetConnections(usuarioId);

            foreach (var item in conexoes)
            {
               // hubContext.Clients.Client(item).aoAtender(model);
            }

            return conexoes != null && conexoes.Any();
        }

        public bool DiscadorLigacaoAtendida(int usuarioId)
        {
            var hubContext = GetMainHub();

            var conexoes = HubMapping.Instance.GetConnections(usuarioId);

            foreach (var item in conexoes)
            {
                //hubContext.Clients.Client(item).ligacaoAd();
            }


            return conexoes != null && conexoes.Any();
        }

        public bool TelefoniaAoAtenderReceptivo(ChatMessage model, int usuarioId)
        {
            var hubContext = GetMainHub();

            var conexoes = HubMapping.Instance.GetConnections(usuarioId);

            foreach (var item in conexoes)
            {
              //  hubContext.Clients.Client(item).test2(model);
            }

            return conexoes != null && conexoes.Any();
        }

        public bool DiscadorResultadoClick2Call(int usuarioId, bool sucesso, string erro, string cid)
        {
            var hubContext = GetMainHub();

            var conexoes = HubMapping.Instance.GetConnections(usuarioId);

            if (conexoes != null && conexoes.Any())
            {
                var model = new
                {
                    sucesso = sucesso,
                    erro = erro,
                    cid = cid
                };

                foreach (var item in conexoes)
                {
                   // hubContext.Clients.Client(item).reddall(model);
                }
            }

            return conexoes != null && conexoes.Any();
        }

        #endregion

        #region Notifications

        public bool NotificacaoPendente(object model, int usuarioId)
        {
            var enviado = false;
            var hubContext = GetMainHub();

            if (HubMapping.Instance.IsConnected(usuarioId))
            {
                var conexoes = HubMapping.Instance.GetConnections(usuarioId);

                foreach (var item in conexoes)
                {
                   // hubContext.Clients.Client(item).notificacao(model);
                }

                enviado = true;
            }
            else
            {
                var action = new PendingAction()
                {
                    MethodName = "notificacaoPendente",
                    HubName = "SignalRHub",
                    Data = model
                };

                AdicionarChamadaPendente(usuarioId, action);
            }

            return enviado;
        }

        #endregion

        public void AsyncTaskCompleted(int usuarioId, int tipoTask, string id, string data)
        {
            var hubContext = GetMainHub();

            var model = new
            {
                TipoTask = tipoTask,
                Id = id,
                Dados = data
            };

            if (HubMapping.Instance.IsConnected(usuarioId))
            {
                var conexoes = HubMapping.Instance.GetConnections(usuarioId);

                foreach (var item in conexoes)
                {
                    //hubContext.Clients.Client(item).asyncTaskCompleted(model);
                }

            }
            else
            {
                var action = new PendingAction()
                {
                    MethodName = "asyncTaskCompleted",
                    HubName = "SignalRHub",
                    Data = model
                };

                AdicionarChamadaPendente(usuarioId, action);
            }
        }


        public void ExecutarChamadasPendentes(int usuarioId, string connectionId)
        {
            List<PendingAction> actionsUsuario = null;
            pendingActions.TryRemove(usuarioId, out actionsUsuario);

            if (actionsUsuario != null && actionsUsuario.Count > 0)
            {
                foreach (var action in actionsUsuario)
                {
                    if (action.Timestamp >= DateTime.UtcNow.AddMinutes(-1))
                    {
                        CallPendingMethod(action, new[] { connectionId });
                    }
                }
            }
        }

        private void AdicionarChamadaPendente(int usuarioId, PendingAction action)
        {
            lock (pendingActions)
            {

                List<PendingAction> actionsUsuario = null;

                if (!pendingActions.TryRemove(usuarioId, out actionsUsuario))
                {

                    actionsUsuario = new List<PendingAction>();
                }

                action.Timestamp = DateTime.UtcNow;
                actionsUsuario.Add(action);

                pendingActions.TryAdd(usuarioId, actionsUsuario.Where(a => a.Timestamp >= DateTime.UtcNow.AddMinutes(-1)).ToList());
            }
        }

        private void CallPendingMethod(PendingAction action, string[] conexoes)
        {
            var hubContext = _hubContext;

            var proxy = hubContext.Clients.Clients(conexoes);

            proxy.SendAsync(action.MethodName, action.Data);
        }

        class PendingAction
        {
            public string HubName { get; set; }
            public string MethodName { get; set; }
            public Object Data { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
