using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace OliverBeebe.UnityUtilities.Runtime.Pooling
{
    public class ObjectPool<T>
    {
        private readonly Stack<T> inactiveObjects;
        private readonly List<T> activeObjects;

        public ObjectPool()
        {
            inactiveObjects = new();
            activeObjects = new();
        }

        public ObjectPool(int startCount, Func<T> create)
        {
            inactiveObjects = new();
            activeObjects = new(startCount);

            Generate(startCount, create);
        }

        public T[] ActiveObjects => activeObjects.ToArray();

        public void Generate(int count, Func<T> create)
        {
            for (int i = 0; i < count; i++)
            {
                activeObjects.Add(create.Invoke());
            }
        }

        public T Retrieve(Func<T> create)
        {
            var obj = inactiveObjects.TryPop(out var poppedObj)
                ? poppedObj
                : create.Invoke();

            activeObjects.Add(obj);

            return obj;
        }

        public void Return(T obj)
        {
            if (activeObjects.Remove(obj))
            {
                inactiveObjects.Push(obj);
            }
            else
            {
                Debug.LogError($"Object Pool of type {typeof(T).FullName} tried to remove object it didn't contain!", obj is UnityEngine.Object uObj ? uObj : null);
            }
        }

        public void Clear(Action<T> action = null)
        {
            if (action != null)
            {
                foreach (var obj in inactiveObjects)
                {
                    action.Invoke(obj);
                }

                foreach (var obj in activeObjects)
                {
                    action.Invoke(obj);
                }
            }

            inactiveObjects.Clear();
            activeObjects.Clear();
        }
    }
}
