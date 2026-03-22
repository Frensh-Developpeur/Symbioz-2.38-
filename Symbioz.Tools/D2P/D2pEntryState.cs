namespace Symbioz.Tools.D2P
{
    /// <summary>
    /// Représente l'état d'une entrée dans un fichier D2P.
    /// Une entrée peut être dans différents états selon les modifications apportées depuis l'ouverture du fichier.
    /// Cet état est utilisé pour savoir quoi faire lors de la sauvegarde du fichier.
    /// </summary>
    public enum D2pEntryState
    {
        /// <summary>Aucune modification — l'entrée est intacte telle qu'elle a été lue.</summary>
        None,
        /// <summary>L'entrée a été modifiée (son contenu a changé mais elle existait déjà).</summary>
        Dirty,
        /// <summary>L'entrée vient d'être ajoutée au fichier D2P et n'existait pas avant.</summary>
        Added,
        /// <summary>L'entrée a été supprimée du fichier D2P.</summary>
        Removed
    }
}