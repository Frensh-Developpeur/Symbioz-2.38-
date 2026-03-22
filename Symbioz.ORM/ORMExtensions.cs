using Symbioz.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Symbioz
{
    /// <summary>
    /// Classe statique de méthodes d'extension pour les objets ITable.
    /// En C#, les "méthodes d'extension" permettent d'appeler des méthodes sur un objet
    /// comme si elles faisaient partie de sa classe, grâce au mot-clé "this" en premier paramètre.
    /// Exemple : monRecord.AddElement() au lieu de SaveTask.AddElement(monRecord).
    ///
    /// Il existe deux familles de méthodes ici :
    /// - Méthodes différées (AddElement, UpdateElement, RemoveElement) : la sauvegarde est mise en file
    ///   d'attente et exécutée lors du prochain cycle du SaveTask (toutes les N secondes).
    /// - Méthodes immédiates (AddInstantElement, UpdateInstantElement, RemoveInstantElement) : la
    ///   sauvegarde est écrite directement en base de données, sans attendre le SaveTask.
    /// </summary>
    public static class ORMExtensions
    {
        /// <summary>
        /// Verrou partagé utilisé pour synchroniser les accès concurrents aux opérations immédiates.
        /// "lock(Locker)" garantit qu'un seul thread à la fois peut exécuter le bloc verrouillé.
        /// </summary>
        public static object Locker = new object();

        /// <summary>
        /// Ajoute cet élément à la file d'attente de mise à jour (UPDATE différé).
        /// La sauvegarde sera effectuée lors du prochain cycle du SaveTask.
        /// Utiliser cette méthode plutôt qu'UpdateInstantElement quand la mise à jour n'est pas urgente.
        /// </summary>
        /// <param name="table">L'objet ITable à mettre à jour.</param>
        public static void UpdateElement(this ITable table)
        {
            SaveTask.UpdateElement(table);
        }

        /// <summary>
        /// Ajoute cet élément à la file d'attente d'insertion (INSERT différé).
        /// La sauvegarde sera effectuée lors du prochain cycle du SaveTask.
        /// </summary>
        /// <param name="table">L'objet ITable à insérer.</param>
        /// <param name="addtolist">Si true (par défaut), ajoute aussi l'élément dans la liste en mémoire vive.</param>
        public static void AddElement(this ITable table, bool addtolist = true)
        {
            SaveTask.AddElement(table, addtolist);
        }

        /// <summary>
        /// Ajoute cet élément à la file d'attente de suppression (DELETE différé).
        /// La sauvegarde sera effectuée lors du prochain cycle du SaveTask.
        /// </summary>
        /// <param name="table">L'objet ITable à supprimer.</param>
        /// <param name="removefromlist">Si true (par défaut), retire aussi l'élément de la liste en mémoire vive.</param>
        public static void RemoveElement(this ITable table, bool removefromlist = true)
        {
            SaveTask.RemoveElement(table, removefromlist);
        }

        /// <summary>
        /// Insère immédiatement cet élément en base de données (INSERT instantané).
        /// Contrairement à AddElement, la requête SQL est exécutée au moment même de l'appel.
        /// Le verrou garantit qu'aucune autre opération simultanée ne perturbe l'insertion.
        /// </summary>
        /// <typeparam name="T">Type concret de la table (doit implémenter ITable).</typeparam>
        /// <param name="table">L'objet T à insérer immédiatement.</param>
        /// <param name="addtolist">Si true (par défaut), ajoute aussi l'élément dans la liste en mémoire.</param>
        public static void AddInstantElement<T>(this T table, bool addtolist = true) where T : ITable
        {
            lock (Locker) // Un seul thread à la fois peut exécuter ce bloc.
            {
                DatabaseWriter<T>.InstantInsert(table);
                if (addtolist)
                    SaveTask.AddToList(table); // Met aussi à jour le cache mémoire.
            }
        }

        /// <summary>
        /// Met à jour immédiatement cet élément en base de données (UPDATE instantané).
        /// </summary>
        /// <typeparam name="T">Type concret de la table.</typeparam>
        /// <param name="table">L'objet T à mettre à jour immédiatement.</param>
        public static void UpdateInstantElement<T>(this T table) where T : ITable
        {
            lock (Locker)
                DatabaseWriter<T>.InstantUpdate(table);
        }

        /// <summary>
        /// Supprime immédiatement cet élément de la base de données (DELETE instantané).
        /// </summary>
        /// <typeparam name="T">Type concret de la table.</typeparam>
        /// <param name="table">L'objet T à supprimer immédiatement.</param>
        /// <param name="removefromList">Si true (par défaut), retire aussi l'élément du cache mémoire.</param>
        public static void RemoveInstantElement<T>(this T table, bool removefromList = true) where T : ITable
        {
            lock (Locker)
            {
                DatabaseWriter<T>.InstantRemove(table);
                if (removefromList)
                    SaveTask.RemoveFromList(table); // Retire du cache mémoire.
            }
        }

        /// <summary>
        /// Supprime immédiatement une collection d'éléments du même type en une seule opération.
        /// Plus efficace que d'appeler RemoveInstantElement en boucle pour de nombreux éléments.
        /// Le paramètre "type" est nécessaire car C# ne peut pas inférer le type d'une IEnumerable&lt;ITable&gt; générique.
        /// </summary>
        /// <param name="tables">Collection d'éléments ITable à supprimer.</param>
        /// <param name="type">Type concret des éléments (ex : typeof(AccountRecord)).</param>
        /// <param name="removefromList">Si true (par défaut), retire aussi les éléments du cache mémoire.</param>
        public static void RemoveInstantElements(this IEnumerable<ITable> tables, Type type, bool removefromList = true)
        {
            // Exécute une seule requête DELETE groupée pour tous les éléments du même type.
            DatabaseManager.GetInstance().WriterInstance(type, DatabaseAction.Remove, tables.ToArray());

            if (removefromList)
            {
                // Retire chaque élément du cache mémoire individuellement.
                foreach (var table in tables)
                {
                    SaveTask.RemoveFromList(table);
                }
            }
        }

        /// <summary>
        /// Insère immédiatement une collection d'éléments du même type en une seule opération.
        /// </summary>
        /// <param name="tables">Collection d'éléments ITable à insérer.</param>
        /// <param name="type">Type concret des éléments (ex : typeof(AccountRecord)).</param>
        /// <param name="addtoList">Si true (par défaut), ajoute aussi les éléments dans le cache mémoire.</param>
        public static void AddInstantElements(this IEnumerable<ITable> tables, Type type, bool addtoList = true)
        {
            // Exécute une seule requête INSERT groupée pour tous les éléments du même type.
            DatabaseManager.GetInstance().WriterInstance(type, DatabaseAction.Add, tables.ToArray());

            if (addtoList)
            {
                // Ajoute chaque élément dans le cache mémoire individuellement.
                foreach (var table in tables)
                {
                    SaveTask.AddToList(table);
                }
            }
        }

        /// <summary>
        /// Met à jour immédiatement une collection d'éléments du même type en une seule opération.
        /// </summary>
        /// <param name="tables">Collection d'éléments ITable à mettre à jour.</param>
        /// <param name="type">Type concret des éléments (ex : typeof(AccountRecord)).</param>
        public static void UpdateInstantElements(this IEnumerable<ITable> tables, Type type)
        {
            // Exécute une seule requête UPDATE groupée pour tous les éléments du même type.
            DatabaseManager.GetInstance().WriterInstance(type, DatabaseAction.Update, tables.ToArray());
        }
    }
}
