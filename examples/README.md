# Examples

- `models/model-viewer.obj` is an original, redistributable multi-object fixture. Rebuild its binary with `scripts/build-assets.ps1`.
- `scenes/hierarchy.mmscene` demonstrates stable IDs, parenting, transforms, and reflection-free component creation.
- `art/mm-mark.svg` is the source for the sample icons and alpha-blended PNG mark.

The supplied DualSense OBJ/GLB is intentionally absent: the bootstrap marks it as local reference material only. Convert a model you are entitled to use with `mmengine mesh import` and replace `samples/ModelViewer/assets/model.mmmesh`.
