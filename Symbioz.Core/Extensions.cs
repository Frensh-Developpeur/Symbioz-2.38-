using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YAXLib;

namespace Symbioz.Core
{
    /// <summary>
    /// Classe de méthodes d'extension utilitaires utilisées dans tout le projet Symbioz.
    /// Regroupe des helpers pour : calculs de pourcentage, tirage aléatoire,
    /// sérialisation XML, conversion CSV, mélange de collections, etc.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Vérifie qu'un élément satisfait tous les prédicats d'une liste.
        /// Retourne false dès qu'un prédicat échoue (court-circuit).
        /// </summary>
        public static bool VerifyPredicates<T>(List<Predicate<T>> predicates, T item)
        {
            foreach (Predicate<T> predicate in predicates)
            {
                if (!predicate(item))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Calcule le montant correspondant à un pourcentage d'une valeur entière.
        /// Exemple : 200.GetPercentageOf(50) == 100
        /// </summary>
        public static int GetPercentageOf(this int value, int percentage)
        {
            return (int)((double)value * (double)((double)percentage / (double)100));
        }

        /// <summary>
        /// Calcule le pourcentage qu'une valeur représente par rapport à une longueur totale.
        /// Exemple : 50.Percentage(200) == 25 (50 est 25% de 200)
        /// </summary>
        public static int Percentage(this int current, int lenght)
        {
            return (int)(((double)current / (double)lenght) * (double)100);
        }

        /// <summary>Variante long de Percentage.</summary>
        public static long Percentage(this long current, long lenght)
        {
            return (long)(((double)current / (double)lenght) * 100d);
        }

        /// <summary>Variante long de GetPercentageOf.</summary>
        public static long GetPercentageOf(this long value, int percentage)
        {
            return (long)((double)value * (double)((double)percentage / (double)100));
        }

        /// <summary>
        /// Retourne un élément aléatoire d'une énumérable.
        /// Retourne la valeur par défaut du type si la collection est vide.
        /// </summary>
        public static T Random<T>(this IEnumerable<T> enumerable)
        {
            int count = enumerable.Count();

            if (count <= 0)
                return default(T);

            return enumerable.ElementAt(new AsyncRandom().Next(count));
        }

        /// <summary>
        /// Retourne un tableau de 'count' éléments tirés aléatoirement (avec remise possible).
        /// </summary>
        public static T[] Random<T>(this IEnumerable<T> enumerable, int count)
        {
            T[] array = new T[count];

            int lenght = enumerable.Count();

            if (lenght <= 0)
                return new T[0];

            var random = new AsyncRandom();

            for (int i = 0; i < count; i++)
            {
                array[i] = enumerable.ElementAt(random.Next(lenght));
            }

            return array;
        }

        /// <summary>
        /// Sérialise n'importe quel objet en XML via YAXLib.
        /// Utilisé notamment pour sauvegarder les configurations.
        /// </summary>
        public static string XMLSerialize(this object obj)
        {
            YAXSerializer serializer = new YAXSerializer(obj.GetType());
            return serializer.Serialize(obj);
        }

        /// <summary>
        /// Calcule la moyenne d'un tableau de valeurs numériques (utilise dynamic pour la somme).
        /// Retourne 0 si le tableau est vide.
        /// </summary>
        public static double Med<T>(this T[] array)
        {
            if (array.Length == 0)
            {
                return 0;
            }
            dynamic sum = 0;

            foreach (var item in array)
            {
                sum += item;
            }

            return (double)(sum / array.Length);
        }

        /// <summary>
        /// Désérialise une chaîne XML en objet du type spécifié via YAXLib.
        /// Si la chaîne est vide, crée une instance par défaut du type.
        /// </summary>
        public static object XMLDeserialize(this string content, Type type)
        {
            if (content == string.Empty)
                return Activator.CreateInstance(type);

            YAXSerializer serializer = new YAXSerializer(type);
            return Convert.ChangeType(serializer.Deserialize(content), type);
        }

        /// <summary>
        /// Désérialise un XML en déduisant le type depuis la balise racine et l'assembly fourni.
        /// Moins performant que la surcharge avec Type explicite.
        /// </summary>
        public static object XMLDeserialize(this string content, Assembly assembly)
        {
            // Extrait le nom du type depuis la première balise XML (ex. "<WorldConfiguration>" → "WorldConfiguration")
            string typeAsString = new string(content.Split('>')[0].Skip(1).ToArray());
            var type = assembly.GetTypes().FirstOrDefault(x => x.Name == typeAsString);
            return XMLDeserialize(content, type);
        }

        /// <summary>Désérialise une chaîne XML dans le type générique T.</summary>
        public static T XMLDeserialize<T>(this string content)
        {
            return (T)XMLDeserialize(content, typeof(T));
        }

        /// <summary>
        /// Convertit une liste en chaîne CSV (valeurs séparées par des virgules).
        /// Exemple : [1, 2, 3] → "1,2,3"
        /// </summary>
        public static string ToCSV(this IList list)
        {
            string str = string.Empty;
            if (list.Count == 0)
                return str;
            foreach (var value in list)
            {
                str += value.ToString() + ",";
            }
            // Supprime la virgule finale
            str = str.Remove(str.Length - 1);
            return str;
        }

        /// <summary>
        /// Convertit une chaîne CSV en liste typée.
        /// Exemple : "1,2,3".FromCSV&lt;int&gt;() → [1, 2, 3]
        /// </summary>
        public static List<T> FromCSV<T>(this string str, char separator = ',')
        {
            if (str == string.Empty)
                return new List<T>();
            var list = new List<T>();
            foreach (var value in str.Split(separator))
            {
                list.Add((T)Convert.ChangeType(value, typeof(T)));
            }
            return list;
        }

        /// <summary>
        /// Retourne un élément aléatoire d'une List&lt;T&gt;.
        /// Retourne la valeur par défaut si la liste est vide.
        /// </summary>
        public static T Random<T>(this List<T> list)
        {

            if (list.Count > 0)
            {
                return list[new AsyncRandom().Next(0, list.Count)];
            }
            else
                return default(T);
        }

        /// <summary>
        /// Parse une chaîne de valeurs séparées par des virgules en tableau typé T.
        /// Utilise un convertisseur fourni par l'appelant.
        /// </summary>
        public static T[] ParseCollection<T>(string str, Func<string, T> converter)
        {
            T[] result;
            if (string.IsNullOrEmpty(str))
            {
                result = new T[0];
            }
            else
            {
                int num = 0;
                int num2 = str.IndexOf(',', 0);
                if (num2 == -1)
                {
                    // Un seul élément, pas de virgule
                    result = new T[]
                    {
                        converter(str)
                    };
                }
                else
                {
                    T[] array = new T[str.CountOccurences(',', num, str.Length - num) + 1];
                    int num3 = 0;
                    while (num2 != -1)
                    {
                        array[num3] = converter(str.Substring(num, num2 - num));
                        num = num2 + 1;
                        num2 = str.IndexOf(',', num);
                        num3++;
                    }
                    array[num3] = converter(str.Substring(num, str.Length - num));
                    result = array;
                }
            }
            return result;
        }

        /// <summary>
        /// Extrait la valeur maximale d'une collection et retourne cette valeur + 1.
        /// Utile pour générer un nouvel ID unique (ex. prochain ID de personnage).
        /// Si la collection est vide, retourne la valeur par défaut fournie.
        /// </summary>
        public static TConvert DynamicPop<TObject, TConvert>(this IEnumerable<TObject> obj, Converter<TObject, TConvert> converter, long @default = 1)
        {
            if (obj.Count() == 0)
            {
                dynamic _defaut = @default;
                return (TConvert)_defaut;
            }
            var collection = Array.ConvertAll(obj.ToArray(), converter);
            Array.Sort(collection);
            // Retourne le dernier élément (le plus grand) + 1
            dynamic lastValue = collection.Last();
            return (TConvert)(lastValue + 1);
        }

        /// <summary>
        /// Mélange une collection selon des poids de probabilité.
        /// Les éléments ayant un indice de probabilité plus élevé ont plus de chances d'être choisis en premier.
        /// Lève une exception si les tailles de 'enumerable' et 'probabilities' diffèrent.
        /// </summary>
        public static IEnumerable<T> ShuffleWithProbabilities<T>(this IEnumerable<T> enumerable,
                                                                IEnumerable<int> probabilities)
        {
            var rand = new Random();
            var elements = enumerable.ToList();
            var result = new T[elements.Count];
            var indices = probabilities.ToList();

            if (elements.Count != indices.Count)
                throw new Exception("Probabilities must have the same length that the enumerable");

            int sum = indices.Sum();

            if (sum == 0)
                return Shuffle(elements);

            for (int i = 0; i < result.Length; i++)
            {
                int randInt = rand.Next(sum + 1);
                int currentSum = 0;
                for (int j = 0; j < indices.Count; j++)
                {
                    currentSum += indices[j];

                    if (currentSum >= randInt)
                    {
                        result[i] = elements[j];

                        // Met à jour la somme et retire l'élément sélectionné pour éviter les doublons
                        sum -= indices[j];
                        indices.RemoveAt(j);
                        elements.RemoveAt(j);
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Mélange aléatoirement une collection (algorithme de Fisher-Yates).
        /// Retourne les éléments via yield pour éviter les copies inutiles.
        /// </summary>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> enumerable)
        {
            var rand = new Random();

            T[] elements = enumerable.ToArray();
            // Note : i > 0 évite une dernière itération inutile (swap avec soi-même)
            for (int i = elements.Length - 1; i > 0; i--)
            {
                // Échange l'élément i avec un élément aléatoire parmi [0, i]
                int swapIndex = rand.Next(i + 1);
                T tmp = elements[i];
                elements[i] = elements[swapIndex];
                elements[swapIndex] = tmp;
            }
            // Retour paresseux (lazy) pour éviter les problèmes d'aliasing
            foreach (T element in elements)
            {
                yield return element;
            }
        }

        /// <summary>
        /// Convertit un tableau d'octets en chaîne hexadécimale minuscule.
        /// Exemple : [0xFF, 0x00] → "ff00"
        /// </summary>
        public static string ByteArrayToString(this byte[] bytes)
        {
            var output = new StringBuilder(bytes.Length);

            foreach (var t in bytes)
            {
                output.Append(t.ToString("X2"));
            }

            return output.ToString().ToLower();
        }

        /// <summary>
        /// Vérifie si deux dictionnaires ont exactement les mêmes clés et valeurs,
        /// sans se soucier de l'ordre d'insertion (comparaison "scramble").
        /// </summary>
        public static bool ScramEqualDictionary<T, T2>(this Dictionary<T, T2> first, Dictionary<T, T2> second)
        {
            if (first.Count != second.Count)
                return false;
            foreach (var data in first)
            {
                if (second.ContainsKey(data.Key))
                {
                    var value = second.First(x => x.Key.Equals(data.Key));
                    if (!value.Value.Equals(data.Value))
                        return false;
                }
                else
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Filtre un tableau de types pour ne garder que ceux ayant un attribut spécifique.
        /// Utilisé lors de l'initialisation pour trouver les handlers de messages.
        /// </summary>
        public static Type[] WithAttributes(this Type[] types, Type attributeType)
        {
            return Array.FindAll(types, x => x.GetCustomAttribute(attributeType) != null);
        }

        /// <summary>
        /// Retourne un dictionnaire associant chaque attribut de type T à la méthode qui le porte.
        /// Utile pour le système de dispatch de messages (MessageHandlerAttribute → méthode handler).
        /// </summary>
        public static Dictionary<T, MethodInfo> MethodsWhereAttributes<T>(this Type type) where T : Attribute
        {
            Dictionary<T, MethodInfo> results = new Dictionary<T, MethodInfo>();

            foreach (var method in type.GetMethods())
            {
                var attributes = method.GetCustomAttributes<T>();
                foreach (var attribute in attributes)
                {
                    results.Add(attribute, method);
                }
            }
            return results;
        }

        /// <summary>
        /// Effectue un tirage aléatoire avec un pourcentage de chance de succès.
        /// Retourne toujours true si percentage >= 100.
        /// Exemple : randomizer.TriggerAleat(30) → true 30% du temps.
        /// </summary>
        /// <param name="percentage">Pourcentage de chance (0-100).</param>
        public static bool TriggerAleat(this AsyncRandom randomizer, int percentage)
        {
            if (percentage >= 100)
            {
                return true;
            }
            // Tire un nombre entre 0 et 100 ; succès si inférieur ou égal au pourcentage
            return randomizer.Next(0, 101) <= percentage;
        }
    }
}
