public static class BlockLoader
{
    public static void Load()
    {
        BlockTextureAtlas standard = new BlockTextureAtlas {
            CellSize=8,
            TexWidth=256,
            TexHeight=256
        };

        createBasic("dirt", standard, 0, 0);
        createBasic("grass", standard, 1, 0);
        createBasic("stone", standard, 0, 1);
        createBasic("sand", standard, 1, 1);
    }

    private static void createBasic(string name, BlockTextureAtlas atlas, int x, int y)
    {
        Block b = new Block {
            Name=name,
            TextureInfo=atlas.Sample(x,y)
        };
        BlockTypes.CreateBlockType(name, () => b);
    }
}