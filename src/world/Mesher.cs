using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Mesher : Node
{
    [Export]
    public Material ChunkMaterial;
    [Export]
    public float MaxMeshTime = 0.1f;
    public static Mesher Singleton;
    private HashSet<Chunk> toMesh = new HashSet<Chunk>();
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Singleton = this;
        BlockLoader.Load();
        WorldGenerator wg = new WorldGenerator();
        wg.Generate(World.Singleton);
        MeshAll();
    }
    public override void _Process(float delta)
    {
        //empty mesh queue
        foreach(var c in toMesh) {
            if (c == null) continue; //not sure if this is needed
            Chunk meshing = c; //copy pointer to avoid race condition
            Task.Run(() => MeshChunk(meshing));
        }
        toMesh.Clear();
        base._Process(delta);
    }
    public void MeshAll()
    {
        foreach (var kvp in World.Singleton.Chunks) {
           MeshDeferred(kvp.Value); 
        }
    }
    public void MeshDeferred(Chunk chunk) {
        toMesh.Add(chunk);
    }
    private void MeshChunk(Chunk chunk) {
        var mesh = GenerateMesh(chunk);
        if (chunk.Mesh != null) {
            chunk.Mesh.QueueFree();
            chunk.Mesh = null;
        }
        chunk.Mesh = mesh;
        if (mesh != null)
        {
            GetParent().CallDeferred("add_child", mesh);
        }
    }
    private MeshInstance GenerateMesh(Chunk chunk)
    {
        if (chunk == null)
        {
            return null;
        }
        var mesh = new MeshInstance();
        var arrMesh = new ArrayMesh();

        Array array = new Array();
        array.Resize((int)ArrayMesh.ArrayType.Max);
        var vertices = new List<Vector3>();
        var tris = new List<int>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();

        Chunk[] neighbors = new Chunk[6]; //we are in 3d
        neighbors[(int)Direction.PosX] = World.Singleton.GetChunk(chunk.Position + new ChunkCoord(1,0,0));
        neighbors[(int)Direction.PosY] = World.Singleton.GetChunk(chunk.Position + new ChunkCoord(0,1,0));
        neighbors[(int)Direction.PosZ] = World.Singleton.GetChunk(chunk.Position + new ChunkCoord(0,0,1));
        neighbors[(int)Direction.NegX] = World.Singleton.GetChunk(chunk.Position + new ChunkCoord(-1,0,0));
        neighbors[(int)Direction.NegY] = World.Singleton.GetChunk(chunk.Position + new ChunkCoord(0,-1,0));
        neighbors[(int)Direction.NegZ] = World.Singleton.GetChunk(chunk.Position + new ChunkCoord(0,0,-1));

        //generate the mesh
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
            {
                for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                {
                    if (chunk[x,y,z] == null) continue;
                    BlockTextureInfo tex = chunk[x,y,z].TextureInfo;
                    meshBlock(chunk, neighbors, new BlockCoord(x,y,z), tex, vertices, uvs, normals, tris);
                }
            }
        }

        if (vertices.Count==0) return null;
        array[(int)ArrayMesh.ArrayType.Vertex] = vertices;
        array[(int)ArrayMesh.ArrayType.Index] = tris;
        array[(int)ArrayMesh.ArrayType.Normal] = normals;
        array[(int)ArrayMesh.ArrayType.TexUv] = uvs;
        arrMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, array);

        mesh.Mesh = arrMesh;
        mesh.SetSurfaceMaterial(0, ChunkMaterial);
        return mesh;
    }
    private void meshBlock(Chunk chunk, Chunk[] neighbors, BlockCoord localPos, BlockTextureInfo tex, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        bool nonOpaque(Chunk c, int x, int y, int z) => c == null || c[x,y,z] == null || c[x,y,z].Transparent;
        Vector3 pos = (Vector3)chunk.LocalToWorld(localPos);

        //check if there's no block/a transparent block in each direction. only generate face if so.
        if (localPos.x == 0 && nonOpaque(neighbors[(int)Direction.NegX], Chunk.CHUNK_SIZE-1,localPos.y,localPos.z) || localPos.x != 0 && nonOpaque(chunk,localPos.x-1,localPos.y,localPos.z)) {
            addFacePosX(pos, tex, verts, uvs, normals, tris);
        }
        if (localPos.y == 0 && nonOpaque(neighbors[(int)Direction.NegY], localPos.x, Chunk.CHUNK_SIZE-1,localPos.z) || localPos.y != 0 && nonOpaque(chunk,localPos.x,localPos.y-1,localPos.z)) {
            addFaceNegY(pos, tex, verts, uvs, normals, tris);
        }
        if (localPos.z == 0 && nonOpaque(neighbors[(int)Direction.NegZ], localPos.x,localPos.y,Chunk.CHUNK_SIZE-1) || localPos.z != 0 && nonOpaque(chunk,localPos.x,localPos.y,localPos.z-1)) {
            addFacePosZ(pos, tex, verts, uvs, normals, tris);
        }
        if (localPos.x == Chunk.CHUNK_SIZE-1 && nonOpaque(neighbors[(int)Direction.PosX], 0,localPos.y,localPos.z) || localPos.x != Chunk.CHUNK_SIZE-1 && nonOpaque(chunk,localPos.x+1,localPos.y,localPos.z)) {
            addFaceNegX(pos, tex, verts, uvs, normals, tris);
        }
        if (localPos.y == Chunk.CHUNK_SIZE-1 && nonOpaque(neighbors[(int)Direction.PosY], localPos.x,0,localPos.z) || localPos.y != Chunk.CHUNK_SIZE-1 && nonOpaque(chunk,localPos.x,localPos.y+1,localPos.z)) {
            addFacePosY(pos, tex, verts, uvs, normals, tris);
        }
        if (localPos.z == Chunk.CHUNK_SIZE-1 && nonOpaque(neighbors[(int)Direction.PosZ], localPos.x,localPos.y,0) || localPos.z != Chunk.CHUNK_SIZE-1 && nonOpaque(chunk,localPos.x,localPos.y,localPos.z+1)) {
            addFaceNegZ(pos, tex, verts, uvs, normals, tris);
        }
    }
    private void finishFace(BlockTextureInfo info, Vector3 normalDir, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        int faceId = normals.Count / 4;
        uvs.Add(info.UVMin);
        uvs.Add(new Vector2(info.UVMax.x, info.UVMin.y));
        uvs.Add(info.UVMax);
        uvs.Add(new Vector2(info.UVMin.x, info.UVMax.y));
        normals.Add(normalDir);
        normals.Add(normalDir);
        normals.Add(normalDir);
        normals.Add(normalDir);
        tris.Add(faceId * 4 + 2);
        tris.Add(faceId * 4 + 1);
        tris.Add(faceId * 4);
        tris.Add(faceId * 4);
        tris.Add(faceId * 4 + 3);
        tris.Add(faceId * 4 + 2);
    }
    //facing the +z direction
    private void addFacePosZ(Vector3 origin, BlockTextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 1, 0));
        verts.Add(origin + new Vector3(1, 1, 0));
        verts.Add(origin + new Vector3(1, 0, 0));
        verts.Add(origin + new Vector3(0, 0, 0));
        finishFace(texInfo, new Vector3(0, 0, 1), uvs, normals, tris);
    }
    //facing the -z direction
    private void addFaceNegZ(Vector3 origin, BlockTextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 0, 1));
        verts.Add(origin + new Vector3(1, 0, 1));
        verts.Add(origin + new Vector3(1, 1, 1));
        verts.Add(origin + new Vector3(0, 1, 1));
        finishFace(texInfo, new Vector3(0, 0, -1), uvs, normals, tris);
    }
    //facing the +x direction
    private void addFacePosX(Vector3 origin, BlockTextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 0, 1));
        verts.Add(origin + new Vector3(0, 1, 1));
        verts.Add(origin + new Vector3(0, 1, 0));
        verts.Add(origin + new Vector3(0, 0, 0));
        finishFace(texInfo, new Vector3(1, 0, 0), uvs, normals, tris);
    }
    //facing the -x direction
    private void addFaceNegX(Vector3 origin, BlockTextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(1, 0, 0));
        verts.Add(origin + new Vector3(1, 1, 0));
        verts.Add(origin + new Vector3(1, 1, 1));
        verts.Add(origin + new Vector3(1, 0, 1));
        finishFace(texInfo, new Vector3(-1, 0, 0), uvs, normals, tris);
    }
    //facing the +y direction
    private void addFacePosY(Vector3 origin, BlockTextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 1, 0));
        verts.Add(origin + new Vector3(0, 1, 1));
        verts.Add(origin + new Vector3(1, 1, 1));
        verts.Add(origin + new Vector3(1, 1, 0));

        finishFace(texInfo, new Vector3(0, 1, 0), uvs, normals, tris);
    }
    //facing the -y direction
    private void addFaceNegY(Vector3 origin, BlockTextureInfo texInfo, List<Vector3> verts, List<Vector2> uvs, List<Vector3> normals, List<int> tris)
    {
        verts.Add(origin + new Vector3(0, 0, 0));
        verts.Add(origin + new Vector3(1, 0, 0));
        verts.Add(origin + new Vector3(1, 0, 1));
        verts.Add(origin + new Vector3(0, 0, 1));
        finishFace(texInfo, new Vector3(0, -1, 0), uvs, normals, tris);
    }
}
