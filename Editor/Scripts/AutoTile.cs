using UnityEditor;
using UnityEngine;

namespace CrossingLears.Editor
{
    // ----------------------------------------------------------------------------
    // Integrated with code adapted from Alexandre Brull
    // https://brullalex.itch.io/
    // ----------------------------------------------------------------------------
    
    public partial class AutoTile : CL_WindowTab
    {
        public override void DrawTitle()
        {
            base.DrawTitle();
            DrawSourceModeSelector();
        }

        private const string Full49TemplatePath = "Assets/Crossing Lears/Auto RuleTile/Editor/AutoRuleTile49_default.asset";
        private const string DefaultSpriteTemplateTexturePath = "Assets/Crossing Lears/Auto RuleTile/Editor/RoadTileset.png";
        private const int Full49RuleCount = 49;
        private const int Full49DefaultSpriteIndex = 24;
        private const int GridSize = 7;
        private const float SpriteFieldSize = 48f;
        private const string DefaultAssetPath = "Assets/New Auto Rule Tile.asset";

        private AutoTileSourceMode selectedSourceMode = AutoTileSourceMode.FromTexture;
        private AutoTilePattern selectedPattern = AutoTilePattern.Full49;
        private AutoTileTextureWriteMode selectedTextureWriteMode = AutoTileTextureWriteMode.MakeNewTexture;
        private AutoTileConverterMode selectedConverterMode = AutoTileConverterMode.CreateConverter;
        private Texture2D tileMap;
        private Texture2D spriteTemplateTexture;
        private Texture2D converterSourceTexture;
        private Texture2D converterTileMapTexture;
        private TextureTilemapConverter selectedTilemapConverter;
        private TextureTilemapConverter editConverter;
        private string assetPath = DefaultAssetPath;
        private readonly Sprite[] spriteGrid = new Sprite[Full49RuleCount];
        private readonly Sprite[] converterSpriteGrid = new Sprite[Full49RuleCount];
        private string spriteGridSourcePath;
        private string converterGridSourcePath;
        private string loadedEditConverterPath;

        public override string TabName => "Auto Tile";

        private enum AutoTileSourceMode
        {
            FromTexture = 0,
            FromSprites = 1,
            FromConverter = 2
        }

        private enum AutoTilePattern
        {
            Blob4Bit = 0,
            Full49 = 1
        }

        private enum AutoTileTextureWriteMode
        {
            ApplyToTexture = 0,
            MakeNewTexture = 1
        }

        private enum AutoTileConverterMode
        {
            CreateConverter = 0,
            CreateTilemapUsingConverter = 1,
            EditExistingConverter = 2
        }

    }
}
