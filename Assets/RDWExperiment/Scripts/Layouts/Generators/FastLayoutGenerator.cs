using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using System.Threading.Tasks;

namespace RDWMCExperiment {
public class FastLayoutGenerator : ALayoutGenerator {

    public Vector2Int dimensions = new Vector2Int(5, 5);
    public float edgeFactor = 0.5f;
    public float nodeSpacingMeters = 1.0f;
    public float pathLengthMeters = 10.0f;

    public override string GenerateParamsString () {
        return string.Format(
            "generator: Fast,\n" +
            "dimensions: {0},\n" +
            "edge factor: {1},\n" +
            "node spacing (meters): {2},\n" +
            "path length (meters): {3}",
            dimensions,
            edgeFactor,
            nodeSpacingMeters,
            pathLengthMeters);
    }

    public override IEnumerator Generate (List<Layout> layouts,
        System.Random masterRandom, int count) {
        var generator = new FastLayoutGeneratorAlgo();
        generator.Set(dimensions.x,dimensions.y,edgeFactor,nodeSpacingMeters,pathLengthMeters);
        for (int i = 0; i < count; i++) {
            layouts.Add(generator.Generate());
        }
        yield break;
    }

    public override float GetProgress() {
        return 0.0f;
    }

    public override bool InProgress() {
        return false;
    }

    public override void Cancel () { }

    // public override Task GenerateAsync(List<Layout> layouts) {
    //     Generate(layouts);
    //     return Task.CompletedTask;
    // }

}
}