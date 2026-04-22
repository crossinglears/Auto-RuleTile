using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CrossingLears.Editor
{
    public partial class AutoTile
    {
        private void CreateAutoTile()
        {
            if (selectedTileAssetMode == AutoTileAssetMode.AnimationTile)
            {
                CreateAnimationTileAsset();
                return;
            }

            if (selectedSourceMode == AutoTileSourceMode.FromConverter && selectedConverterMode == AutoTileConverterMode.EditExistingConverter)
            {
                if (!ValidateConverterMode())
                {
                    return;
                }

                SaveExistingConverter();
                return;
            }

            switch (selectedSourceMode)
            {
                case AutoTileSourceMode.FromTexture:
                    if (!ValidateTextureMode())
                    {
                        return;
                    }

                    break;
                case AutoTileSourceMode.FromSprites:
                    if (!ValidateSpriteMode())
                    {
                        return;
                    }

                    break;
                case AutoTileSourceMode.FromConverter:
                    if (!ValidateConverterMode())
                    {
                        return;
                    }

                    break;
            }

            string selectedAssetPath = EditorUtility.SaveFilePanelInProject("Create AutoTile", Path.GetFileNameWithoutExtension(assetPath), "asset", "Select where to create the autotile asset.");

            if (string.IsNullOrWhiteSpace(selectedAssetPath))
            {
                return;
            }

            assetPath = selectedAssetPath;

            if (!ValidateAssetPath())
            {
                return;
            }

            switch (selectedSourceMode)
            {
                case AutoTileSourceMode.FromTexture:
                    CreateFull49AutoTile(tileMap);
                    break;
                case AutoTileSourceMode.FromSprites:
                    Texture2D builtTexture = BuildTextureFromSpriteGrid();

                    if (builtTexture == null)
                    {
                        return;
                    }

                    CreateFull49AutoTile(builtTexture);
                    break;
                case AutoTileSourceMode.FromConverter:
                    CreateFromConverter();
                    break;
            }
        }

        private void CreateAnimationTileAsset()
        {
            if (!ValidateAnimationTileMode())
            {
                return;
            }

            string selectedAssetPath = EditorUtility.SaveFilePanelInProject("Create AnimationTile", Path.GetFileNameWithoutExtension(assetPath), "asset", "Select where to create the animation tile asset.");

            if (string.IsNullOrWhiteSpace(selectedAssetPath))
            {
                return;
            }

            assetPath = selectedAssetPath;

            if (!ValidateAssetPath())
            {
                return;
            }

            TileBase firstTileBase = animationTileSources[0];

            if (firstTileBase is Tile)
            {
                CreateAnimatedTileFromTiles();
                return;
            }

            if (firstTileBase is RuleTile)
            {
                CreateAnimatedRuleTileFromRuleTiles();
                return;
            }

            throw new InvalidOperationException("Only Tile and RuleTile inputs are supported for AnimationTile creation.");
        }

        private bool ValidateAssetPath()
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                EditorUtility.DisplayDialog("AutoTile", "Assign a valid asset path.", "OK");
                return false;
            }

            if (!assetPath.StartsWith("Assets/"))
            {
                EditorUtility.DisplayDialog("AutoTile", "Asset path must stay inside Assets.", "OK");
                return false;
            }

            if (!assetPath.EndsWith(".asset"))
            {
                assetPath += ".asset";
            }

            return true;
        }

        private bool ValidateAnimationTileMode()
        {
            if (animationTileSources.Count == 0)
            {
                EditorUtility.DisplayDialog("AutoTile", "Add at least one TileBase.", "OK");
                return false;
            }

            TileBase firstTileBase = null;

            for (int i = 0; i < animationTileSources.Count; i++)
            {
                TileBase tileBase = animationTileSources[i];

                if (tileBase == null)
                {
                    EditorUtility.DisplayDialog("AutoTile", "All TileBase slots must contain a tile.", "OK");
                    return false;
                }

                if (firstTileBase == null)
                {
                    firstTileBase = tileBase;
                    continue;
                }

                if (tileBase.GetType() != firstTileBase.GetType())
                {
                    throw new InvalidOperationException("All TileBase inputs must be the same type.");
                }
            }

            return true;
        }

        private void ClearSpriteGrid()
        {
            Texture2D activeTemplateTexture = GetActiveSpriteTemplateTexture();

            if (activeTemplateTexture == null)
            {
                for (int i = 0; i < spriteGrid.Length; i++)
                {
                    spriteGrid[i] = null;
                }

                spriteGridSourcePath = string.Empty;
                return;
            }

            string activeTexturePath = AssetDatabase.GetAssetPath(activeTemplateTexture);

            if (!EnsureTextureIsSliced7x7(activeTexturePath))
            {
                return;
            }

            Sprite[] sprites = LoadSortedSprites(activeTexturePath);

            if (sprites.Length != Full49RuleCount)
            {
                return;
            }

            for (int i = 0; i < spriteGrid.Length; i++)
            {
                spriteGrid[i] = sprites[i];
            }

            spriteGridSourcePath = activeTexturePath;
        }

        private bool ValidateTextureMode()
        {
            if (tileMap == null)
            {
                EditorUtility.DisplayDialog("AutoTile", "Assign a Tile Map texture first.", "OK");
                return false;
            }

            return true;
        }

        private bool ValidateSpriteMode()
        {
            Texture2D activeTemplateTexture = GetActiveSpriteTemplateTexture();

            if (activeTemplateTexture == null)
            {
                EditorUtility.DisplayDialog("AutoTile", "Assign a Template Texture or set a valid fallback texture path.", "OK");
                return false;
            }

            if (!EnsureSpriteGridInitialized(activeTemplateTexture))
            {
                return false;
            }

            for (int i = 0; i < spriteGrid.Length; i++)
            {
                if (spriteGrid[i] == null)
                {
                    EditorUtility.DisplayDialog("AutoTile", "All 49 sprite slots must contain a sprite.", "OK");
                    return false;
                }
            }

            return true;
        }

        private void CreateFull49AutoTile(Texture2D sourceTileMap)
        {
            RuleTile template = AssetDatabase.LoadAssetAtPath<RuleTile>(Full49TemplatePath);

            if (template == null)
            {
                template = AssetDatabase.LoadAssetAtPath<RuleTile>(Full49TemplatePathFallback);
            }
            if (template == null)
            {
                EditorUtility.DisplayDialog("AutoTile", "Missing the 49 tile RuleTile template asset.", "OK");
                return;
            }

            string spriteSheetPath = AssetDatabase.GetAssetPath(sourceTileMap);

            if (!EnsureTextureIsSliced7x7(spriteSheetPath))
            {
                return;
            }

            Sprite[] sprites = LoadSortedSprites(spriteSheetPath);

            if (sprites.Length != Full49RuleCount)
            {
                EditorUtility.DisplayDialog("AutoTile", "The Tile Map could not be sliced into 49 sprites.", "OK");
                return;
            }

            EnsureAssetFolderExists(assetPath);

            RuleTile newRuleTile = ScriptableObject.CreateInstance<RuleTile>();
            EditorUtility.CopySerialized(template, newRuleTile);

            for (int i = 0; i < newRuleTile.m_TilingRules.Count; i++)
            {
                newRuleTile.m_TilingRules[i].m_Sprites[0] = sprites[i];
            }

            newRuleTile.m_DefaultSprite = sprites[Full49DefaultSpriteIndex];
            AssetDatabase.CreateAsset(newRuleTile, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(newRuleTile);
            Selection.activeObject = newRuleTile;
        }

        private bool ValidateConverterMode()
        {
            switch (selectedConverterMode)
            {
                case AutoTileConverterMode.CreateConverter:
                    return ValidateCreateConverterMode();
                case AutoTileConverterMode.CreateTilemapUsingConverter:
                    return ValidateCreateTilemapUsingConverterMode();
                case AutoTileConverterMode.EditExistingConverter:
                    return ValidateEditExistingConverterMode();
            }

            return false;
        }

        private bool ValidateCreateConverterMode()
        {
            if (converterSourceTexture == null)
            {
                EditorUtility.DisplayDialog("AutoTile", "Assign a Source Texture first.", "OK");
                return false;
            }

            if (!EnsureConverterGridReady())
            {
                return false;
            }

            for (int i = 0; i < converterSpriteGrid.Length; i++)
            {
                Sprite sprite = converterSpriteGrid[i];

                if (sprite == null)
                {
                    continue;
                }

                if (sprite.texture != converterSourceTexture)
                {
                    EditorUtility.DisplayDialog("AutoTile", "All converter sprites must come from Source Texture.", "OK");
                    return false;
                }
            }

            return true;
        }

        private bool ValidateCreateTilemapUsingConverterMode()
        {
            if (converterTileMapTexture == null)
            {
                EditorUtility.DisplayDialog("AutoTile", "Assign a Texture first.", "OK");
                return false;
            }

            if (selectedTilemapConverter == null)
            {
                EditorUtility.DisplayDialog("AutoTile", "Assign a TextureTilemapConverter first.", "OK");
                return false;
            }

            if (!ValidateConverterTextureConsistency(converterTileMapTexture, selectedTilemapConverter))
            {
                return false;
            }

            return true;
        }

        private bool ValidateEditExistingConverterMode()
        {
            if (editConverter == null)
            {
                EditorUtility.DisplayDialog("AutoTile", "Assign a TextureTilemapConverter first.", "OK");
                return false;
            }

            if (converterSourceTexture == null)
            {
                EditorUtility.DisplayDialog("AutoTile", "Assign a matching texture first.", "OK");
                return false;
            }

            if (!EnsureConverterGridReady())
            {
                return false;
            }

            if (!ValidateConverterTextureConsistency(converterSourceTexture, editConverter))
            {
                return false;
            }

            for (int i = 0; i < converterSpriteGrid.Length; i++)
            {
                Sprite sprite = converterSpriteGrid[i];

                if (sprite == null)
                {
                    continue;
                }

                if (sprite.texture != converterSourceTexture)
                {
                    EditorUtility.DisplayDialog("AutoTile", "All converter sprites must come from Source Texture.", "OK");
                    return false;
                }
            }

            return true;
        }

        private void CreateFromConverter()
        {
            switch (selectedConverterMode)
            {
                case AutoTileConverterMode.CreateConverter:
                    CreateTextureTilemapConverter();
                    break;
                case AutoTileConverterMode.CreateTilemapUsingConverter:
                    CreateTilemapUsingConverter();
                    break;
                case AutoTileConverterMode.EditExistingConverter:
                    SaveExistingConverter();
                    break;
            }
        }

        private void CreateTextureTilemapConverter()
        {
            Sprite[] sourceSprites = LoadSortedSprites(AssetDatabase.GetAssetPath(converterSourceTexture));
            Rect firstSpriteRect = sourceSprites[0].rect;
            TextureTilemapConverter converter = ScriptableObject.CreateInstance<TextureTilemapConverter>();
            converter.originalTexturePath = AssetDatabase.GetAssetPath(converterSourceTexture);
            converter.originalTextureDimensions = new Vector2Int(converterSourceTexture.width, converterSourceTexture.height);
            converter.tileSize = new Vector2Int(Mathf.RoundToInt(firstSpriteRect.width), Mathf.RoundToInt(firstSpriteRect.height));
            converter.spriteIndices = BuildConverterIndices();
            EnsureAssetFolderExists(assetPath);
            AssetDatabase.CreateAsset(converter, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(converter);
            Selection.activeObject = converter;
        }

        private void CreateTilemapUsingConverter()
        {
            Texture2D builtTexture = BuildTextureFromConverter(converterTileMapTexture, selectedTilemapConverter);

            if (builtTexture == null)
            {
                return;
            }

            CreateFull49AutoTile(builtTexture);
        }

        private void SaveExistingConverter()
        {
            Sprite[] sourceSprites = LoadSortedSprites(AssetDatabase.GetAssetPath(converterSourceTexture));
            Rect firstSpriteRect = sourceSprites[0].rect;
            editConverter.originalTexturePath = AssetDatabase.GetAssetPath(converterSourceTexture);
            editConverter.originalTextureDimensions = new Vector2Int(converterSourceTexture.width, converterSourceTexture.height);
            editConverter.tileSize = new Vector2Int(Mathf.RoundToInt(firstSpriteRect.width), Mathf.RoundToInt(firstSpriteRect.height));
            editConverter.spriteIndices = BuildConverterIndices();
            EditorUtility.SetDirty(editConverter);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(editConverter);
            Selection.activeObject = editConverter;
        }

        private void CreateAnimatedTileFromTiles()
        {
            AnimatedTile animatedTile = ScriptableObject.CreateInstance<AnimatedTile>();
            List<Sprite> animatedSprites = new List<Sprite>();
            Tile firstTile = (Tile)animationTileSources[0];

            for (int i = 0; i < animationTileSources.Count; i++)
            {
                Tile tile = (Tile)animationTileSources[i];
                animatedSprites.Add(tile.sprite);
            }

            animatedTile.m_AnimatedSprites = animatedSprites.ToArray();
            animatedTile.m_MinSpeed = 1f;
            animatedTile.m_MaxSpeed = 1f;
            animatedTile.m_TileColliderType = firstTile.colliderType;
            EnsureAssetFolderExists(assetPath);
            AssetDatabase.CreateAsset(animatedTile, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(animatedTile);
            Selection.activeObject = animatedTile;
        }

        private void CreateAnimatedRuleTileFromRuleTiles()
        {
            RuleTile firstRuleTile = (RuleTile)animationTileSources[0];
            RuleTile animatedRuleTile = (RuleTile)ScriptableObject.CreateInstance(firstRuleTile.GetType());
            EditorUtility.CopySerialized(firstRuleTile, animatedRuleTile);
            int expectedRuleCount = firstRuleTile.m_TilingRules.Count;

            for (int tileIndex = 1; tileIndex < animationTileSources.Count; tileIndex++)
            {
                RuleTile sourceRuleTile = (RuleTile)animationTileSources[tileIndex];

                if (sourceRuleTile.m_TilingRules.Count != expectedRuleCount)
                {
                    throw new InvalidOperationException("All RuleTiles must have the same rule count.");
                }
            }

            for (int ruleIndex = 0; ruleIndex < animatedRuleTile.m_TilingRules.Count; ruleIndex++)
            {
                Sprite[] animatedSprites = new Sprite[animationTileSources.Count];

                for (int tileIndex = 0; tileIndex < animationTileSources.Count; tileIndex++)
                {
                    RuleTile sourceRuleTile = (RuleTile)animationTileSources[tileIndex];
                    animatedSprites[tileIndex] = sourceRuleTile.m_TilingRules[ruleIndex].m_Sprites[0];
                }

                animatedRuleTile.m_TilingRules[ruleIndex].m_Output = RuleTile.TilingRuleOutput.OutputSprite.Animation;
                animatedRuleTile.m_TilingRules[ruleIndex].m_Sprites = animatedSprites;
            }

            animatedRuleTile.m_DefaultSprite = firstRuleTile.m_DefaultSprite;
            EnsureAssetFolderExists(assetPath);
            AssetDatabase.CreateAsset(animatedRuleTile, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(animatedRuleTile);
            Selection.activeObject = animatedRuleTile;
        }
    }
}
