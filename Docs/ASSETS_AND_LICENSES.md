# Sources, licensing and distribution gate

**As of 2026-11-06.** This is an engineering inventory, not legal advice.

## Historical project and standalone code license

- Original historical MagicMirror prototype: https://github.com/Kevoyuan/MagicMirror . Its OpenPose plugin sources and bundled media are **excluded** from the standalone MagicMirror 2.0 source tree.
- The repository owner must choose an explicit license for this **new project's original source code**. Do not infer a grant from an older README or from public repository visibility.
- No OpenPose dependency or model binary is redistributed with the clean source snapshot.

## Z-Anatomy resources

- Models and Blender template: https://github.com/Z-Anatomy/Models-of-human-anatomy
- Unity application and FBX exports: https://github.com/LluisV/Z-Anatomy/tree/PC-Version
- Main Z-Anatomy materials: **CC BY-SA 4.0** according to the project's README.
- Attribution includes **BodyParts3D — CC BY-SA 2.1 Japan** where applicable.
- Z-Anatomy's README also lists source-specific materials with **CC BY-NC-SA 4.0** and **CC BY-NC 4.0** restrictions (e.g. particular inner-ear/kidney models). Content must be audited at the mesh/asset level; assuming the entire collection is uniformly redistributable is unsafe.
- Attribution, share-alike, transformation records and other license obligations must be preserved for applicable assets.
- The standalone MagicMirror 2.0 source snapshot contains **no copied Z-Anatomy FBX files**.

## Model sources planned for evaluation

- `SkeletalSystem100.fbx`
- `Joints100.fbx`
- `MuscularSystem100.fbx`
- `VisceralSystem100.fbx`

All available at https://github.com/LluisV/Z-Anatomy/tree/PC-Version/Resources/Models/FBX .

## Release gate

Before bundling an asset:
1. Record exact source URL, revision, filename, original author, applicable license and attribution.
2. Determine whether the specific mesh can be redistributed in the proposed release model.
3. Capture any modifications, derivatives, and required share-alike terms.
4. Exclude unresolved non-commercial and conflicting licenses from commercial distributions.
5. Preserve notices in both README and product About/Credits.
6. Do not redistribute assets with unclear provenance.
