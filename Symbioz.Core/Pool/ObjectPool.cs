using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Symbioz.Core.Pool
{
    /// <summary>
    /// Pool d'objets générique permettant de réutiliser des instances au lieu d'en créer de nouvelles.
    /// Réduit la pression sur le Garbage Collector (GC) pour les objets créés très fréquemment.
    ///
    /// Fonctionnement :
    ///   - Obtain() : récupère un objet du pool (ou en crée un si le pool est vide).
    ///   - Recycle() : remet un objet dans le pool après utilisation (appelle Cleanup() si IPooledObject).
    ///   - Les <m_minSize> premiers objets sont stockés en référence forte (HardReference) :
    ///     ils ne seront pas collectés par le GC.
    ///   - Au-delà de m_minSize, les objets sont stockés en WeakReference : le GC peut les libérer.
    ///   - En mode "balanced" (IsBalanced=true), le pool vérifie que Recycle est appelé autant que Obtain.
    /// </summary>
    /// <typeparam name="T">Type des objets à pooler (doit être une classe).</typeparam>
    public class ObjectPool<T> : IObjectPool where T : class
    {
        // true si le pool surveille l'équilibre Obtain/Recycle (utile pour détecter les fuites)
        private bool m_isBalanced;

        // File d'attente sans verrou contenant les objets disponibles (Object ou WeakReference)
        private readonly LockFreeQueue<object> m_queue = new LockFreeQueue<object>();

        // Nombre minimum d'objets à conserver en référence forte (évite les re-créations fréquentes)
        private volatile int m_minSize = 25;

        // Compteur des objets actuellement en référence forte dans le pool
        private volatile int m_hardReferences = 0;

        // Compteur des objets actuellement empruntés (hors du pool), utilisé en mode balanced
        private volatile int m_obtainedReferenceCount;

        // Fonction factory fournie par l'utilisateur pour créer un nouvel objet si le pool est vide
        private readonly Func<T> m_createObj;
        public int HardReferenceCount
        {
            get
            {
                return this.m_hardReferences;
            }
        }
        public int MinimumSize
        {
            get
            {
                return this.m_minSize;
            }
            set
            {
                this.m_minSize = value;
            }
        }
        public int AvailableCount
        {
            get
            {
                return this.m_queue.Count;
            }
        }
        public int ObtainedCount
        {
            get
            {
                return this.m_obtainedReferenceCount;
            }
        }
        public ObjectPoolInfo Info
        {
            get
            {
                ObjectPoolInfo result;
                result.HardReferences = this.m_hardReferences;
                result.WeakReferences = this.m_queue.Count - this.m_hardReferences;
                return result;
            }
        }
        public bool IsBalanced
        {
            get
            {
                return this.m_isBalanced;
            }
            set
            {
                this.m_isBalanced = value;
            }
        }

        public ObjectPool(Func<T> func)
            : this(func, false)
        {
        }

        public ObjectPool(Func<T> func, bool isBalanced)
        {
            this.IsBalanced = isBalanced;
            this.m_createObj = func;
        }

        public void Recycle(T obj)
        {
            if (obj is IPooledObject)
            {
                ((IPooledObject)((object)obj)).Cleanup();
            }
            if (this.m_hardReferences >= this.m_minSize)
            {
                this.m_queue.Enqueue(new WeakReference(obj));
            }
            else
            {
                this.m_queue.Enqueue(obj);
                Interlocked.Increment(ref this.m_hardReferences);
            }
            if (this.m_isBalanced)
            {
                this.OnRecycle();
            }
        }

        public void Recycle(object obj)
        {
            if (obj is T)
            {
                if (obj is IPooledObject)
                {
                    ((IPooledObject)obj).Cleanup();
                }
                if (this.m_hardReferences >= this.m_minSize)
                {
                    this.m_queue.Enqueue(new WeakReference(obj));
                }
                else
                {
                    this.m_queue.Enqueue(obj);
                    Interlocked.Increment(ref this.m_hardReferences);
                }
                if (this.m_isBalanced)
                {
                    this.OnRecycle();
                }
            }
        }

        private void OnRecycle()
        {
            if (Interlocked.Decrement(ref this.m_obtainedReferenceCount) < 0)
            {
                throw new InvalidOperationException("Objects in Pool have been recycled too often: " + this);
            }
        }

        public T Obtain()
        {
            if (this.m_isBalanced)
            {
                Interlocked.Increment(ref this.m_obtainedReferenceCount);
            }
            object obj;
            T result;
            while (this.m_queue.TryDequeue(out obj))
            {
                if (obj is WeakReference)
                {
                    object target = ((WeakReference)obj).Target;
                    if (target == null)
                    {
                        continue;
                    }
                    result = (target as T);
                }
                else
                {
                    Interlocked.Decrement(ref this.m_hardReferences);
                    result = (obj as T);
                }
                return result;
            }
            result = this.m_createObj();
            return result;
        }

        public object ObtainObj()
        {
            if (this.m_isBalanced)
            {
                Interlocked.Increment(ref this.m_obtainedReferenceCount);
            }
            object obj;
            object result;
            while (this.m_queue.TryDequeue(out obj))
            {
                WeakReference weakReference = obj as WeakReference;
                if (weakReference != null)
                {
                    object target = weakReference.Target;
                    if (target == null)
                    {
                        continue;
                    }
                    result = target;
                }
                else
                {
                    Interlocked.Decrement(ref this.m_hardReferences);
                    result = obj;
                }
                return result;
            }
            result = this.m_createObj();
            return result;
        }

        public override string ToString()
        {
            return base.GetType().Name + " for " + typeof(T).FullName;
        }
    }
}
