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

        /// <summary> Create a new object pool. </summary>
        public ObjectPool()
        {
            inactiveObjects = new();
            activeObjects = new();
        }

        /// <summary> Create a new object pool with starting count of objects. </summary>
        /// <param name="startCount"> Number of objetcs to create. </param>
        /// <param name="create"> Function for creating objects. </param>
        public ObjectPool(int startCount, Func<T> create)
        {
            inactiveObjects = new();
            activeObjects = new(startCount);

            Generate(startCount, create);
        }

        /// <summary> A copy of the active objects collection. </summary>
        public T[] ActiveObjects => activeObjects.ToArray();

        /// <summary> Generate a count of objects. </summary>
        /// <param name="count"> Number of objects to create. </param>
        /// <param name="create"> Function for creating objects. </param>
        public void Generate(int count, Func<T> create)
        {
            for (int i = 0; i < count; i++)
            {
                activeObjects.Add(create.Invoke());
            }
        }

        /// <summary> Retrieve an object from the pool. Creates a new object if needed. </summary>
        /// <param name="create"> Function for creating objects. </param>
        public T Retrieve(Func<T> create)
        {
            var obj = inactiveObjects.TryPop(out var poppedObj)
                ? poppedObj
                : create.Invoke();

            activeObjects.Add(obj);

            return obj;
        }

        /// <summary> Returns an object to the pool. </summary>
        /// <param name="obj"> The object to return. </param>
        public void Return(T obj)
        {
            if (activeObjects.Remove(obj))
            {
                inactiveObjects.Push(obj);
            }
            else
            {
                Debug.LogError($"Object Pool of type {typeof(T).FullName} tried to return an object it didn't contain!", obj is UnityEngine.Object uObj ? uObj : null);
            }
        }

        /// <summary> Return all objects to the pool. </summary>
        public void ReturnAll()
        {
            foreach (var obj in activeObjects)
            {
                inactiveObjects.Push(obj);
            }

            activeObjects.Clear();
        }

        /// <summary> Retrieve all objects from the pool. </summary>
        public T[] RetrieveAll()
        {
            while (inactiveObjects.Count > 0)
            {
                activeObjects.Add(inactiveObjects.Pop());
            }

            return activeObjects.ToArray();
        }

        /// <summary> Clears all the items from the pool. </summary>
        /// <param name="action"> An optional delegate to run on all the objects before they are cleared. </param>
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

        /// <summary> Add an object created outside of the pool. </summary>
        /// <param name="obj"> The object to add. </param>
        /// <param name="active"> Whether to add the object as active or inactive. </param>
        public void AddNew(T obj, bool active)
        {
            if (active)
            {
                activeObjects.Add(obj);
            }
            else
            {
                inactiveObjects.Push(obj);
            }
        }

        /// <summary> Remove an object from the pool. <para> This empties and refills the inactive objects stack. </para></summary>
        /// <param name="obj"> The object to remove. </param>
        public void Remove(T obj)
        {
            if (activeObjects.Remove(obj))
            {
                return;
            }

            var inactiveObjectsList = new List<T>(inactiveObjects);
            inactiveObjectsList.Remove(obj);

            inactiveObjects.Clear();
            foreach (var inactiveObj in inactiveObjectsList)
            {
                inactiveObjects.Push(inactiveObj);
            }
        }
    }
}
