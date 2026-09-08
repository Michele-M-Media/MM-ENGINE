# Third-party notices

MM ENGINE is an independent community project. It is not affiliated with or endorsed by Sony Interactive Entertainment.

## SharpProspero 0.8

MM ENGINE source projects reference an external SharpProspero checkout through `SHARPPROSPERO_ROOT`. The supplied SharpProspero project declares `GPL-3.0-or-later`. `MultiMeshRenderer3D` adapts a source-verified state/link/draw sequence from SharpProspero's renderer and retains the applicable license notice in source.

No complete SharpProspero SDK source tree is redistributed by MM ENGINE. The public repository keeps a copy of the GNU GPL v3 text under `third_party/SharpProspero-GPL-3.0-or-later.txt` for notice/reference purposes. Users remain responsible for obtaining and using an SDK copy they are permitted to use.

## .NET / NativeAOT tooling

The build pipeline uses external Microsoft/.NET tooling under its own licenses. Those tools are not relicensed by MM ENGINE.

## DejaVu Sans

The Hello2D and InputDemo asset folders contain `DejaVuSans.ttf`. The font copyright and permissive license text are reproduced in `third_party/DejaVu-Fonts-LICENSE.txt`. The font must not be sold by itself; derived fonts must respect the reserved-name conditions in its license.

## Original MM ENGINE sample art

`examples/art/mm-mark.svg`, the rendered sample icons/mark, and `examples/models/model-viewer.*` were created for MM ENGINE and are distributed under the project license unless a file states otherwise.

## Excluded development/reference material

Development-only controller OBJ/GLB files, reference images/video, reference applications, private test material, proprietary SDK archives, title credentials, console secrets, generated packages, and local build output are not part of this public source release.

Any third-party component later added to the repository must retain the license and attribution required by its original copyright holder.
