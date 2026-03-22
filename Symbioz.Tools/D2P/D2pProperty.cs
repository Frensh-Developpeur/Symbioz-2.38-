using SSync.IO;
using System.ComponentModel;

namespace Symbioz.Tools.D2P
{
    /// <summary>
    /// Représente une propriété (métadonnée) stockée dans un fichier D2P.
    /// Les propriétés sont des paires clé/valeur situées dans l'index du fichier D2P.
    /// Exemple : une propriété de type "link" indique que ce D2P référence un autre fichier D2P.
    /// Elles sont lues/écrites sous forme de chaînes UTF.
    /// </summary>
    public class D2pProperty : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _Key;

        public string Key
        {
            get
            {
                return this._Key;
            }

            set
            {
                if (string.Equals(this._Key, value))
                {
                    return;
                }
                this._Key = value;
                this.OnPropertyChanged("Key");
            }
        }

        private string _Value;

        public string Value
        {
            get
            {
                return this._Value;
            }

            set
            {
                if (string.Equals(this._Value, value))
                {
                    return;
                }
                this._Value = value;
                this.OnPropertyChanged("Value");
            }
        }

        /// <summary>
        /// Crée une propriété vide (les champs Key et Value devront être renseignés ensuite).
        /// </summary>
        public D2pProperty()
        {
        }

        /// <summary>
        /// Crée une propriété avec une clé et une valeur.
        /// </summary>
        /// <param name="key">Nom de la propriété (ex : "link").</param>
        /// <param name="property">Valeur de la propriété (ex : nom d'un autre fichier D2P).</param>
        public D2pProperty(string key, string property)
        {
            this.Key = key;
            this.Value = property;
        }

        /// <summary>
        /// Lit une propriété depuis un flux binaire D2P.
        /// Les propriétés sont stockées sous la forme [UTF: clé] [UTF: valeur].
        /// </summary>
        /// <param name="reader">Lecteur positionné sur la propriété à lire.</param>
        public void ReadProperty(IDataReader reader)
        {
            this.Key = reader.ReadUTF();   // Lecture de la clé (chaîne UTF)
            this.Value = reader.ReadUTF(); // Lecture de la valeur (chaîne UTF)
        }

        /// <summary>
        /// Écrit cette propriété dans un flux binaire au format D2P.
        /// </summary>
        /// <param name="writer">Écrivain binaire cible.</param>
        public void WriteProperty(IDataWriter writer)
        {
            writer.WriteUTF(this.Key);   // Écriture de la clé
            writer.WriteUTF(this.Value); // Écriture de la valeur
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