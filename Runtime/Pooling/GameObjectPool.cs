using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OliverBeebe.UnityUtilities.Runtime.Pooling
{
    [CreateAssetMenu(menuName = "Oliver Utilities/Pooling/GameObject Pool")]
    public class GameObjectPool : ScriptableObjectPool<Poolable>
    {
        [SerializeField] private Poolable prefab;
        [SerializeField] private bool setActiveOnRetrieval;

        private static Transform allParent;
        private Transform heirarchyParent;

        protected virtual Transform SpawnHeirarchyParent() => new GameObject().transform;

        private void Initialize()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (from, to) => Clear(); 

            if (allParent == null)
            {
                allParent = new GameObject("GameObject Pools").transform;
            }

            heirarchyParent = SpawnHeirarchyParent();
            heirarchyParent.parent = allParent;
            heirarchyParent.name = name;
        }

        protected override Poolable Create()
        {
            if (heirarchyParent == null)
            {
                Initialize();
            }

            return Instantiate(prefab, heirarchyParent);
        }

        protected override void Destroy(Poolable poolable)
        {
            if (poolable != null && poolable.gameObject != null)
            {
                Destroy(poolable.gameObject);
            }
        }

        public override Poolable Retrieve()
        {
            var poolable = base.Retrieve();

            poolable.Retrieve();
            poolable.Returned += OnReturned;

            if (setActiveOnRetrieval)
            {
                poolable.gameObject.SetActive(true);
            }

            return poolable;
        }

        private void OnReturned(Poolable poolable)
        {
            poolable.Returned -= OnReturned;

            if (setActiveOnRetrieval)
            {
                poolable.gameObject.SetActive(false);
            }

            base.Return(poolable);
        }

        public override void Return(Poolable poolable)
        {
            poolable.Return();
        }
    }
}
