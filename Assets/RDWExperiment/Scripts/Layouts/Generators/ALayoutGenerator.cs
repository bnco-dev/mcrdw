using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace RDWMCExperiment {
public abstract class ALayoutGenerator : MonoBehaviour {
    public abstract string GenerateParamsString ();
    public abstract IEnumerator Generate (List<Layout> layouts,
        System.Random masterRandom, int count);
    public abstract void Cancel ();
    public abstract float GetProgress ();
    public abstract bool InProgress ();
    // public abstract Task GenerateAsync (List<Layout> layouts);
}
}
