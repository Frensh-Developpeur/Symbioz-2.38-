namespace Symbioz.Tools.D2O
{
    /// <summary>
    /// Interface que doivent implémenter toutes les classes représentant des données du jeu Dofus.
    /// Toute classe C# qui correspond à un type D2O (sorts, items, maps, etc.) doit implémenter
    /// cette interface pour pouvoir être instanciée par le système de désérialisation D2O.
    /// Le DataCenterTypeManager utilise cette interface pour découvrir et instancier ces classes.
    /// </summary>
    public interface IDataCenter
    {
        /// <summary>
        /// Nom du module auquel appartient cet objet de données.
        /// Le "module" correspond généralement au nom du fichier D2O source
        /// (ex: "Spells", "Items", "MapPositions").
        /// </summary>
        string Module { get; }
    }
}
