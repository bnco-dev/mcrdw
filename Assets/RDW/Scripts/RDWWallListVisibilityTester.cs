using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RDWWallListVisibilityTester : ARDWVisibilityTester {

    [System.Serializable]
    public class Wall {
        public Vector3 a;
        public Vector3 b;
    }

    public enum Plane {
        XZ,
        XY,
        YZ
    }

    public List<Wall> walls;
    public Plane walkPlane;

    public override bool Visible (Vector3 a, Vector3 b) {

        var ap = ToPlanar(a);
        var bp = ToPlanar(b);

        for (int i = 0; i < walls.Count; i++) {
            var wap = ToPlanar(walls[i].a);
            var wbp = ToPlanar(walls[i].b);
            if (CollideLineSegmentWithLineSegment(ap,bp,wap,wbp,out _)) {
                return false;
            }
        }
        return true;
    }

    Vector2 ToPlanar (Vector3 v) {
        switch (walkPlane) {
            case Plane.XZ : return new Vector2(v.x,v.z);
            case Plane.XY : return new Vector2(v.x,v.y);
            case Plane.YZ : return new Vector2(v.y,v.z);
        }
        return Vector2.zero;
    }

    bool CollideLineSegmentWithLineSegment (Vector2 line00, Vector2 line01,
        Vector2 line10, Vector2 line11, out Vector2 hit) {

        // https://stackoverflow.com/a/1968345
        Vector2 s0 = line01 - line00;
        Vector2 s1 = line11 - line10;

        float denom = -s1.x * s0.y + s0.x * s1.y;
        float s = (-s0.y * (line00.x - line10.x) + s0.x * (line00.y - line10.y)) / denom;
        float t = ( s1.x * (line00.y - line10.y) - s1.y * (line00.x - line10.x)) / denom;

        if (s >= 0 && s <= 1 && t >= 0 && t <= 1) {

            hit = line00 + (t * s0);
            return true;
        }

        hit = Vector2.zero;
        return false;
    }
}