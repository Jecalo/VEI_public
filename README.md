# VEI
Voxel terrain generator for Unity using the Surface Nets algorithm, powered by the Burst compiler and the Jobs system.

Most time heavy functions have been optimized for general use cases, using multithreading whenever possible, efficient algorithms, and memory pools.

### Surface Nets
Surface nets is a dual contouring algorithm that generates a quad mesh from an SDF (signed distance field). A simple linear interpolation is used, which avoids the use of Hermite data or any relaxation/smoothing steps. This makes procedurally generating and working with the SDF much simpler and faster, while only losing precision on sharp terrain features.

The algorithm was modified to use custom lookup tables of precomputed data to speed up the meshing. Given the state (solid or not) of 8 adjacent voxels, a 1 byte mask is used to obtain precomputed edge count, which faces were completed by a vertex, and which edges are taken into account when interpolating.


After all faces of a chunk are generated, the quads are ordered by material using an in place sort that takes advantage of the fixed amount of materials and the count of each material's faces.


https://github.com/Jecalo/VEI_public/assets/9933392/69202e21-9b74-42b2-9425-f1af3f0e03c6


https://github.com/Jecalo/VEI_public/assets/9933392/06335b10-c303-40f0-aca2-fe9b5f0d202e


#### Chunks
Terrain data is split into chunks. Both the chunk size and resolution (amount of voxels per chunk) can easily be changed, although it is not recommended to change resolution, as it can affect cache performance.

The algorithm places vertices in a cube formed by 8 adjacent voxel points. At a chunk boundary, a face can only be connected if the algorithm knows where the next vertex is located. This means that at least 2 voxel points must overlap, allowing one chunk to take "ownership" of the connecting face and generating it.

This makes chunk data not naively continuous. In order to efficiently maintain this duplicate data seam, all changes to the terrain are computed as one continuous kernel (independent of actual chunk boundaries). From this kernel, a quick computation generates kernel slices that contain the range within the kernel which overlaps each affected chunk.

