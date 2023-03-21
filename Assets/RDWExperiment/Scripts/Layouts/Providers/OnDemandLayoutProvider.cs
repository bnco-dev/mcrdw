using System.Collections.Generic;

namespace RDWMCExperiment {
public class OnDemandLayoutProvider : ALayoutProvider {

    public ALayoutGenerator generator;
    public int count = 1;

    public override string GetCollectionDescriptor () {
        return "On Demand";
    }

    public override int GetCollectionLayoutCount() {
        return count;
    }

    public override void ImportAdditive(List<Layout> layouts) {
        generator.Generate(layouts, new System.Random(),count);
    }

    public override void Import(List<Layout> layouts) {
        layouts.Clear();
        ImportAdditive(layouts);
    }
}
}