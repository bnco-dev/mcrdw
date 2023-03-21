using System.Collections;
using System.Collections.Generic;

namespace RDW.Collections {

public interface IPoolable {
    void Reset ();
}

public class Pool <T> where T : RDW.Collections.IPoolable, new() {

    public int count {get {return _freeList.Count; }}

    List<T> _freeList = new List<T>();

    public void Clear () {
        _freeList.Clear();
    }

    public T Obtain () {
        if (_freeList.Count > 0) {
            var t = _freeList[_freeList.Count-1];
            _freeList.RemoveAt(_freeList.Count-1);
            // No need to reset, t will have been reset before entering pool
            return t;
        } else {
            var t = new T();
            t.Reset();
            return t;
        }
    }

    public void ReturnToPool (T t) {
        t.Reset();
        _freeList.Add(t);
    }
}

}