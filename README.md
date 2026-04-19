# Auto RuleTile

Create a Unity `RuleTile` from textures, sprite grids, or reusable converter assets.

This package is focused on a 7x7, 49-rule layout and generates a ready-to-use RuleTile asset from a source tilemap texture.

## Features

- Create a RuleTile directly from a 7x7 texture sheet
- Create a RuleTile from a manually assigned 7x7 sprite grid
- Create a `TextureTilemapConverter` asset for remapping sprites from another texture layout
- Rebuild a 7x7 tilemap texture from a converter and generate a RuleTile from it
- Edit an existing converter and continue from previous progress
- Empty converter slots are supported and export as transparent cells

## Requirements

- Unity 6 or later
- A 49-rule template asset included with this package
- `RuleTile` available in the project

## Included Workflows

### From Texture

Use this when your texture is already arranged as a 7x7 tilemap.

- Assign `Tile Map`
- Select `Pattern`
- Press `Make AutoTile`

The texture must be divisible by 7 in both width and height.

### From Sprites

Use this when you want to assemble the 7x7 output manually from sprites.

- Assign `Template Texture` or use the fallback texture
- Fill the 7x7 grid
- Choose whether to write into the template texture or create a new texture
- Press `Make AutoTile`

All assigned sprites must match the template tile size.

### From Converter

Use this when the source texture is not already arranged in the layout expected by `From Texture`.

#### Create Converter

Creates a reusable `TextureTilemapConverter` asset.

- Assign `Source Texture`
- Fill any cells you want in the 7x7 grid
- Leave unused cells empty if needed
- Press `Create Converter`

The source texture does not need to be divisible by 7.

The source texture must:

- already contain sprites
- use consistent sprite sizes

The converter stores:

- original texture path
- original texture dimensions
- tile size
- sprite index mapping for the 7x7 output

#### Create Tilemap Using Converter

Builds a new 7x7 texture from a texture plus a converter, then creates a RuleTile from it.

- Assign `Texture`
- Assign `TextureTilemapConverter`
- Press `Create Texture And Tilemap`

Validation checks:

- texture dimensions must match the converter
- sprite sizes must match the converter
- converter indices must be valid for the texture

Empty converter cells are exported as transparent tiles.

#### Edit Existing Converter

Resume work on a previously created converter.

- Assign `TextureTilemapConverter`
- If the original texture still exists at the saved path, it is loaded automatically
- If not, assign a matching texture manually
- Continue editing the 7x7 grid
- Press `Save Converter`

This updates the existing converter asset in place.

## Output

Depending on the selected mode, the tool can create:

- a `RuleTile` asset
- a generated 7x7 texture
- a `TextureTilemapConverter` asset

## Notes

- The package currently targets the 49-tile pattern
- `From Texture` and final RuleTile generation expect a 7x7 tilemap layout
- Converter assets are useful when different tilesets share the same logical tile meanings but not the same source arrangement
