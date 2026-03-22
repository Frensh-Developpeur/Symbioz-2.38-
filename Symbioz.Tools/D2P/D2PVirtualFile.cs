using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symbioz.Tools.D2P
{
    /// <summary>
    /// Représente un fichier "virtuel" issu d'une archive D2P.
    /// Un fichier virtuel est un fichier qui n'existe pas physiquement sur le disque,
    /// mais dont le contenu a été extrait (ou est en cours de création) depuis/vers un D2P.
    /// Il est utilisé comme conteneur temporaire pour le nom et les données brutes d'un fichier.
    /// </summary>
    public class D2PVirtualFile
    {
        /// <summary>
        /// Crée un fichier virtuel avec son nom et son contenu binaire.
        /// </summary>
        /// <param name="name">Nom du fichier (peut inclure un chemin relatif, ex: "maps/12.dlm").</param>
        /// <param name="content">Contenu binaire brut du fichier.</param>
        public D2PVirtualFile(string name, byte[] content)
        {
            this.Name = name;
            this.Content = content;
        }

        /// <summary>
        /// Nom (et chemin relatif) du fichier à l'intérieur de l'archive D2P.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Contenu binaire brut du fichier (données non décompressées).
        /// </summary>
        public byte[] Content { get; set; }
    }
}
