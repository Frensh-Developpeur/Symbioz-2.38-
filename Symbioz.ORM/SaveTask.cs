
using Symbioz;
using Symbioz.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;

namespace Symbioz.ORM
{
    /// <summary>
    /// Gestionnaire de sauvegarde différée (asynchrone) pour la base de données.
    /// Au lieu d'écrire immédiatement chaque changement en base (ce qui serait trop lent),
    /// SaveTask accumule les éléments à ajouter/modifier/supprimer dans des files d'attente,
    /// puis effectue une sauvegarde groupée à intervalles réguliers via un Timer.
    /// Cela réduit considérablement le nombre de requêtes SQL et améliore les performances du serveur.
    /// </summary>
    public static class SaveTask
    {
        // Logger pour afficher les messages de sauvegarde dans la console.
        static Logger logger = new Logger();

        /// <summary>Événement déclenché au début d'un cycle de sauvegarde.</summary>
        public static event Action OnSaveStarted;

        /// <summary>Délégué pour l'événement de fin de sauvegarde, avec le temps écoulé en secondes.</summary>
        public delegate void OnSaveEndedDel(int elapsed);

        /// <summary>Événement déclenché à la fin d'un cycle de sauvegarde, avec la durée en secondes.</summary>
        public static event OnSaveEndedDel OnSaveEnded;

        /// <summary>Timer qui déclenche la sauvegarde automatiquement à intervalles réguliers.</summary>
        private static System.Timers.Timer _timer;

        /// <summary>
        /// File d'attente des éléments à insérer en base de données (INSERT).
        /// Clé = Type de l'objet, Valeur = liste des objets à insérer.
        /// </summary>
        private static Dictionary<Type, List<ITable>> _newElements = new Dictionary<Type, List<ITable>>();

        /// <summary>
        /// File d'attente des éléments à mettre à jour en base de données (UPDATE).
        /// </summary>
        private static Dictionary<Type, List<ITable>> _updateElements = new Dictionary<Type, List<ITable>>();

        /// <summary>
        /// File d'attente des éléments à supprimer de la base de données (DELETE).
        /// </summary>
        private static Dictionary<Type, List<ITable>> _removeElements = new Dictionary<Type, List<ITable>>();

        /// <summary>Indicateur booléen : true si une sauvegarde est en cours.</summary>
        static bool Saving;

        /// <summary>
        /// Initialise le SaveTask et démarre le timer automatique de sauvegarde.
        /// </summary>
        /// <param name="seconds">Intervalle entre chaque sauvegarde automatique (en secondes).</param>
        public static void Initialize(int seconds)
        {
            // Convertit les secondes en millisecondes pour le Timer.
            _timer = new System.Timers.Timer(seconds * 1000);
            _timer.Elapsed += _timer_Elapsed; // Abonne l'événement de fin de timer à la méthode Save.
            _timer.AutoReset = true; // Le timer se réarme automatiquement après chaque déclenchement.
            _timer.Start();
        }

        /// <summary>
        /// Ajoute un élément à la file d'attente d'insertion (INSERT).
        /// Si l'élément est déjà dans la file, il n'est pas ajouté en double.
        /// Optionnellement, ajoute aussi l'élément dans la liste statique en mémoire de son type.
        /// </summary>
        /// <param name="element">L'élément ITable à insérer.</param>
        /// <param name="addtolist">Si true (par défaut), ajoute aussi l'élément dans la liste en mémoire.</param>
        public static void AddElement(ITable element, bool addtolist = true)
        {
            lock (_newElements) // Verrou pour garantir la sécurité des threads.
            {
                if (_newElements.ContainsKey(element.GetType()))
                {
                    // Si la clé existe déjà, on ajoute l'élément s'il n'est pas déjà présent.
                    if (!_newElements[element.GetType()].Contains(element))
                        _newElements[element.GetType()].Add(element);
                }
                else
                {
                    // Première fois qu'on voit ce type : on crée une nouvelle liste.
                    _newElements.Add(element.GetType(), new List<ITable> { element });
                }
            }

            if (addtolist)
            {
                // Ajoute aussi l'élément dans la liste statique en mémoire vive du serveur.
                AddToList(element);
            }
        }

