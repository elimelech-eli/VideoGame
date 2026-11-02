# Corporate Office source-derived environment kit

Source: Studio Office Interior by Arun Babu
https://sketchfab.com/3d-models/studio-office-interior-8a43d357b98b4f7aadba6946d812869a
License: Creative Commons Attribution4.0
https://creativecommons.org/licenses/by/4.0/

Original archive obtained through the official download UI. SHA256:
48f2c554c22dd3e0767be370ad24b08a1eba814148554f7caf79b040d9f18b64

Changes: parquet floor faces cropped at native meter scale with original UVs; source wall face measured into complete closed panel modules with affine source UV density and dominant original wall material; solid backing and closed cut edges added for isometric construction; six source furniture/equipment meshes isolated, normalized to floor/contact origins and given collision proxies. Architecture corners use the same source facade surface and solidification. Existing source material properties retained and available original maps relinked. No full texture rebake or generated detail.

Excluded from extracted kit: Archinteriors-named combined furniture mesh, wall posters, screenshot/wallpaper maps, branded mouse artwork and the original complete source scene. The raw source archive/FBX is a local intake cache, not a runtime dependency or automatically approved public-repository asset. Runtime dependency manifest must contain no original FBX or excluded artwork.

Source object mapping:
Floor1x1/Floor1x2/Floor2x1/Floor2x2: Plane.013 (native parquet floor)
Wall1/Wall2/WallLow1/WallLow2/Corner/CornerLow: Plane.015 (native facade)
Counter: Plane
Cabinet: Box323536.001
Bookcase: Cube
Monitor: Monitor27'Curved.002
Keyboard: Monitor27'Curved.003
Cup: low_poly.001

Rebuilder: CorporateBiomeAssets.cs. Requires the approved raw source at Assets/CorporateReview/Models/StudioOffice.fbx and restored review materials. That private source setup is deliberately not part of the runtime dependency bundle. See artifacts/biome-parallel/corporate/source/provenance.json for source-file hashes and the pipeline checkpoint for remaining validation.
