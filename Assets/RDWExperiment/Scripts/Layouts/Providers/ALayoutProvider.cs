using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace RDWMCExperiment {
public abstract class ALayoutProvider : MonoBehaviour {
    public abstract void Import (List<Layout> layouts);
    public abstract void ImportAdditive (List<Layout> layouts);
    public abstract string GetCollectionDescriptor ();
    public abstract int GetCollectionLayoutCount ();
}
}