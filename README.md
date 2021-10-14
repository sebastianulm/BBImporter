# This imports model files  from Blockbench
See https://www.blockbench.net/ \
Package made with https://omiyagames.github.io/template-unity-package/
## Install

Unity's own Package Manager supports [importing packages through a URL to a Git repo](https://docs.unity3d.com/Manual/upm-ui-giturl.html):

1. First, on this repository page, click the "Clone or download" button, and copy over this repository's HTTPS URL.  
2. Then click on the + button on the upper-left-hand corner of the Package Manager, select "Add package from git URL..." on the context menu, then paste this repo's URL!

## LICENSE

Overall package is licensed under [MIT](/LICENSE.md)

## Project Goals
- It's a simple Plugin. It does ONLY imports.
- ONLY for the blockbench 4 beta onward.
- ONLY for generic Models
- ONLY for importing from BBModel files, no GLTF or JSON, etc
- Getting the most out of this plugin has some implications how you organize your files in terms of naming, facing, texture organisation.

## Internals
### Coordinate System
This converts Vectors from OpenGL (Left handed) to Unity (Right handed) when importing.
Accordingly it will also convert euler rotations, by inverting y and z rotations. 
This leads to Models looking the same way. 
Additionally, NORTH in Blockbench will be Vector3.backwards in Unity. THIS MIGHT CHANGE

### Settings
* Material Template -> A copy of this Material will be created and mainTexture set to the texture of the fae
* Combine Meshes -> when true only one Mesh is created, containing the entire file. When false a Mesh per object in the File is created.
* Filter Hidden -> No meshes will be created for file objects that are hidden

### Materials
If no Template Material is set, a new Standard Material will be instantiated and saved in the imported asset. 
For looks, Smoothness and Metallic will be set to 0. If a Texture is present in the model file, a new Texture2D subasset 
is created and set to mainTexture on the Material.
If a Material is supplied, a new copy will be created as subasset, and mainTexture is set.
A Submesh is created for Texture and the vertices are sorted accordingly.
A MeshRenderer is attached to each generated object, supplied with the created Mesh and Material list. 
The Process is very similar to the built in Importers.

### Planned features
- Fixing Bugs first, then moving on to more stuff
- (new Settings) Shared Material, use MaterialPropertyBlock on renderer to set texture
- (Feature) Texture-Deduplication
- (new Setting) Shared material, the generated GOs will have a shared material. Useful for pallete textures
- (Feature) Runtime Script that retains some parsed form of the parsed Model file

### Support
- Forks and PR's welcome, but expect delays
- BugReports should include the File you try to import
- I do this as a side project to my side project's sideproject, attention will be low
- Be civil