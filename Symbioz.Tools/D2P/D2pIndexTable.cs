using SSync.IO;
using System.ComponentModel;
using System.IO;

namespace Symbioz.Tools.D2P
{
    /// <summary>
    /// Représente la table d'index d'un fichier D2P.
    /// Cette table est située à la toute fin du fichier (24 derniers octets).
    /// Elle contient les informations nécessaires pour naviguer dans le fichier :
    ///   - L'offset de base des données (OffsetBase)
    ///   - La taille totale des données (Size)
    ///   - La position et le nombre de définitions des entrées
    ///   - La position et le nombre de propriétés
    ///
    /// Structure binaire (24 octets, Big Endian) :
    ///   [int OffsetBase] [int Size] [int EntriesDefinitionOffset] [int EntriesCount]
    ///   [int PropertiesOffset] [int PropertiesCount]
    /// </summary>
    public class D2pIndexTable : INotifyPropertyChanged
    {
        /// <summary>Décalage depuis la fin du fichier pour trouver la table d'index (-24 octets).</summary>
        public const int TableOffset = -24;
        /// <summary>La table est toujours lue depuis la fin du fichier.</summary>
        public const SeekOrigin TableSeekOrigin = SeekOrigin.End;

        public event PropertyChangedEventHandler PropertyChanged;

        private D2pFile _Container;

        public D2pFile Container
        {
            get
            {
                return this._Container;
            }

            private set
            {
                if (this._Container == value)
                {
                    return;
                }
                this._Container = value;
                this.OnPropertyChanged("Container");
            }
        }

        private int _OffsetBase;

        public int OffsetBase
        {
            get
            {
                return this._OffsetBase;
            }

            set
            {
                if (this._OffsetBase == value)
                {
                    return;
                }
                this._OffsetBase = value;
                this.OnPropertyChanged("OffsetBase");
            }
        }

        private int _Size;

        public int Size
        {
            get
            {
                return this._Size;
            }

            set
            {
                if (this._Size == value)
                {
                    return;
                }
                this._Size = value;
                this.OnPropertyChanged("Size");
            }
        }

        private int _EntriesDefinitionOffset;

        public int EntriesDefinitionOffset
        {
            get
            {
                return this._EntriesDefinitionOffset;
            }

            set
            {
                if (this._EntriesDefinitionOffset == value)
                {
                    return;
                }
                this._EntriesDefinitionOffset = value;
                this.OnPropertyChanged("EntriesDefinitionOffset");
            }
        }

        private int _EntriesCount;

        public int EntriesCount
        {
            get
            {
                return this._EntriesCount;
            }

            set
            {
                if (this._EntriesCount == value)
                {
                    return;
                }
                this._EntriesCount = value;
                this.OnPropertyChanged("EntriesCount");
            }
        }

        private int _PropertiesOffset;

        public int PropertiesOffset
        {
            get
            {
                return this._PropertiesOffset;
            }

            set
            {
                if (this._PropertiesOffset == value)
                {
                    return;
                }
                this._PropertiesOffset = value;
                this.OnPropertyChanged("PropertiesOffset");
            }
        }

        private int _PropertiesCount;

        public int PropertiesCount
        {
            get
            {
                return this._PropertiesCount;
            }

            set
            {
                if (this._PropertiesCount == value)
                {
                    return;
                }
                this._PropertiesCount = value;
                this.OnPropertyChanged("PropertiesCount");
            }
        }

        /// <summary>
        /// Crée une table d'index associée à un fichier D2P.
        /// </summary>
        /// <param name="container">Le fichier D2P propriétaire de cette table.</param>
        public D2pIndexTable(D2pFile container)
        {
            this.Container = container;
        }

        /// <summary>
        /// Lit la table d'index depuis le flux binaire (24 octets, Big Endian).
        /// Le lecteur doit être positionné à -24 depuis la fin du fichier avant l'appel.
        /// </summary>
        /// <param name="reader">Lecteur positionné sur la table.</param>
        public void ReadTable(IDataReader reader)
        {
            this.OffsetBase = reader.ReadInt();             // Offset de début des données de fichiers
            this.Size = reader.ReadInt();                   // Taille totale de la zone de données
            this.EntriesDefinitionOffset = reader.ReadInt(); // Position des définitions des entrées
            this.EntriesCount = reader.ReadInt();           // Nombre d'entrées (fichiers) dans l'archive
            this.PropertiesOffset = reader.ReadInt();       // Position des propriétés (métadonnées)
            this.PropertiesCount = reader.ReadInt();        // Nombre de propriétés
        }

        /// <summary>
        /// Écrit la table d'index à la fin du flux binaire de destination.
        /// Toujours 24 octets, à la fin du fichier D2P.
        /// </summary>
        /// <param name="writer">Écrivain binaire cible.</param>
        public void WriteTable(IDataWriter writer)
        {
            writer.WriteInt(this.OffsetBase);
            writer.WriteInt(this.Size);
            writer.WriteInt(this.EntriesDefinitionOffset);
            writer.WriteInt(this.EntriesCount);
            writer.WriteInt(this.PropertiesOffset);
            writer.WriteInt(this.PropertiesCount);
        }

        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}