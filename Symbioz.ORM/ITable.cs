namespace Symbioz.ORM
{
    /// <summary>
    /// Interface de base que toutes les tables de la base de données doivent implémenter.
    /// En C#, une interface est un "contrat" : toute classe qui l'implémente s'engage à respecter ce contrat.
    /// Ici, ITable ne définit aucune méthode obligatoire, mais elle sert de marqueur :
    /// grâce à elle, l'ORM sait qu'une classe représente une table SQL.
    /// Exemple d'utilisation : public class AccountRecord : ITable { ... }
    /// </summary>
    public interface ITable
    {
        // Aucune méthode requise : ITable est une interface "marqueur".
        // Son seul rôle est de permettre au système ORM d'identifier les classes qui sont des tables.
    }
}
