using System;

namespace Laba3
{
    public struct Vertex
    {
        public float X, Y, Z;
        public Vertex(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    public struct Triangle
    {
        public int V1, V2, V3;
        public Triangle(int v1, int v2, int v3) { V1 = v1; V2 = v2; V3 = v3; }
    }
}
