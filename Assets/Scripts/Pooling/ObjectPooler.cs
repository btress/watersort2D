using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPooler : MonoBehaviour
{
    [SerializeField] private BottleController _bottlePrefab;
    [SerializeField] private Transform _container;
    [SerializeField] private int _prewarmCount = 10;
    [SerializeField] private int _maxSize = 20;

    private ObjectPool<BottleController> _pool;

    private void Awake()
    {
        if (_container == null) _container = transform;

        _pool = new ObjectPool<BottleController>(
            CreateBottle, OnGetBottle, OnReleaseBottle, OnDestroyBottle,
            true, _prewarmCount, _maxSize);

        Prewarm();
    }

    public BottleController Get()
    {
        return _pool.Get();
    }

    public void Release(BottleController bottle)
    {
        _pool.Release(bottle);
    }

    private void Prewarm()
    {
        List<BottleController> warmed = new List<BottleController>(_prewarmCount);
        for (int i = 0; i < _prewarmCount; i++) warmed.Add(_pool.Get());
        for (int i = 0; i < warmed.Count; i++) _pool.Release(warmed[i]);
    }

    private BottleController CreateBottle()
    {
        return Instantiate(_bottlePrefab, _container);
    }

    private void OnGetBottle(BottleController bottle)
    {
        bottle.gameObject.SetActive(true);
    }

    private void OnReleaseBottle(BottleController bottle)
    {
        bottle.ResetForPool();
        bottle.gameObject.SetActive(false);
    }

    private void OnDestroyBottle(BottleController bottle)
    {
        if (bottle != null) Destroy(bottle.gameObject);
    }
}
