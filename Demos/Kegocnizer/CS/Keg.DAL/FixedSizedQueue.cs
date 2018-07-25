using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keg.DAL
{
    public class FixedSizedQueue<T> : IReadOnlyCollection<T>
    {
        private object LOCK = new object();
        ConcurrentQueue<T> queue;
        private int _count;

        public int MaxSize { get; set; }

        public int Count { get { return _count; } }

        public FixedSizedQueue(int maxSize, IEnumerable<T> items = null)
        {
            this.MaxSize = maxSize;
            if (items == null)
            {
                queue = new ConcurrentQueue<T>();
            }
            else
            {
                queue = new ConcurrentQueue<T>(items);
                EnsureLimitConstraint();
            }
        }

        public void Enqueue(T obj)
        {
            queue.Enqueue(obj);
            _count++;
            EnsureLimitConstraint();
        }

        private void EnsureLimitConstraint()
        {
            if (queue.Count > MaxSize)
            {
                lock (LOCK)
                {
                    T overflow;
                    while (queue.Count > MaxSize)
                    {
                        queue.TryDequeue(out overflow);
                        _count--;
                    }
                }
            }
        }

        public T[] ToArray()
        {
            return queue.ToArray();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return queue.GetEnumerator();
        }
    }
}
