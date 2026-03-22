
using Symbioz.Core.DesignPattern.StartupEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Symbioz.Core;
using Symbioz.World.Models.Fights.FightModels;
using Symbioz.World.Records.Monsters;
using Symbioz.World.Models.Fights.Fighters;

namespace Symbioz.World.Providers.Brain.Behaviors
{
    /// <summary>
    /// Registre des comportements personnalisés des monstres.
    /// Au démarrage, scanne tous les types portant l'attribut [Behavior("Identifiant")]
    /// et construit un dictionnaire nom → type.
    /// Quand un monstre avec un BehaviorName est créé en combat, MonsterBrain appelle
    /// GetBehavior() pour instancier le Behavior correspondant par réflexion.
    /// </summary>
    public class BehaviorManager
    {
        private static Logger logger = new Logger();

        // Dictionnaire : identifiant du comportement (string) → type C# concret
        private static Dictionary<string, Type> BehaviorTypes = new Dictionary<string, Type>();

        // Initialisation au démarrage : charge tous les Behavior déclarés dans l'assembly
        [StartupInvoke("Behaviors", StartupInvokePriority.Eighth)]
        public static void Initialize()
        {
            foreach (var type in Program.WorldAssembly.GetTypes())
            {
                BehaviorAttribute attribute = type.GetCustomAttribute<BehaviorAttribute>();

                if (attribute != null)
                {
                    BehaviorTypes.Add(attribute.Identifier, type);
                }
            }
        }

        // Instancie le Behavior correspondant au nom donné et l'attache au fighter
        public static Behavior GetBehavior(string behaviorName, BrainFighter fighter)
        {
            var data = BehaviorTypes.FirstOrDefault(x => x.Key == behaviorName);

            if (data.Value != null)
            {
                return (Behavior)Activator.CreateInstance(data.Value, fighter);
            }
            else
            {
                logger.Error("Unable to handle beahvior identifier: " + behaviorName);
                return null;
            }
        }
        // Vérifie si un comportement avec cet identifiant est enregistré
        public static bool Exist(string behaviorName)
        {
            return BehaviorTypes.ContainsKey(behaviorName);
        }
    }
}
