# This imports model files  from Blockbench
See https://www.blockbench.net/ \
Package made with https://omiyagames.github.io/template-unity-package/
## Install

Unity's own Package Manager supports [importing packages through a URL to a Git repo](https://docs.unity3d.com/Manual/upm-ui-giturl.html):

1. First, on this repository page, click the "Clone or download" button, and copy over this repository's HTTPS URL.  
2. Then click on the + button on the upper-left-hand corner of the Package Manager, select "Add package from git URL..." on the context menu, then paste this repo's URL!

## Dependencies: 
* https://github.com/jilleJr/Newtonsoft.Json-for-Unity.git#upm

## Project Goals
- It's a simple Plugin. It does ONLY imports.
- ONLY for the blockbench 5+;
- ONLY for generic Models
- ONLY for importing from BBModel files, no GLTF or JSON, etc
- Getting the most out of this plugin has some implications how you organize your files in terms of naming, facing, texture organisation.

## Internals
### Coordinate System
This converts Vectors from OpenGL (Left handed) to Unity (Right handed) when importing.
Accordingly it will also convert euler rotations, by inverting y and z rotations. 
This leads to Models looking the same way. 
Additionally, NORTH in Blockbench will be Vector3.forward (+Z) in Unity.

### Settings
* Material Template -> A copy of this Material will be created and mainTexture set to the texture of the face
* Import Mode -> Select here how the blockbench file is converted into game objects
* Filter Hidden -> No meshes will be created for file objects that are hidden

### Materials
If no Template Material is set, a new Standard Material will be instantiated and saved in the imported asset. 
For looks, Smoothness and Metallic will be set to 0. If a Texture is present in the model file, a new Texture2D subasset 
is created and set to mainTexture on the Material.
If a Material is supplied, a new copy will be created as subasset, and mainTexture is set.
A Submesh is created for Texture and the vertices are sorted accordingly.
A MeshRenderer is attached to each generated object, supplied with the created Mesh and Material list. 
The Process is very similar to the built in Importers.

### Animations
Needs to be imported as Hierarchy. BlockBench uses Groups
to animate, so this importer creates empty game objects for groups and animates their transforms.
Due to how Unity (and hierarchical models in general) work, transform positions will be different in 
Unity than in Blockbench


### Planned features
- (Feature) Texture-Deduplication
- (Feature) Runtime Script that retains some parsed form of the parsed Model file
- (Feature) Animation bezier if compatible with Unities animation system

### Support
- Forks and PR's welcome, but expect delays
- BugReports should include the File you try to import
- I do this as a side project to my side project's sideproject, attention will be low
- Be civil

## LICENSE
[MIT](/LICENSE.md)
