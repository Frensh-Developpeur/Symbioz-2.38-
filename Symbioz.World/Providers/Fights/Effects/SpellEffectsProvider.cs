using Symbioz.Core.DesignPattern.StartupEngine;
using Symbioz.World.Models.Effects;
using Symbioz.World.Models.Fights.Fighters;
using Symbioz.World.Models.Maps;
using Symbioz.World.Records.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Symbioz.World.Network;
using Symbioz.Protocol.Selfmade.Enums;

namespace Symbioz.World.Providers.Fights.Effects
{
    // Ce provider fait le lien entre un effet de sort (EffectsEnum) et la classe qui sait le gérer
    // Au démarrage, il scanne tous les fichiers et enregistre les classes marquées [SpellEffectHandler(...)]
    // Quand un sort est lancé, il trouve la bonne classe et l'exécute
    class SpellEffectsProvider
    {
        // Dictionnaire qui associe les attributs [SpellEffectHandler] à la classe correspondante
        // Clé   = tableau d'attributs (une classe peut gérer plusieurs effets)
        // Valeur = le Type de la classe handler (ex: DirectDamage, TakenDamageMultiply...)
        private static Dictionary<SpellEffectHandlerAttribute[], Type> Handlers = new Dictionary<SpellEffectHandlerAttribute[], Type>();

        // Appelé automatiquement au démarrage du serveur (priorité Eighth)
        // Scanne tout l'assembly World pour trouver les classes avec [SpellEffectHandler]
        [StartupInvoke(StartupInvokePriority.Eighth)]
        public static void Initialize()
        {
            foreach (var type in Program.WorldAssembly.GetTypes())
            {
                // On récupère tous les attributs [SpellEffectHandler] de cette classe
                SpellEffectHandlerAttribute[] handlers = type.GetCustomAttributes<SpellEffectHandlerAttribute>().ToArray();

                // Si la classe a au moins un attribut [SpellEffectHandler], on l'enregistre
                if (handlers.Length > 0)
                {
                    Handlers.Add(handlers, type);
                }
            }
        }

        // Appelé par SpellEffectsManager pour chaque effet d'un sort lancé
        // Cherche la classe handler correspondant à l'effet et l'exécute
        public static void Handle(Fighter source, SpellLevelRecord level, EffectInstance effect, Fighter[] targets, MapPoint castPoint, bool critical)
        {
            // On cherche dans le dictionnaire la classe qui gère cet EffectEnum
            var handlerDatas = Handlers.FirstOrDefault(x => x.Key.FirstOrDefault(w => w.Effect == effect.EffectEnum) != null);

            if (handlerDatas.Value != null)
            {
                // On crée une instance de la classe handler (ex: new DirectDamage(...))
                // et on appelle Execute() qui déclenche Apply()
                SpellEffectHandler handler = (SpellEffectHandler)Activator.CreateInstance(handlerDatas.Value, new object[] { source, level, effect, targets, castPoint, critical });
                handler.Execute();
            }
            else
            {
                // Aucun handler trouvé pour cet effet → le sort ne fait rien
                // Si un Fondateur est connecté, il voit le message en jeu pour savoir quel effet implémenter
                var client = WorldServer.Instance.GetClients().FirstOrDefault();

                if (client != null && client.Account.Role == ServerRoleEnum.Fondator)
                {
                    client.Character.Reply("Effect " + effect.EffectEnum + " is not handled.");
                }
            }
        }
    }
}
