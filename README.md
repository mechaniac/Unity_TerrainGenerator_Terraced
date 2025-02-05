# Unity_TerrainGenerator_Terraced (WiP) including Biome Generator
smooth slopes / sharp cliffs

A Terrain Generator based on pixel height maps. One tile is represented by one pixel.

Draw a (power of 2 sized) texture with brightness information. 
### Terrain Generator (Component):
- width per pixel (simple multiplier)
- height multiplier (base values are: black pixel is height 0, white pixel height 1)
- MAX SLOPE HEIGHT: Map stores height difference between tiles(pixels). If height difference between neighbours is bigger = cliff.
    
### Chunk Generator (Component):
- creates the meshes. define chunk size (tyles per chunk)

height from pixelimage.
cliffs from maxDistanceThreshold (Max Slope Height)




![alt text](https://github.com/mechaniac/Unity_TerrainGenerator_Terraced/blob/master/documentation/Screenshot_02.jpg?raw=true)

latest commit:
![alt text](https://github.com/mechaniac/Unity_TerrainGenerator_Terraced/blob/master/documentation/Screenshot_03.jpg?raw=true)
