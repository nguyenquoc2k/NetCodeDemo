using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;
using Random = UnityEngine.Random;


public static class Commons
{
    public static Coroutine SetTimeout(this MonoBehaviour monoBehaviour, Action callback, float delay)
    {
        return monoBehaviour.StartCoroutine(Timeout(delay, callback));
    }

    private static IEnumerator Timeout(float delay, Action callback)
    {
        yield return new WaitForSecondsRealtime(delay);
        callback();
    }

    public static Coroutine SetInterval(this MonoBehaviour monoBehaviour, Action callback, float delay)
    {
        return monoBehaviour.StartCoroutine(Interval(delay, callback));
    }

    private static IEnumerator Interval(float delay, Action callback)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            callback();
        }
    }

    public static Coroutine WaitEndFrame(this MonoBehaviour monoBehaviour, Action callback)
    {
        return monoBehaviour.StartCoroutine(IWaitEndFrame(callback));
    }

    public static Coroutine WaitUntil(this MonoBehaviour monoBehaviour, Action callback, Func<bool> condition)
    {
        return monoBehaviour.StartCoroutine(IWaitUntil(callback, condition));
    }

    static IEnumerator IWaitUntil(Action callback, Func<bool> condition)
    {
        while (!condition())
        {
            yield return null;
        }

        callback();
    }

    static IEnumerator IWaitEndFrame(Action callback)
    {
        yield return new WaitForEndOfFrame();
        callback();
    }


    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}

[Serializable]
public class ValueKeyPair<TKey, TValue>
{
    public TKey Key;
    public TValue Value;

    public ValueKeyPair(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }
}

[Serializable]
public class ValueKeyPair<TKey, T1, T2>
{
    public TKey Key;
    public T1 V1;
    public T2 V2;

    public ValueKeyPair(TKey key, T1 v1, T2 v2)
    {
        Key = key;
        V1 = v1;
        V2 = v2;
    }
}

[Serializable]
public class SerializedDictionary<TKey, TValue> : ISerializationCallbackReceiver
{
    [NonSerialized] Dictionary<TKey, TValue> map = new Dictionary<TKey, TValue>();
    [SerializeField] List<ValueKeyPair<TKey, TValue>> data = new List<ValueKeyPair<TKey, TValue>>();


    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        map.Clear();
        for (int i = 0; i < this.data.Count; i++)
        {
            var current = data[i];
            if (map.ContainsKey(current.Key))
            {
                map[current.Key] = current.Value;
            }
            else map.Add(current.Key, current.Value);
        }
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
        //data.Clear();

        //foreach (var item in map)
        //{
        //    data.Add(new ValueKeyPair<TKey, TValue>(item.Key, item.Value));
        //}
    }

    public TValue this[TKey key]
    {
        get { return map[key]; }
        set { map[key] = value; }
    }

    public int Count => map.Count;
}

[Serializable]
public struct NestedArray<T>
{
    public T[] List;

    public T this[int index]
    {
        get => List[index];
    }
}

[Serializable]
public struct NestedList<T>
{
    public List<T> List;

    public T this[int index]
    {
        get => List[index];
    }

    public int Count => List.Count;

    public IEnumerator<T> GetEnumerator()
    {
        return List.GetEnumerator();
    }
}