using Symbioz.Core.DesignPattern.StartupEngine;
using Symbioz.World.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.World.Handlers.RolePlay.Commands
{
    /// <summary>
    /// Gestionnaire central des commandes de chat en jeu (préfixe ".").
    ///
    /// Au démarrage, LoadChatCommands() parcourt toutes les classes de l'assembly par réflexion
    /// et enregistre les méthodes annotées [ChatCommand] dans le dictionnaire Commands.
    ///
    /// Quand un joueur tape ".item 12345", ChatHandler appelle Handle() qui :
    ///   1. Extrait le nom de la commande ("item") depuis le message
    ///   2. Vérifie que la commande existe
    ///   3. Vérifie que le joueur a le rôle requis
    ///   4. Appelle le handler via DynamicInvoke avec les arguments restants
    /// </summary>
    class CommandsHandler
    {
        // Préfixe obligatoire pour déclencher une commande depuis le chat
        public const string CommandsPrefix = ".";

        // Dictionnaire : clé = Command (nom + rôle requis), valeur = délégué Action<string, WorldClient>
        public static Dictionary<Command, Delegate> Commands = new Dictionary<Command, Delegate>();

        // Chargement au démarrage : parcourt toutes les méthodes de l'assembly et enregistre
        // celles annotées avec [ChatCommand] dans le dictionnaire Commands.
        [StartupInvoke("InGame Commands", StartupInvokePriority.Eighth)]
        public static void LoadChatCommands()
        {
            foreach (var item in Program.WorldAssembly.GetTypes())
            {
                foreach (var subItem in item.GetMethods())
                {
                    var attribute = subItem.GetCustomAttributes(typeof(ChatCommand), false).FirstOrDefault() as ChatCommand;
                    if (attribute != null)
                    {
                        // Crée un délégué typé Action<string, WorldClient> depuis la méthode statique
                        Delegate del = Delegate.CreateDelegate(typeof(Action<string, WorldClient>), subItem);
                        Commands.Add(new Command(attribute.Name, attribute.Role), del);
                    }
                }
            }
        }

        // Traite un message de chat commençant par ".".
        // content = message complet (ex. ".item 12345 1"), client = joueur émetteur.
        public static void Handle(string content, WorldClient client)
        {
            // Extrait la première partie du message (ex. ".item")
            var comInfo = content.Split(null).ToList()[0];
            foreach (var com in Commands.Keys)
            {
                // Cherche la commande par nom (insensible à la casse, sans le ".")
                var com_ = Commands.ToList().Find(x => x.Key.Value.ToLower() == comInfo.Split('.')[1].ToLower());
                if (com_.Key == null)
                {
                    client.Character.Reply("La commande " + comInfo.Split('.')[1] + " n'éxiste pas");
                    return;
                }

                if (client.Account.Role < com_.Key.MinimumRoleRequired)
                {
                    client.Character.Reply("Vous n'avez pas les droits pour executer cette commande");
                    break;
                }
                else
                    if (com != null)
                    {
                        var action = Commands.First(x => x.Key.Value.ToLower() == comInfo.Split('.')[1].ToLower());
                        // Reconstruit les arguments en retirant le nom de la commande
                        var param = content.Split(null).ToList();
                        param.Remove(param[0]);
                        if (param.Count > 0)
                        {
                            try
                            {
                                action.Value.DynamicInvoke(string.Join(" ", param), client);
                            }
                            catch (Exception ex)
                            {
                                client.Character.ReplyError("Impossible d'executer la commande");
                            }
                        }
                        else
                        {
                            try
                            {
                                // Pas d'arguments : passe null au handler
                                action.Value.DynamicInvoke(null, client);
                            }
                            catch (Exception ex)
                            {
                                client.Character.ReplyError("Impossible d'executer la commande");
                            }
                        }
                        break;
                    }
            }
        }

    }
}