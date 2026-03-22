using System.IO;
using System.IO.Compression;
using zlib;

namespace Symbioz.MapLoader.IO
{
    /// <summary>
    /// Classe utilitaire pour la compression et la décompression de données.
    /// Fournit deux algorithmes différents :
    /// - GZip : compression standard (classes .NET intégrées)
    /// - Deflate via zlib : utilisé notamment pour les fichiers de maps DLM de Dofus
    /// </summary>
    public class ZipHelper
    {
        /// <summary>
        /// Compresse un tableau d'octets en utilisant l'algorithme GZip.
        /// </summary>
        /// <param name="data">Les données brutes à compresser.</param>
        /// <returns>Les données compressées au format GZip.</returns>
        public static byte[] Compress(byte[] data)
        {
            // On encapsule le tableau dans un MemoryStream pour pouvoir le passer au compresseur
            MemoryStream input = new MemoryStream(data);
            MemoryStream memoryStream = new MemoryStream();
            ZipHelper.Compress(input, memoryStream);
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Compresse un flux de données vers un autre flux en utilisant GZip.
        /// </summary>
        /// <param name="input">Flux source à compresser.</param>
        /// <param name="output">Flux de destination pour les données compressées.</param>
        public static void Compress(Stream input, Stream output)
        {
            // GZipStream en mode Compress : tout ce qui est écrit dans gZipStream est compressé vers output
            using (GZipStream gZipStream = new GZipStream(output, CompressionMode.Compress))
            {
                input.CopyTo(gZipStream);
            }
        }

        /// <summary>
        /// Décompresse un tableau d'octets au format GZip.
        /// </summary>
        /// <param name="data">Les données compressées GZip.</param>
        /// <returns>Les données décompressées.</returns>
        public static byte[] Uncompress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream memoryStream = new MemoryStream();
            ZipHelper.Uncompress(input, memoryStream);
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Décompresse un flux GZip vers un autre flux.
        /// </summary>
        /// <param name="input">Flux compressé GZip.</param>
        /// <param name="output">Flux de destination pour les données décompressées.</param>
        public static void Uncompress(Stream input, Stream output)
        {
            // leaveOpen: true indique de ne pas fermer le flux d'entrée après décompression
            using (GZipStream gZipStream = new GZipStream(input, CompressionMode.Decompress, true))
            {
                gZipStream.CopyTo(output);
            }
        }

        /// <summary>
        /// Décompresse des données en utilisant l'algorithme Deflate via la bibliothèque zlib.
        /// Utilisé pour les fichiers de maps Dofus (format DLM) qui utilisent zlib plutôt que GZip.
        /// La différence principale avec GZip : zlib n'a pas de header/footer de fichier.
        /// </summary>
        /// <param name="input">Flux contenant les données compressées zlib/Deflate.</param>
        /// <param name="output">Flux de destination pour les données décompressées.</param>
        public static void Deflate(Stream input, Stream output)
        {
            // ZOutputStream est un wrapper zlib qui décompresse les données à la volée
            ZOutputStream zOutputStream = new ZOutputStream(output);
            BinaryReader binaryReader = new BinaryReader(input);
            // Lecture de toutes les données du flux d'entrée et décompression vers la sortie
            zOutputStream.Write(binaryReader.ReadBytes((int)input.Length), 0, (int)input.Length);
            zOutputStream.Flush();
        }
    }
}
