# External Asset Inventory

Fjordfall v0.3 no longer relies only on runtime primitives. The manual ZIP bundles standalone Unity-importable assets under `Assets/Resources/External`.

## Models

| Asset | Format | Runtime use |
|---|---|---|
| `fjord_house` | OBJ/MTL | village buildings |
| `fjord_tree` | OBJ/MTL | forest clusters |
| `fjord_longboat` | OBJ/MTL | enemy landing boats |
| `fjord_wall` | OBJ/MTL | defensive walls |
| `fjord_rock` | OBJ/MTL | shoreline props |
| `fjord_soldier` | OBJ/MTL | allied and enemy unit bodies |
| `fjord_shield` | OBJ/MTL | swordsman and raider shields |

## Textures

Nine 256×256 albedo maps and six 256×256 normal maps cover grass, cliff stone, plaster, roof shingles, bark, foliage, water, sailcloth and worn iron.

## Ownership and license

These models and textures were created specifically for this Fjordfall build. No model, texture, logo, icon or level data was copied from the reference game. The user may modify, redistribute and commercially use the bundled original assets as part of the project. See `Assets/Resources/External/Licenses/FJORDFALL_ORIGINAL_ASSETS.txt`.

## Fallback behavior

The runtime first loads imported assets through `Resources.Load`. If an asset is missing or fails to import, the previous procedural primitive version remains visible, so one bad asset should not prevent the prototype from opening.

## Git transport fallback

`FjordfallGeneratedModelBuilder.cs` and `FjordfallGeneratedTextureBuilder.cs` recreate missing OBJ/MTL and PNG assets on first Unity import. The downloadable manual ZIP already contains the baked files with UVs and ready-to-import textures.