        /// <summary>
        /// Ajoute un élément directement dans la liste statique en mémoire de son type.
        /// Chaque classe de table possède un champ statique List&lt;T&gt; (ex : AccountRecord.Accounts)
        /// qui sert de cache en mémoire. Cette méthode utilise la réflexion pour trouver ce champ et y ajouter l'élément.
        /// </summary>
        /// <param name="element">L'élément ITable à ajouter dans la liste en mémoire.</param>
        public static void AddToList(ITable element)
        {
            #region Add value into array
            // Récupère le champ statique List<T> de la classe (cache mémoire).
            var field = GetCache(element);
            if (field == null)
            {
                logger.Error("Unable to add record value to the list, static list field wasnt finded");
                return;
            }

            // Récupère la méthode Add() de la liste via réflexion.
            var method = field.FieldType.GetMethod("Add");
            if (method == null)
            {
                Console.WriteLine("Unable to add record value to the list, add method wasnt finded");
                return;
            }

            // Invoque List<T>.Add(element) sur la liste statique (field.GetValue(null) = valeur d'un champ statique).
            method.Invoke(field.GetValue(null), new object[] { element });
            #endregion
        }

        /// <summary>
        /// Ajoute un élément à la file d'attente de mise à jour (UPDATE).
        /// Si l'élément est déjà dans la file d'insertion (_newElements), il est ignoré
        /// (un INSERT à venir inclura déjà les dernières valeurs).
        /// </summary>
        /// <param name="element">L'élément ITable à mettre à jour.</param>
        public static void UpdateElement(ITable element)
        {
            lock (_updateElements)
            {
                // Si l'élément est déjà en attente d'insertion, pas besoin de l'updater séparément.
                if (_newElements.ContainsKey(element.GetType()) && _newElements[element.GetType()].Contains(element))
                    return;

                if (_updateElements.ContainsKey(element.GetType()))
                {
                    if (!_updateElements[element.GetType()].Contains(element))
                        _updateElements[element.GetType()].Add(element);
                }
                else
                {
                    _updateElements.Add(element.GetType(), new List<ITable> { element });
                }
            }
        }

        /// <summary>
        /// Ajoute un élément à la file d'attente de suppression (DELETE).
        /// Si l'élément était en attente d'insertion, il est simplement retiré de cette file
        /// (pas besoin de l'insérer puis de le supprimer).
        /// Si l'élément était en attente de mise à jour, il est retiré de cette file également.
        /// Optionnellement, retire l'élément de la liste statique en mémoire.
        /// </summary>
        /// <param name="element">L'élément ITable à supprimer.</param>
        /// <param name="removefromlist">Si true (par défaut), retire aussi l'élément de la liste en mémoire.</param>
        public static void RemoveElement(ITable element, bool removefromlist = true)
        {
            if (element == null)
                return;

            lock (_removeElements)
            {
                // Si l'élément était en attente d'insertion, on l'annule simplement (pas de DELETE nécessaire).
                if (_newElements.ContainsKey(element.GetType()) && _newElements[element.GetType()].Contains(element))
                {
                    RemoveFromList(element);
                    _newElements[element.GetType()].Remove(element);
                    return;
                }

                // Si l'élément était en attente de mise à jour, on retire cette demande.
                if (_updateElements.ContainsKey(element.GetType()) && _updateElements[element.GetType()].Contains(element))
                    _updateElements[element.GetType()].Remove(element);

                // Ajoute à la file de suppression.
                if (_removeElements.ContainsKey(element.GetType()))
                {
                    if (!_removeElements[element.GetType()].Contains(element))
                        _removeElements[element.GetType()].Add(element);
                }
                else
                {
                    _removeElements.Add(element.GetType(), new List<ITable> { element });
                }
            }

            if (removefromlist)
            {
                // Retire l'élément de la liste statique en mémoire.
                RemoveFromList(element);
            }
        }

        /// <summary>
        /// Retire un élément directement de la liste statique en mémoire de son type.
        /// Utilise la réflexion pour trouver la méthode Remove() de la liste statique.
        /// </summary>
        /// <param name="element">L'élément ITable à retirer de la liste en mémoire.</param>
        public static void RemoveFromList(ITable element)
        {
            var field = GetCache(element);
            if (field == null)
            {
                logger.Alert("[Remove] Erreur ! Field unknown");
                return;
            }

            var method = field.FieldType.GetMethod("Remove");
            if (method == null)
            {
                logger.Alert("[Remove] Erreur ! Field unknown");
                return;
            }

            // Invoque List<T>.Remove(element) sur la liste statique.
            method.Invoke(field.GetValue(null), new object[] { element });
        }

        /// <summary>
        /// Méthode appelée automatiquement par le Timer à chaque intervalle.
        /// Elle déclenche le cycle de sauvegarde.
        /// </summary>
        private static void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Save();
        }

