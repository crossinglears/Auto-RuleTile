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

            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

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

            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

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
    }
}
