using UnityEditorInternal;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CrossingLears.Editor
{
    public partial class AutoTile
    {
        private ReorderableList animationTileSourceList;

        public override void DrawContent()
        {
            DrawSelectedModeContent();
        }

        private void DrawSourceModeSelector()
        {
            EditorGUILayout.BeginHorizontal();
            DrawSourceModeButton(AutoTileSourceMode.FromTexture, "From Texture");
            DrawSourceModeButton(AutoTileSourceMode.FromSprites, "From Sprites");
            DrawSourceModeButton(AutoTileSourceMode.FromConverter, "From Converter");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectedModeContent()
        {
            GUIStyle contentStyle = new GUIStyle(EditorStyles.helpBox);
            contentStyle.padding = new RectOffset(10, 10, 10, 10);
            contentStyle.margin = new RectOffset(0, 0, 0, 0);
            EditorGUILayout.BeginVertical(contentStyle);
            GUILayout.Space(2f);

            if (selectedTileAssetMode == AutoTileAssetMode.AnimationTile)
            {
                DrawAnimationTileContent();
                EditorGUILayout.EndVertical();
                return;
            }

            DrawSourceModeSelector();

            switch (selectedSourceMode)
            {
                case AutoTileSourceMode.FromTexture:
                    DrawFromTextureContent();
                    break;
                case AutoTileSourceMode.FromSprites:
                    DrawFromSpritesContent();
                    break;
                case AutoTileSourceMode.FromConverter:
                    DrawFromConverterContent();
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAnimationTileContent()
        {
            EnsureAnimationTileSourceList();
            EditorGUILayout.LabelField("Create a single AnimationTile from TileBase assets.", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);
            animationMinSpeed = EditorGUILayout.FloatField("Min Speed", animationMinSpeed);
            animationMaxSpeed = EditorGUILayout.FloatField("Max Speed", animationMaxSpeed);
            EditorGUILayout.Space(4f);
            animationTileSourceList.DoLayoutList();

            if (GUILayout.Button("Make AnimationTile"))
            {
                CreateAutoTile();
            }
        }

        private void EnsureAnimationTileSourceList()
        {
            if (animationTileSourceList != null)
            {
                return;
            }

            animationTileSourceList = new ReorderableList(animationTileSources, typeof(TileBase), true, true, true, true);
            animationTileSourceList.drawHeaderCallback = DrawAnimationTileSourceListHeader;
            animationTileSourceList.drawElementCallback = DrawAnimationTileSourceListElement;
            animationTileSourceList.elementHeight = EditorGUIUtility.singleLineHeight + 4f;
            animationTileSourceList.onAddCallback = AddAnimationTileSourceListElement;
        }

        private void AddAnimationTileSourceListElement(ReorderableList list)
        {
            animationTileSources.Add(null);
            list.index = animationTileSources.Count - 1;
        }

        private void DrawAnimationTileSourceListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Tile Bases");
            HandleAnimationTileSourceHeaderDrag(rect);
        }

        private void DrawAnimationTileSourceListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            Rect fieldRect = new Rect(rect.x, rect.y + 2f, rect.width, EditorGUIUtility.singleLineHeight);
            animationTileSources[index] = (TileBase)EditorGUI.ObjectField(fieldRect, animationTileSources[index], typeof(TileBase), false);
        }

        private void HandleAnimationTileSourceHeaderDrag(Rect rect)
        {
            Event currentEvent = Event.current;

            if (!rect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            if (currentEvent.type == EventType.DragUpdated)
            {
                if (HasTileBaseDragReferences())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    currentEvent.Use();
                }

                return;
            }

            if (currentEvent.type != EventType.DragPerform)
            {
                return;
            }

            if (!HasTileBaseDragReferences())
            {
                return;
            }

            DragAndDrop.AcceptDrag();

            for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
            {
                TileBase tileBase = DragAndDrop.objectReferences[i] as TileBase;

                if (tileBase == null)
                {
                    continue;
                }

                animationTileSources.Add(tileBase);
            }

            currentEvent.Use();
        }

        private bool HasTileBaseDragReferences()
        {
            for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
            {
                if (DragAndDrop.objectReferences[i] is TileBase)
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawSourceModeButton(AutoTileSourceMode sourceMode, string label)
        {
            GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButtonMid);
            buttonStyle.fixedHeight = 30f;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.margin = new RectOffset(0, 0, 0, 0);
            bool isSelected = selectedSourceMode == sourceMode;
            Color previousBackgroundColor = GUI.backgroundColor;

            if (isSelected)
            {
                GUI.backgroundColor = new Color(0.76f, 0.76f, 0.76f, 1f);
            }

            if (GUILayout.Toggle(isSelected, label, buttonStyle))
            {
                selectedSourceMode = sourceMode;
            }

            GUI.backgroundColor = previousBackgroundColor;
        }

        private void DrawFromTextureContent()
        {
            EditorGUILayout.LabelField("Create a Rule Tile from a texture sheet.", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);
            tileMap = (Texture2D)EditorGUILayout.ObjectField("Tile Map", tileMap, typeof(Texture2D), false);
            selectedPattern = (AutoTilePattern)EditorGUILayout.EnumPopup("Pattern", selectedPattern);

            if (selectedPattern != AutoTilePattern.Full49)
            {
                EditorGUILayout.HelpBox("Only the 49 tile pattern is implemented right now.", MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(selectedPattern != AutoTilePattern.Full49))
            {
                if (GUILayout.Button("Make AutoTile"))
                {
                    CreateAutoTile();
                }
            }
        }

        private void DrawFromSpritesContent()
        {
            EditorGUILayout.LabelField("Create a Rule Tile from a sprite grid.", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);
            selectedPattern = (AutoTilePattern)EditorGUILayout.EnumPopup("Pattern", selectedPattern);
            spriteTemplateTexture = EditorGUILayout.ObjectField("Template Texture", spriteTemplateTexture, typeof(Object), false) as Texture2D;
            selectedTextureWriteMode = (AutoTileTextureWriteMode)EditorGUILayout.EnumPopup("Texture Output", selectedTextureWriteMode);

            if (spriteTemplateTexture == null && selectedTextureWriteMode == AutoTileTextureWriteMode.ApplyToTexture)
            {
                selectedTextureWriteMode = AutoTileTextureWriteMode.MakeNewTexture;
            }

            GUILayout.Space(4);
            if (GUILayout.Button("Clear Sprites"))
            {
                ClearSpriteGrid();
            }

            EditorGUILayout.Space(4f);

            if (spriteTemplateTexture == null)
            {
                EditorGUILayout.HelpBox("Using fallback texture: " + DefaultSpriteTemplateTexturePath, MessageType.None);
            }

            Texture2D activeTemplateTexture = GetActiveSpriteTemplateTexture();
            bool hasValidTemplateTexture = activeTemplateTexture != null;

            if (!hasValidTemplateTexture)
            {
                EditorGUILayout.HelpBox("Fallback texture not found at " + DefaultSpriteTemplateTexturePath, MessageType.Error);
            }
            else if (EnsureSpriteGridInitialized(activeTemplateTexture))
            {
                DrawSpriteGrid();
            }

            if (selectedPattern != AutoTilePattern.Full49)
            {
                EditorGUILayout.HelpBox("Only the 49 tile pattern is implemented right now.", MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(selectedPattern != AutoTilePattern.Full49 || !hasValidTemplateTexture))
            {
                if (GUILayout.Button("Make AutoTile"))
                {
                    CreateAutoTile();
                }
            }
        }

        private void DrawSpriteGrid()
        {
            for (int y = 0; y < GridSize; y++)
            {
                EditorGUILayout.BeginHorizontal();

                for (int x = 0; x < GridSize; x++)
                {
                    int spriteIndex = y * GridSize + x;
                    spriteGrid[spriteIndex] = (Sprite)EditorGUILayout.ObjectField(spriteGrid[spriteIndex], typeof(Sprite), false, GUILayout.Width(SpriteFieldSize), GUILayout.Height(SpriteFieldSize));
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawFromConverterContent()
        {
            EditorGUILayout.LabelField("Create a converter or build a Rule Tile from one.", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);
            selectedConverterMode = (AutoTileConverterMode)EditorGUILayout.EnumPopup("Mode", selectedConverterMode);

            switch (selectedConverterMode)
            {
                case AutoTileConverterMode.CreateConverter:
                    DrawCreateConverterContent();
                    break;
                case AutoTileConverterMode.CreateTilemapUsingConverter:
                    DrawCreateTilemapUsingConverterContent();
                    break;
                case AutoTileConverterMode.EditExistingConverter:
                    DrawEditExistingConverterContent();
                    break;
            }
        }

        private void DrawCreateConverterContent()
        {
            converterSourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source Texture", converterSourceTexture, typeof(Texture2D), false);

            if (converterSourceTexture == null)
            {
                EditorGUILayout.HelpBox("Assign a Source Texture.", MessageType.Info);
            }
            else if (EnsureConverterGridReady())
            {
                GUILayout.Space(4f);
                if (GUILayout.Button("Clear Sprites"))
                {
                    ClearConverterSpriteGrid();
                }

                EditorGUILayout.Space(4f);
                DrawConverterGrid();
            }

            using (new EditorGUI.DisabledScope(converterSourceTexture == null))
            {
                if (GUILayout.Button("Create Converter"))
                {
                    CreateAutoTile();
                }
            }
        }

        private void DrawCreateTilemapUsingConverterContent()
        {
            converterTileMapTexture = (Texture2D)EditorGUILayout.ObjectField("Texture", converterTileMapTexture, typeof(Texture2D), false);
            selectedTilemapConverter = (TextureTilemapConverter)EditorGUILayout.ObjectField("TextureTilemapConverter", selectedTilemapConverter, typeof(TextureTilemapConverter), false);

            if (GUILayout.Button("Create Texture And Tilemap"))
            {
                CreateAutoTile();
            }
        }

        private void DrawEditExistingConverterContent()
        {
            editConverter = (TextureTilemapConverter)EditorGUILayout.ObjectField("TextureTilemapConverter", editConverter, typeof(TextureTilemapConverter), false);
            SyncEditConverterState();

            if (editConverter == null)
            {
                EditorGUILayout.HelpBox("Assign a TextureTilemapConverter.", MessageType.Info);
                return;
            }

            converterSourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source Texture", converterSourceTexture, typeof(Texture2D), false);

            if (converterSourceTexture == null)
            {
                EditorGUILayout.HelpBox("Original texture not found. Reassign a matching texture.", MessageType.Warning);
                return;
            }

            SyncEditConverterGridFromCurrentTexture();

            if (!EnsureConverterGridReady())
            {
                return;
            }

            GUILayout.Space(4f);
            if (GUILayout.Button("Clear Sprites"))
            {
                ClearConverterSpriteGrid();
            }

            EditorGUILayout.Space(4f);
            DrawConverterGrid();

            if (GUILayout.Button("Save Converter"))
            {
                CreateAutoTile();
            }
        }

        private void DrawConverterGrid()
        {
            for (int y = 0; y < GridSize; y++)
            {
                EditorGUILayout.BeginHorizontal();

                for (int x = 0; x < GridSize; x++)
                {
                    int spriteIndex = y * GridSize + x;
                    converterSpriteGrid[spriteIndex] = (Sprite)EditorGUILayout.ObjectField(converterSpriteGrid[spriteIndex], typeof(Sprite), false, GUILayout.Width(SpriteFieldSize), GUILayout.Height(SpriteFieldSize));
                }

                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
