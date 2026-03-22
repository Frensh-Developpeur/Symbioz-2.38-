using Symbioz.Core.DesignPattern.StartupEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Symbioz.Core;
using Symbioz.World.Models.Fights.Fighters;

namespace Symbioz.World.Providers.Brain.Actions
{
    /// <summary>
    /// Registre des actions de l'IA des monstres.
    /// Au démarrage, scanne tous les types héritant de BrainAction et portant l'attribut [Brain(ActionType.X)]
    /// pour construire un dictionnaire ActionType → Type.
    /// Lors d'un tour de monstre, MonsterBrain appelle GetAction() pour instancier l'action souhaitée.
    /// Ce pattern (réflexion + attribut) permet d'ajouter de nouvelles actions sans modifier ce provider.
    /// </summary>
    class BrainProvider
    {
        static Logger logger = new Logger();

        // Dictionnaire : type d'action → classe concrète de BrainAction
        private static Dictionary<ActionType, Type> Brains = new Dictionary<ActionType, Type>();

        // Scanné au démarrage : charge automatiquement toutes les BrainAction déclarées dans l'assembly
        [StartupInvoke("Brain", StartupInvokePriority.Eighth)]
        public static void Initialize()
        {
            foreach (var type in Program.WorldAssembly.GetTypes())
            {
                if (type.BaseType == typeof(BrainAction))
                {
                    IEnumerable<BrainAttribute> attributes = type.GetCustomAttributes<BrainAttribute>();

                    foreach (var attribute in attributes)
                    {
                        Brains.Add(attribute.ActionType, type);
                    }
                    logger.Gray(type.Name + " loaded");
                }
            }
        }
        // Instancie dynamiquement la BrainAction correspondant au type d'action demandé
        public static BrainAction GetAction(BrainFighter fighter, ActionType actionType)
        {
            return (BrainAction)Activator.CreateInstance(Brains[actionType], new object[] { fighter });
        }
    }
}
