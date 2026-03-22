using System;

namespace Symbioz.ORM
{
    /// <summary>
    /// Attribut qui indique à l'ORM d'ignorer complètement un champ.
    /// Un champ marqué [Ignore] ne sera ni lu depuis la base de données, ni sauvegardé dedans.
    /// Utile pour les champs calculés, les propriétés temporaires ou les énumérations dérivées
    /// qui n'ont pas besoin d'être persistées en base de données.
    /// Exemple : [Ignore] public ServerRoleEnum RoleEnum { get { return (ServerRoleEnum)Role; } }
    /// </summary>
    public class IgnoreAttribute : Attribute
    {
        /// <summary>
        /// Constructeur vide : cet attribut n'a besoin d'aucun paramètre.
        /// Sa simple présence sur un champ suffit à l'exclure de la lecture/écriture SQL.
        /// </summary>
        public IgnoreAttribute()
        { }
    }
}