![](https://github.com/Jecalo/VEI_public/blob/main/Media/images/suzanne_size32.png)
![](https://github.com/Jecalo/VEI_public/blob/main/Media/images/suzanne_size16.png)
![](https://github.com/Jecalo/VEI_public/blob/main/Media/images/suzanne_size8.png)


https://github.com/Jecalo/VEI_public/assets/9933392/7b7c3b7d-ed84-4a14-9699-8ff861a2cb5c


#### Voxel interfacing
Material hardness can be included as a custom kernel applying job, which would limit what a single change (such as an explosion) can destroy. This custom job could also easily count how many voxels were destroyed, allowing the player to gather resources. Each job works with a single chunk, so no extra synchronization is necessary.

Aside from standard voxel destruction and voxel creation, other simple but powerful SDF operations are available:
- Shaped destruction.
- Full shape swap (all voxels in the kernel are swapped).
- Material replacement (targeted or not).
- Shape smoothing.

#### Materials
Generating voxel meshes with (stable) UVs is a complex and slow task. Shading is usually done with other techniques, such as matcaps, triplanar projection, tiled 3D textures or fully procedural textures.

By default, this project generates meshes that are split into submeshes with different Unity materials. This is the most complex case, as other shading techniques only need the basic vertex data (position and normal), or just require extra parameters per vertex (generated while meshing). A simpler, faster version that does not split meshes into submeshes is included, but is not actually used.


https://github.com/Jecalo/VEI_public/assets/9933392/26607037-c570-4fd3-951b-08c8ced097fd


#### Transparent materials (unfinished)
The transparent material system is unfinished. It allows solid transparent materials (glass) and non solid transparent materials (smoke or liquids). Each voxel boundary must be examined, in order to generate quads facing the right direction for any given combination of adjacent voxels. This requires several modifications like vertex interpolation that remains stable for different signed values, overlapped quad duplication for boundaries between transparent materials and determination of quad facing based on the boundary materials.

Submeshes would need to be split into separate meshes, so that liquid or gaseous materials could be baked into different physics meshes for game logic.

#### Mesh to SDF
Having hand crafted voxel structures is an important part of procedural world generation. But handcrafting voxel arrays directly can be an annoying and slow task. Instead, simple meshes can be turned into voxels.

Transforming a given mesh to an SDF (which can then be turned back into a mesh as part of the terrain) is not a trivial problem. For this purpose, a mesh is first processed and turned into an intermediate data structure (BVH with extra information about normals). This data structure can be queried for the distance from any given point to the closest point on the surface of the mesh. It also outputs the sign (whether this point is contained in the mesh).

An SDF can then be built from this data structure. If the mesh is going the be used in the voxel terrain repeatedly, it can also be saved in the intermediate state or directly as a voxel kernel. This can be done either at runtime or in an asset file.

Automation of this process can be prepared so that changes to meshes are automatically built into assets, splitting the mesh into different parts in order to bake different materials into one kernel.


https://github.com/Jecalo/VEI_public/assets/9933392/ab7edc15-c224-4902-8a13-8c67486a9400


#### Performance
The voxel terrain generation has been optimized, being capable of meshing a typical chunk in less than 1.0ms. Each CPU core can work with a different chunk at the same time, although cache and memory limits makes this performance not proportional to the core count.

Some benchmarks have been done, such as trying reduced per-voxel size (5 to 2 bytes). Nevertheless, optimization of this kind of algorithm cannot be done as a discrete problem. Benchmarks must be done with the whole game, including its scope and specific features. White room optimization usually has limited use.

The physics baking (using the default unity physics) can take a large amount of time for complex chunks. Depending on the procedural terrain generation, noise calculations can also take a significant amount of time. Possible solutions to these performance concerns are discussed furhter below.


### Custom Gizmos
Unity's gizmos system provides a nice interface for rendering debugging and helper gizmos. Unfortunately, it only works in development builds. It also must be called each frame from the OnDrawGizmos method.

A custom implementation using Renderer Features for the Scriptable Rendering Pipeline is included, along with a simple wireframe view. Both work as Renderer Features, which are rendered after the scene. They are not dependent on any GameObject.

A simple macro could be used to strip this logic from final builds, making any call to a gizmo drawing that does nothing be factored out by the compiler.


https://github.com/Jecalo/VEI_public/assets/9933392/e0b9c9ed-5a5f-4592-95bb-2a421c52a884


### Custom Console
Custom ingame console, which can be easily extended by adding functions to a script. These are turned into commands at the start of the game using the C# reflection system.

The console includes automatic help commands and autocompletion. More complex commands are also available, such as storing (and modifying) the return object of a command on a variable, or nesting commands (using the output of one command as the argument for another).

Parsing and autocompletion are still a work in progress, as the valid syntax has not been decided yet.


https://github.com/Jecalo/VEI_public/assets/9933392/c065ccb7-f5fc-4e1a-810b-0a2b1168648a


https://github.com/Jecalo/VEI_public/assets/9933392/93d521e4-73c2-4b2b-af6e-b764fca99a56


### Noise
Perlin noise generation is implemented, both for 2D and 3D. It uses a slower (and higher quality) hash for computing directions, but is also vectorized to offset the performance hit.

For most use cases this extra noise quality is probably not necessary. Further benchmarking would be necessary for each specific application or game.


https://github.com/Jecalo/VEI_public/assets/9933392/ff4b47df-a524-49e7-8dc6-bc7a297d1188


### Pathfinding (Unfinished)
Enabling efficient pathfinding on a large voxel terrain for different agent sizes and capabilities is very hard task that requires planning, optimization, and preprocessing of terrain.

Some very basic algorithms and data structures for pathfinding are implemented. HAA\* and similar algorithms could be used, in addition to other techniques such as extra path pruning, splitting pathfinding into chunks or splitting paths into different steps.

### Improvements
- Manual drawing of meshes using the scriptable rendering pipeline. This would avoid a large cost of Unity C# and C++ core communication caused by the creation and update of a large amount of meshes and gameobjects. Each chunk can be uploaded directly to the GPU, culled using chunk specific algorithms, and rendered as a large amount of data. Managing buffers manually would also improve rendering speed.
- Chunk gameobjects and meshes are necessary for the unity physics engine. A relatively simple physics system for small voxel sizes (where the interpolation can be ignored) could be implemented, as voxels are usually axis aligned. Finer collision detection can then be used as needed.
- Chunk data can be turned into an octree, which would reduce memory size and possibly improve cache performance. This would make the algorithm itself slightly slower.
- Basic LODs can be added with the stiching technique. Chunks that are far away can be rendered skipping every other voxel (halving resolution), and then boundary chunks between different LODs can stiched so they have no holes. Chunk data could also be compressed with a smarter algorithm for each LOD.
- Generation of procedural terrain on the GPU has stricter limits to its logic, but noise is generated much faster. Generation can also be split, so that CPU does some processing on the terrain after the bulk of generation has been handled by the GPU.
- Finally, raymarching/raytracing is improving quickly (along with hardware performance). Skipping meshing altogether is probably a great option.

### References
- Constrained Elastic SurfaceNets: Generating Smooth Models from Binary Segmented Data [Gibson, 1998]
- Dual Contouring of Hermite Data [Siggraph 2002]
- Generating Signed Distance Fields From Triangle Meshes [Andreas Bærentzen, Henrik Aanæs, 2002]
- https://catlikecoding.com/unity/tutorials/pseudorandom-noise/
- https://github.com/Cyan4973/xxHash
- Hierarchical Path Planning for Multi-Size Agents in Heterogeneous Environments [Harabor, Botea,  2008]
- https://web.archive.org/web/20190411040123/http://aigamedev.com/open/article/clearance-based-pathfinding/
