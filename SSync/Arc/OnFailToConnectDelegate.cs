using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSync.Arc
{
    /// <summary>
    /// Délégué appelé quand une tentative de connexion TCP échoue.
    /// Reçoit l'exception décrivant la raison de l'échec (refus, timeout, etc.).
    /// Utilisé par AbstractClient pour notifier l'appelant d'une erreur de connexion.
    /// </summary>
    public delegate void OnFailToConnectDelegate(Exception ex);
}
