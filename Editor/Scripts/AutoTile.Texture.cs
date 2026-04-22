using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace CrossingLears.Editor
{
    public partial class AutoTile
    {
        private bool EnsureTextureIsSliced7x7(string spriteSheetPath)
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(spriteSheetPath) as TextureImporter;
            Texture2D sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(spriteSheetPath);

            if (sourceTexture.width % GridSize != 0 || sourceTexture.height % GridSize != 0)
            {
                EditorUtility.DisplayDialog("AutoTile", "The Tile Map size must be divisible by 7.", "OK");
                return false;
            }

            int tileWidth = sourceTexture.width / GridSize;
            int tileHeight = sourceTexture.height / GridSize;
            SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(textureImporter);
            dataProvider.InitSpriteEditorDataProvider();
            SpriteRect[] existingSpriteRects = dataProvider.GetSpriteRects();

            if (existingSpriteRects.Length == Full49RuleCount && AreSpriteRects7x7(existingSpriteRects, tileWidth, tileHeight))
            {
                return true;
            }

            SpriteRect[] spriteRects = new SpriteRect[Full49RuleCount];
            SpriteNameFileIdPair[] nameFileIdPairs = new SpriteNameFileIdPair[Full49RuleCount];
            int spriteIndex = 0;
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Multiple;

            for (int y = GridSize - 1; y >= 0; y--)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    GUID spriteGuid = GUID.Generate();
                    SpriteRect spriteRect = new SpriteRect();
                    spriteRect.name = sourceTexture.name + "_" + spriteIndex;
                    spriteRect.spriteID = spriteGuid;
                    spriteRect.alignment = SpriteAlignment.Center;
                    spriteRect.pivot = new Vector2(0.5f, 0.5f);
                    spriteRect.rect = new Rect(x * tileWidth, y * tileHeight, tileWidth, tileHeight);
                    spriteRects[spriteIndex] = spriteRect;
                    nameFileIdPairs[spriteIndex] = new SpriteNameFileIdPair(spriteRect.name, spriteGuid);
                    spriteIndex++;
                }
            }

            dataProvider.SetSpriteRects(spriteRects);

            if (dataProvider.HasDataProvider(typeof(ISpriteNameFileIdDataProvider)))
            {
                ISpriteNameFileIdDataProvider nameFileIdDataProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
                nameFileIdDataProvider.SetNameFileIdPairs(nameFileIdPairs);
            }

            dataProvider.Apply();
            textureImporter.SaveAndReimport();
            return true;
        }

        private bool AreSpriteRects7x7(SpriteRect[] spriteRects, int tileWidth, int tileHeight)
        {
            for (int i = 0; i < spriteRects.Length; i++)
            {
                int x = i % GridSize;
                int y = GridSize - 1 - i / GridSize;
                Rect expectedRect = new Rect(x * tileWidth, y * tileHeight, tileWidth, tileHeight);

                if (spriteRects[i].rect != expectedRect)
                {
                    return false;
                }
            }

            return true;
        }

        private Sprite[] LoadSortedSprites(string spriteSheetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath).OfType<Sprite>().OrderByDescending(sprite => sprite.rect.y).ThenBy(sprite => sprite.rect.x).ToArray();
        }

        private Texture2D GetActiveSpriteTemplateTexture()
        {
            if (spriteTemplateTexture != null)
            {
                return spriteTemplateTexture;
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultSpriteTemplateTexturePath);
        }

        private bool EnsureSpriteGridInitialized(Texture2D activeTemplateTexture)
        {
            string activeTexturePath = AssetDatabase.GetAssetPath(activeTemplateTexture);

            if (!EnsureTextureIsSliced7x7(activeTexturePath))
            {
                return false;
            }

            if (spriteGridSourcePath == activeTexturePath)
            {
                return true;
            }

            Sprite[] sprites = LoadSortedSprites(activeTexturePath);

            if (sprites.Length != Full49RuleCount)
            {
                return false;
            }

            for (int i = 0; i < spriteGrid.Length; i++)
            {
                spriteGrid[i] = sprites[i];
            }

            spriteGridSourcePath = activeTexturePath;
            return true;
        }

        private Texture2D BuildTextureFromSpriteGrid()
        {
            Texture2D activeTemplateTexture = GetActiveSpriteTemplateTexture();
            string templateTexturePath = AssetDatabase.GetAssetPath(activeTemplateTexture);

            if (!EnsureTextureIsReadable(templateTexturePath))
            {
                return null;
            }

            int tileWidth = activeTemplateTexture.width / GridSize;
            int tileHeight = activeTemplateTexture.height / GridSize;
            Texture2D outputTexture = new Texture2D(activeTemplateTexture.width, activeTemplateTexture.height, TextureFormat.RGBA32, false);
            outputTexture.filterMode = activeTemplateTexture.filterMode;
            outputTexture.wrapMode = activeTemplateTexture.wrapMode;
            outputTexture.SetPixels(activeTemplateTexture.GetPixels());

            for (int i = 0; i < spriteGrid.Length; i++)
            {
                if (!CanUseSpriteForGrid(spriteGrid[i], tileWidth, tileHeight))
                {
                    return null;
                }

                CopySpriteToGridCell(outputTexture, spriteGrid[i], i, tileWidth, tileHeight);
            }

            outputTexture.Apply();
            string outputTexturePath = GetOutputTexturePath(templateTexturePath);
            EnsureAssetFolderExists(outputTexturePath);
            File.WriteAllBytes(outputTexturePath, outputTexture.EncodeToPNG());
            AssetDatabase.ImportAsset(outputTexturePath, ImportAssetOptions.ForceUpdate);
            EnsureTextureIsSliced7x7(outputTexturePath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outputTexturePath);
        }

        private bool CanUseSpriteForGrid(Sprite sprite, int tileWidth, int tileHeight)
        {
            string spriteTexturePath = AssetDatabase.GetAssetPath(sprite.texture);

            if (!EnsureTextureIsReadable(spriteTexturePath))
            {
                return false;
            }

            Rect spriteRect = sprite.rect;

            if (Mathf.RoundToInt(spriteRect.width) != tileWidth || Mathf.RoundToInt(spriteRect.height) != tileHeight)
            {
                EditorUtility.DisplayDialog("AutoTile", "All sprites must match the template tile size.", "OK");
                return false;
            }

            return true;
        }

        private void CopySpriteToGridCell(Texture2D destinationTexture, Sprite sourceSprite, int spriteIndex, int tileWidth, int tileHeight)
        {
            Rect sourceRect = sourceSprite.rect;
            int sourceX = Mathf.RoundToInt(sourceRect.x);
            int sourceY = Mathf.RoundToInt(sourceRect.y);
            int destinationX = spriteIndex % GridSize * tileWidth;
            int destinationY = (GridSize - 1 - spriteIndex / GridSize) * tileHeight;
            Color[] sourcePixels = sourceSprite.texture.GetPixels(sourceX, sourceY, tileWidth, tileHeight);
            destinationTexture.SetPixels(destinationX, destinationY, tileWidth, tileHeight, sourcePixels);
        }

        private string GetOutputTexturePath(string templateTexturePath)
        {
            if (selectedTextureWriteMode == AutoTileTextureWriteMode.ApplyToTexture)
            {
                return templateTexturePath;
            }

            string assetDirectory = Path.GetDirectoryName(assetPath);
            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            string outputTexturePath = assetDirectory + "/" + assetName + ".png";
            return AssetDatabase.GenerateUniqueAssetPath(outputTexturePath);
        }

        private bool EnsureTextureIsReadable(string texturePath)
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;

            if (textureImporter.isReadable)
            {
                return true;
            }

            textureImporter.isReadable = true;
            textureImporter.SaveAndReimport();
            return true;
        }

        private void EnsureAssetFolderExists(string targetAssetPath)
        {
            string folderPath = Path.GetDirectoryName(targetAssetPath);

            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            string[] folderParts = folderPath.Split('/');
            string currentPath = folderParts[0];

            for (int i = 1; i < folderParts.Length; i++)
            {
                string nextPath = currentPath + "/" + folderParts[i];

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folderParts[i]);
                }

                currentPath = nextPath;
            }
        }

        private bool EnsureConverterGridReady()
        {
            string texturePath = AssetDatabase.GetAssetPath(converterSourceTexture);
            Sprite[] sprites = LoadSortedSprites(texturePath);

            if (sprites.Length == 0)
            {
                EditorUtility.DisplayDialog("AutoTile", "Source Texture must already contain sprites.", "OK");
                converterSourceTexture = null;
                converterGridSourcePath = string.Empty;
                ClearConverterSpriteGrid();
                return false;
            }

            Rect firstSpriteRect = sprites[0].rect;

            for (int i = 1; i < sprites.Length; i++)
            {
                Rect spriteRect = sprites[i].rect;

                if (Mathf.RoundToInt(spriteRect.width) != Mathf.RoundToInt(firstSpriteRect.width) || Mathf.RoundToInt(spriteRect.height) != Mathf.RoundToInt(firstSpriteRect.height))
                {
                    EditorUtility.DisplayDialog("AutoTile", "All Source Texture sprites must have the same size.", "OK");
                    converterSourceTexture = null;
                    converterGridSourcePath = string.Empty;
                    ClearConverterSpriteGrid();
                    return false;
                }
            }

            if (converterGridSourcePath != texturePath)
            {
                for (int i = 0; i < converterSpriteGrid.Length; i++)
                {
                    converterSpriteGrid[i] = null;
                }

                converterGridSourcePath = texturePath;
            }

            return true;
        }

        private void ClearConverterSpriteGrid()
        {
            for (int i = 0; i < converterSpriteGrid.Length; i++)
            {
                converterSpriteGrid[i] = null;
            }
        }

        private bool ValidateConverterTextureConsistency(Texture2D sourceTexture, TextureTilemapConverter converter)
        {
            if (sourceTexture.width != converter.originalTextureDimensions.x || sourceTexture.height != converter.originalTextureDimensions.y)
            {
                EditorUtility.DisplayDialog("AutoTile", "Texture dimensions do not match the TextureTilemapConverter.", "OK");
                return false;
            }

            if (converter.spriteIndices == null || converter.spriteIndices.Length != Full49RuleCount)
            {
                EditorUtility.DisplayDialog("AutoTile", "TextureTilemapConverter data is invalid.", "OK");
                return false;
            }

            string texturePath = AssetDatabase.GetAssetPath(sourceTexture);
            Sprite[] sprites = LoadSortedSprites(texturePath);

            if (sprites.Length == 0)
            {
                EditorUtility.DisplayDialog("AutoTile", "The Texture must contain sprites.", "OK");
                return false;
            }

            for (int i = 0; i < sprites.Length; i++)
            {
                Rect spriteRect = sprites[i].rect;

                if (Mathf.RoundToInt(spriteRect.width) != converter.tileSize.x || Mathf.RoundToInt(spriteRect.height) != converter.tileSize.y)
                {
                    EditorUtility.DisplayDialog("AutoTile", "Texture sprite size does not match the TextureTilemapConverter.", "OK");
                    return false;
                }
            }

            for (int i = 0; i < converter.spriteIndices.Length; i++)
            {
                int spriteIndex = converter.spriteIndices[i];

                if (spriteIndex < -1 || spriteIndex >= sprites.Length)
                {
                    EditorUtility.DisplayDialog("AutoTile", "TextureTilemapConverter sprite indices are invalid.", "OK");
                    return false;
                }
            }

            return true;
        }

        private int[] BuildConverterIndices()
        {
            string texturePath = AssetDatabase.GetAssetPath(converterSourceTexture);
            Sprite[] sourceSprites = LoadSortedSprites(texturePath);
            int[] spriteIndices = new int[Full49RuleCount];

            for (int i = 0; i < converterSpriteGrid.Length; i++)
            {
                spriteIndices[i] = FindSpriteIndex(sourceSprites, converterSpriteGrid[i]);
            }

            return spriteIndices;
        }

        private int FindSpriteIndex(Sprite[] sprites, Sprite targetSprite)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == targetSprite)
                {
                    return i;
                }
            }

            return -1;
        }

        private Texture2D BuildTextureFromConverter(Texture2D sourceTexture, TextureTilemapConverter converter)
        {
            string sourceTexturePath = AssetDatabase.GetAssetPath(sourceTexture);

            if (!EnsureTextureIsReadable(sourceTexturePath))
            {
                return null;
            }

            if (!EnsureTextureIsSliced7x7(sourceTexturePath))
            {
                return null;
            }

            Sprite[] sourceSprites = LoadSortedSprites(sourceTexturePath);
            Texture2D outputTexture = new Texture2D(converter.tileSize.x * GridSize, converter.tileSize.y * GridSize, TextureFormat.RGBA32, false);
            outputTexture.filterMode = sourceTexture.filterMode;
            outputTexture.wrapMode = sourceTexture.wrapMode;
            FillTextureWithTransparentPixels(outputTexture);

            for (int i = 0; i < converter.spriteIndices.Length; i++)
            {
                int sourceSpriteIndex = converter.spriteIndices[i];

                if (sourceSpriteIndex < 0)
                {
                    continue;
                }

                CopySpriteToGridCell(outputTexture, sourceSprites[sourceSpriteIndex], i, converter.tileSize.x, converter.tileSize.y);
            }

            outputTexture.Apply();
            string outputTexturePath = GetConverterOutputTexturePath();
            EnsureAssetFolderExists(outputTexturePath);
            File.WriteAllBytes(outputTexturePath, outputTexture.EncodeToPNG());
            AssetDatabase.ImportAsset(outputTexturePath, ImportAssetOptions.ForceUpdate);
            EnsureTextureIsSliced7x7(outputTexturePath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outputTexturePath);
        }

        private string GetConverterOutputTexturePath()
        {
            string assetDirectory = Path.GetDirectoryName(assetPath);
            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            string outputTexturePath = assetDirectory + "/" + assetName + ".png";
            return AssetDatabase.GenerateUniqueAssetPath(outputTexturePath);
        }

        private void FillTextureWithTransparentPixels(Texture2D texture)
        {
            Color[] pixels = new Color[texture.width * texture.height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            texture.SetPixels(pixels);
        }

        private void SyncEditConverterState()
        {
            if (editConverter == null)
            {
                loadedEditConverterPath = string.Empty;
                converterSourceTexture = null;
                converterGridSourcePath = string.Empty;
                ClearConverterSpriteGrid();
                return;
            }

            string converterAssetPath = AssetDatabase.GetAssetPath(editConverter);

            if (loadedEditConverterPath == converterAssetPath)
            {
                return;
            }

            loadedEditConverterPath = converterAssetPath;
            converterSourceTexture = LoadConverterOriginalTexture(editConverter);

            if (converterSourceTexture == null)
            {
                converterGridSourcePath = string.Empty;
                ClearConverterSpriteGrid();
                return;
            }

            if (!ValidateConverterTextureConsistency(converterSourceTexture, editConverter))
            {
                converterSourceTexture = null;
                converterGridSourcePath = string.Empty;
                ClearConverterSpriteGrid();
                return;
            }

            LoadConverterSpritesIntoGrid(editConverter, converterSourceTexture);
        }

        private Texture2D LoadConverterOriginalTexture(TextureTilemapConverter converter)
        {
            if (string.IsNullOrWhiteSpace(converter.originalTexturePath))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(converter.originalTexturePath);
        }

        private void LoadConverterSpritesIntoGrid(TextureTilemapConverter converter, Texture2D sourceTexture)
        {
            string texturePath = AssetDatabase.GetAssetPath(sourceTexture);
            Sprite[] sprites = LoadSortedSprites(texturePath);

            for (int i = 0; i < converterSpriteGrid.Length; i++)
            {
                int spriteIndex = converter.spriteIndices[i];

                if (spriteIndex < 0)
                {
                    converterSpriteGrid[i] = null;
                    continue;
                }

                converterSpriteGrid[i] = sprites[spriteIndex];
            }

            converterGridSourcePath = texturePath;
        }

        private void SyncEditConverterGridFromCurrentTexture()
        {
            if (editConverter == null || converterSourceTexture == null)
            {
                return;
            }

            string texturePath = AssetDatabase.GetAssetPath(converterSourceTexture);

            if (converterGridSourcePath == texturePath)
            {
                return;
            }

            if (!ValidateConverterTextureConsistency(converterSourceTexture, editConverter))
            {
                converterSourceTexture = null;
                converterGridSourcePath = string.Empty;
                ClearConverterSpriteGrid();
                return;
            }

            LoadConverterSpritesIntoGrid(editConverter, converterSourceTexture);
        }
    }
}
