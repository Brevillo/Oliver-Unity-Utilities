using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

namespace OliverBeebe.UnityUtilities.Runtime.Pooling
{
    public class Poolable : MonoBehaviour
    {
        [SerializeField] private UnityEvent<Poolable> returned;
        public event Action<Poolable> Returned;
        [Space]
        [SerializeField] private UnityEvent<Poolable> retrieved;
        public event Action<Poolable> Retrieved;

        public void Return()
        {
            Returned?.Invoke(this);
            returned.Invoke(this);
        }

        internal void Retrieve()
        {
            Retrieved?.Invoke(this);
            retrieved.Invoke(this);
        }
    }
}
