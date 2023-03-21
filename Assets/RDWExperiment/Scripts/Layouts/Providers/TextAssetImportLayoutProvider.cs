using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace RDWMCExperiment {
public class TextAssetImportLayoutProvider : ALayoutProvider {

    public TextAsset collectionDesc;
    public List<TextAsset> assets;

    public override string GetCollectionDescriptor () {
        return collectionDesc.text;
    }

    public override int GetCollectionLayoutCount() {
        return assets.Count;
    }

    public override void ImportAdditive(List<Layout> layouts) {
        if (assets == null) {
            Debug.LogWarning("Layout provider could not find text assets");
            return;
        }

        for (int i = 0; i < assets.Count; i++) {
            layouts.Add(LayoutUtil.ImportFromText(assets[i].text));
        }
    }

    public override void Import(List<Layout> layouts) {
        layouts.Clear();
        ImportAdditive(layouts);
    }

}
}