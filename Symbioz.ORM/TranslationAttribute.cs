using System;

namespace Symbioz.ORM
{
    /// <summary>
    /// Attribut qui signale qu'un champ nécessite une conversion personnalisée (sérialisation/désérialisation).
    /// Les types complexes (comme des enums ou des structures spéciales) ne peuvent pas être stockés
    /// directement en SQL. Cet attribut indique à l'ORM d'utiliser les méthodes ToString/Deserialize
    /// du type pour convertir la valeur vers/depuis une chaîne de caractères SQL.
    /// </summary>
    public class TranslationAttribute : Attribute
    {
        /// <summary>
        /// Si true : on est en mode lecture (désérialisation depuis la base de données vers l'objet C#).
        /// Si false : on est en mode écriture (sérialisation de l'objet C# vers une chaîne SQL).
        /// </summary>
        public bool readingMode;

        /// <summary>
        /// Constructeur de l'attribut Translation.
        /// </summary>
        /// <param name="readingMode">
        /// true = utilisé lors de la lecture (par défaut).
        /// false = utilisé lors de l'écriture dans la base de données.
        /// </param>
        public TranslationAttribute(bool readingMode = true)
        {
            this.readingMode = readingMode;
        }
    }
}