        /// <summary>
        /// Effectue un cycle complet de sauvegarde :
        /// 1. Traite toutes les suppressions (_removeElements)
        /// 2. Traite toutes les insertions (_newElements)
        /// 3. Traite toutes les mises à jour (_updateElements)
        /// Le timer est arrêté pendant la sauvegarde pour éviter un second déclenchement simultané.
        /// Un chronomètre mesure la durée totale, affichée via l'événement OnSaveEnded.
        /// </summary>
        public static void Save()
        {
            Saving = true;
            Stopwatch w = Stopwatch.StartNew(); // Démarre le chronomètre.

            // Déclenche l'événement OnSaveStarted si des abonnés existent.
            if (OnSaveStarted != null)
                OnSaveStarted();

            // Arrête le timer pour éviter un second Save() pendant qu'on est déjà en train de sauvegarder.
            _timer.Stop();

            try
            {
                // --- ÉTAPE 1 : Traitement des suppressions ---
                var types = _removeElements.Keys.ToList();
                foreach (var type in types)
                {
                    List<ITable> elements;
                    lock (_removeElements)
                        elements = _removeElements[type];

                    try
                    {
                        // Exécute le DELETE pour tous les éléments de ce type.
                        DatabaseManager.GetInstance().WriterInstance(type, DatabaseAction.Remove, elements.ToArray());
                    }
                    catch (Exception e) { logger.Error(e.Message); }

                    // Retire les éléments traités de la file (Skip = ignore les N premiers éléments déjà traités).
                    lock (_removeElements)
                        _removeElements[type] = _removeElements[type].Skip(elements.Count).ToList();
                }

                // --- ÉTAPE 2 : Traitement des insertions ---
                types = _newElements.Keys.ToList();
                foreach (var type in types)
                {
                    List<ITable> elements;
                    lock (_newElements)
                        elements = _newElements[type];

                    try
                    {
                        // Exécute le INSERT pour tous les éléments de ce type.
                        DatabaseManager.GetInstance().WriterInstance(type, DatabaseAction.Add, elements.ToArray());
                    }
                    catch (Exception e) { logger.Error(e.ToString()); }

                    lock (_newElements)
                        _newElements[type] = _newElements[type].Skip(elements.Count).ToList();
                }

                // --- ÉTAPE 3 : Traitement des mises à jour ---
                types = _updateElements.Keys.ToList();
                foreach (var type in types)
                {
                    List<ITable> elements;
                    lock (_updateElements)
                        elements = _updateElements[type];

                    try
                    {
                        // Exécute le UPDATE pour tous les éléments de ce type.
                        DatabaseManager.GetInstance().WriterInstance(type, DatabaseAction.Update, elements.ToArray());
                    }
                    catch (Exception e) { logger.Error(e.ToString()); }

                    lock (_updateElements)
                    {
                        _updateElements[type] = _updateElements[type].Skip(elements.Count).ToList();
                    }
                }

                // Redémarre le timer pour le prochain cycle de sauvegarde.
                _timer.Start();

                // Déclenche l'événement OnSaveEnded avec la durée en secondes.
                if (OnSaveEnded != null)
                    OnSaveEnded(w.Elapsed.Seconds);
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());
                Saving = false;
            }

            Saving = false;
        }

        /// <summary>
        /// Recherche le champ statique List&lt;T&gt; d'une classe de table (cache mémoire).
        /// Chaque classe de table possède un champ statique dont le nom correspond au nom de la table SQL.
        /// Exemple : AccountRecord possède un champ statique "Accounts" de type List&lt;AccountRecord&gt;.
        /// </summary>
        /// <param name="type">Type de la classe ITable à inspecter.</param>
        /// <returns>Le FieldInfo du champ statique List&lt;T&gt;, ou null si non trouvé.</returns>
        public static FieldInfo GetCache(Type type)
        {
            // Récupère l'attribut [Table] pour obtenir le nom de la table.
            var attribute = type.GetCustomAttribute(typeof(TableAttribute), false);
            if (attribute == null)
                return null;

            // Cherche un champ statique dont le nom correspond au nom de la table (insensible à la casse).
            // Ce champ doit être statique et de type générique (List<T>).
            var field = type.GetFields().FirstOrDefault(x => x.Name.ToLower() == (attribute as TableAttribute).tableName.ToLower());
            if (field == null || !field.IsStatic || !field.FieldType.IsGenericType)
                return null;

            return field;
        }

        /// <summary>
        /// Surcharge de GetCache qui accepte directement une instance ITable.
        /// Délègue à GetCache(Type).
        /// </summary>
        /// <param name="table">Instance d'un objet ITable.</param>
        /// <returns>Le FieldInfo du champ statique List&lt;T&gt;, ou null si non trouvé.</returns>
        private static FieldInfo GetCache(ITable table)
        {
            return GetCache(table.GetType());
        }
    }
}
