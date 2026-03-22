using SSync.Transition;
using Symbioz.World.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Providers.Network
{
    /// <summary>
    /// Client de transition spécialisé pour la connexion au serveur d'authentification (AuthServer).
    /// Hérite de TransitionClient (couche SSync) et surcharge les callbacks de connexion
    /// pour déléguer la logique au TransitionServerManager.
    ///
    /// Cycle de vie typique :
    ///   1. OnConnected()      → connexion réussie, on s'enregistre auprès de l'Auth
    ///   2. OnClosed()         → connexion perdue, on arrête le serveur proprement
    ///   3. OnFailToConnect()  → impossible de joindre l'Auth, on retente dans 3s
    /// </summary>
    public class AuthTransitionClient : TransitionClient
    {
        /// <summary>
        /// Constructeur par défaut : utilisé pour créer une connexion sortante vers l'AuthServer.
        /// </summary>
        public AuthTransitionClient()
        {

        }

        /// <summary>
        /// Constructeur avec socket existant : utilisé si la connexion est déjà établie (cas serveur entrant).
        /// </summary>
        public AuthTransitionClient(Socket socket)
            : base(socket)
        {

        }

        /// <summary>
        /// Appelé quand la connexion TCP vers l'AuthServer est fermée.
        /// Si on était bien connecté (IsConnected=true), déclenche la procédure d'arrêt d'urgence
        /// pour sauvegarder les données et déconnecter les joueurs.
        /// </summary>
        public override void OnClosed()
        {
            // On ne déclenche la procédure d'urgence que si on était réellement connecté
            // (évite un double déclenchement si la connexion n'avait jamais abouti)
            if (TransitionServerManager.Instance.IsConnected)
            {
                TransitionServerManager.Instance.OnConnectionToAuthLost();
                base.OnClosed();
            }
        }

        /// <summary>
        /// Appelé si la tentative de connexion TCP vers l'AuthServer échoue.
        /// Délègue au TransitionServerManager qui attendra 3s puis retentera.
        /// </summary>
        public override void OnFailToConnect(Exception ex)
        {
            TransitionServerManager.Instance.OnFailedToConnectAuth();
            base.OnFailToConnect(ex);
        }

        /// <summary>
        /// Appelé quand la connexion TCP vers l'AuthServer est établie avec succès.
        /// Délègue au TransitionServerManager qui enverra les informations d'enregistrement du serveur.
        /// </summary>
        public override void OnConnected()
        {
            TransitionServerManager.Instance.OnConnectedToAuth();
            base.OnConnected();
        }

    }
}
