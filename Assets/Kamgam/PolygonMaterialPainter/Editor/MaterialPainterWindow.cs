using UnityEngine;
using UnityEditor;
using System.Globalization;
using UnityEditor.EditorTools;

namespace Kamgam.PolygonMaterialPainter
{
    public class MaterialPainterWindow : EditorWindow
    {
        public static MaterialPainterWindow Instance;

        MaterialPainterTool _tool;
        bool _waitForTheToolToStart;

        public static void ShowWindow()
        {
            bool utility = false;
#if UNITY_EDITOR_OSX
            // Sadly dockable windows on the mac are sent to the background.
            // That's why we use the utility as a workaround.
            utility = true;
#endif
            ShowWindow(utility);
        }

        public static void ShowWindow(bool utility)
        {
            MaterialPainterWindow window = EditorWindow.GetWindow<MaterialPainterWindow>(utility, "Polygon Material Painter", focus: true);
            Instance = window;

            if (utility)
            {
                window.ShowUtility();
            }
            else
            {
                window.Show();
            }
        }

        void OnEnable()
        {
            Instance = this;

#if UNITY_2020_2_OR_NEWER
            ToolManager.activeToolChanged -= onToolChanged;
            ToolManager.activeToolChanged += onToolChanged;

#else
            EditorTools.activeToolChanged -= onToolChanged;
            EditorTools.activeToolChanged += onToolChanged;
#endif
        }

        private void onToolChanged()
        {
            EditorApplication.delayCall += () =>
            {
                Repaint();
            };
        }

        void OnDestroy()
        {
            if (MaterialPainterTool.Instance != null)
                MaterialPainterTool.Instance.ExitTool();
        }


        void OnGUI()
        {
            // Forward key events to tool
            if (Event.current.isKey)
            {
                _tool.OnGUIWithEvent(Event.current);
            }

            if (_waitForTheToolToStart && !MaterialPainterToolActiveState.IsActive)
            {
                DrawLabel("Loading ..");
                return;
            }

            if (!MaterialPainterToolActiveState.IsActive && !_waitForTheToolToStart)
            {
                DrawLabel("The tool is not active.", bold: true);
                DrawLabel("Would you like to start it now ?");
                if (GUILayout.Button("Start"))
                {
                    _waitForTheToolToStart = true;
                    EditorApplication.delayCall += () =>
                    {
                        MaterialPainterTool.Activate();
                    };
                }
                return;
            }
            else if(MaterialPainterToolActiveState.IsActive)
            {
                _waitForTheToolToStart = false;
            }

            if (_tool == null)
                _tool = MaterialPainterTool.Instance;

            if (_tool == null)
                return;


            drawWindowContent();
        }

        void drawWindowContent()
        {
            GUILayout.BeginHorizontal();
            int mode = (int)_tool.GetMode();
            mode = GUILayout.Toolbar(mode, new GUIContent[] {
                EditorGUIUtility.IconContent("d_FilterByType@2x", "Pick Object"),
                EditorGUIUtility.IconContent("d_pick@2x", "Paint Selection"),
            }, GUILayout.Height(26));
            _tool.SetMode((MaterialPainterTool.Mode)mode);
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_clear@2x", "Stops the tool and closes this window (if not docked)."), new GUILayoutOption[] { GUILayout.Width(22) , GUILayout.Height(26) }))
            {
                _tool.ExitTool();

                if (!docked)
                    Close();

                GUILayout.EndHorizontal();
                return;
            }
            GUILayout.EndHorizontal();

            // Content
            switch (_tool.GetMode())
            {
                case MaterialPainterTool.Mode.PickObject:
                    _tool.SetMode(MaterialPainterTool.Mode.PickObject);
                    drawPickObjectsWindowContent();
                    break;

                case MaterialPainterTool.Mode.PaintSelection:
                    _tool.SetMode(MaterialPainterTool.Mode.PaintSelection);
                    drawSelectWindowContentGUI();
                    break;

                default:
                    break;
            }
        }

        void drawPickObjectsWindowContent()
        {
            DrawLabel("Select objects", bold: true);
            DrawLabel("Select one or more objects to paint on.", "This is useful to avoid selecting background meshes by accident. You can return to this step at any time and add or remove objects.", wordwrap: true);

            GUILayout.BeginHorizontal();
            if (DrawButton("Reset", "Clears the current selection, deselects any object and resets all configurations to default."))
            {
                _tool.Reset();
            }
            GUI.enabled = _tool.HasSelectedObjects();
            if (DrawButton("Start Painting", "Switches to the painting tab (you can also switch via the tabs buttons at the top)."))
            {
                _tool.SetMode(MaterialPainterTool.Mode.PaintSelection);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        void drawSelectWindowContentGUI()
        {
            DrawLabel("Brush", "Paint on the objects to select polyons.", bold: true);

            if (!_tool.HasSelectedObjects())
            {
                DrawLabel("Please select some objects first!");
                return;
            }

            GUI.enabled = _tool.HasSelectedObjects();

            _tool.SetCullBack(!GUILayout.Toggle(!_tool.GetCullBack(), new GUIContent("X-Ray", "X-Ray mode allows you to select front and back facing triangles at the same time.")));

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Brush Size:", "Reduce the brush size to 0 to select only one triangle at a time.\n\nYou can also use SHIFT + MOUSE WHEEL to change the brush size."), GUILayout.MaxWidth(75));
            _tool.SetBrushSize(GUILayout.HorizontalSlider(_tool.GetBrushSize(), 0f, 1f));
            GUILayout.Label((_tool.GetBrushSize() * 10).ToString("f1", CultureInfo.InvariantCulture), GUILayout.MaxWidth(22));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Brush Depth:", "Brush depth defines how far into the object the selection will go. This helps to avoid selecting background polygons by accident. If you want infinite depth then simply turn on X-Ray."), GUILayout.MaxWidth(75));
            _tool.SetBrushDepth(GUILayout.HorizontalSlider(_tool.GetBrushDepth(), 0f, 2f));
            _tool.SetBrushDepth(EditorGUILayout.FloatField(_tool.GetBrushDepth(), GUILayout.MaxWidth(32)));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.enabled = _tool.GetLastSelectedTriangle() != null;
            if (DrawButton("Select Linked", "Selects all triangles which are connected to the last selected triangle.\n\nHold SHIFT while clicking the button to deselect linked.\n\nHINT: You can press S or SHIFT + S while selecting to trigger this action."))
            {
                _tool.AddLinkedToSelection(remove: Event.current.shift);
                _tool.RecordSelectionForUndo(0.2f);
                SceneView.RepaintAll();
                SceneView.lastActiveSceneView.Focus();
            }
            if (DrawButton("Deselect Linked", "Deselects all triangles which are connected to the last selected triangle."))
            {
                _tool.AddLinkedToSelection(remove: true);
                _tool.RecordSelectionForUndo(0.2f);
                SceneView.RepaintAll();
                SceneView.lastActiveSceneView.Focus();
            }
            _tool.SetLimitLinkedSearchToSubMesh(
                EditorGUILayout.ToggleLeft(
                    new GUIContent("Limit", "Enable to limit selection to a single sub mesh.\nIt will use the sub mesh of the last selected triangle."),
                    _tool.GetLimitLinkedSearchToSubMesh(),
                    GUILayout.Width(50)
                )
            );
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            if (DrawButton("Clear Selection", "Clears the current selection."))
            {
                _tool.ClearSelection();
                _tool.RecordSelectionForUndo(0.2f);
                SceneView.RepaintAll();
                SceneView.lastActiveSceneView.Focus();
            }


            GUILayout.Space(10);
            DrawLabel("Materials", bold: true);
            var color = GUI.skin.label.normal.textColor;
            color.a = _tool.HasSelectedTriangles() ? 0.5f : 1f;
            DrawLabel("Click 'Assign' again to reuse the last selection.", bold: false, color: color);

            for (int i = 0; i < _tool.MaterialReferences.References.Count; i++)
            {
                GUILayout.BeginHorizontal();
                // Material
                var mat = _tool.MaterialReferences.GetAt(i);
                mat = (Material) EditorGUILayout.ObjectField(mat, typeof(Material), allowSceneObjects: false);
                _tool.MaterialReferences.SetAt(i, mat);
                // Button
                GUI.enabled = mat != null;
                if (GUILayout.Button("Assign " + (mat != null ? mat.name : ""), GUILayout.Width(200)))
                {
                    _tool.AssignMaterialToSelection(mat);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset All"))
            {
                _tool.ResetAll();
            }
            GUI.enabled = _tool.IsUsingEditedMesh();
            if (GUILayout.Button("Reset Selected", GUILayout.Width(200)))
            {
                _tool.ResetSelected();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            if (!_tool.HasOriginalMesh())
            {
                EditorGUILayout.HelpBox(new GUIContent("The based mesh already is an edited mesh. Therefore it may not be possible to revert all changes."));
            }

            if (_tool.MaterialReferences.HasChanged)
            {
                _tool.MaterialReferences.HasChanged = false;
                _tool.MaterialReferences.Save();
            }

            GUILayout.Space(10);
            DrawLabel("Options", bold: true);
             
            _tool.MergeSameMaterialMesh = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    "Merge same materials",
                    "If enabled then the selection will be merged with submeshes which have the same material.\n" +
                    "If disabled then the selection will always be added as a NEW submesh."),
                _tool.MergeSameMaterialMesh
            );
            
            _tool.ApplyChangesToSelectedObject = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    "Apply changes to selected object",
                    "Usually you want this enabled. If disabled then the changes are only applied to the new mesh asset but that asset is then not linked to the currently selected object."),
                _tool.ApplyChangesToSelectedObject
            );

            _tool.DeleteNewMeshAfterResetToOriginal = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    "Delete new mesh after reset to original",
                    "After resetting to the original mesh the new mehs usually is no longer needed. Enable this to auto-delete it."),
                _tool.DeleteNewMeshAfterResetToOriginal
            );

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Undo"))
            {
                _tool.Undo();
            }
            if (GUILayout.Button("Redo"))
            {
                _tool.Redo();
            }
            if (GUILayout.Button("Reset to Original"))
            {
                _tool.ResetToOriginalMesh();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save current as new original state"))
            {
                if (UnityEditor.EditorUtility.DisplayDialog(
                    "Are you sure?",
                    "This will clear to stored original state and save the current model state as the new original state.\n\n" +
                    "Your old original state will be lost."
                    , "Yes", "Abort"))
                {
                    _tool.MakeCurrentOriginal();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            if (GUILayout.Button("Export as .OBJ"))
            {
                _tool.ExportAsObj();
            }
        }


#region GUI Helpers
        public static bool DrawButton(string text, string tooltip = null, string icon = null, GUIStyle style = null, params GUILayoutOption[] options)
        {
            GUIContent content;

            // icon
            if (!string.IsNullOrEmpty(icon))
                content = EditorGUIUtility.IconContent(icon);
            else
                content = new GUIContent();

            // text
            content.text = text;

            // tooltip
            if (!string.IsNullOrEmpty(tooltip))
                content.tooltip = tooltip;

            if (style == null)
                style = new GUIStyle(GUI.skin.button);

            return GUILayout.Button(content, style, options);
        }

        public static void BeginHorizontalIndent(int indentAmount = 10, bool beginVerticalInside = true)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indentAmount);
            if (beginVerticalInside)
                GUILayout.BeginVertical();
        }

        public static void EndHorizontalIndent(float indentAmount = 10, bool begunVerticalInside = true, bool bothSides = false)
        {
            if (begunVerticalInside)
                GUILayout.EndVertical();
            if (bothSides)
                GUILayout.Space(indentAmount);
            GUILayout.EndHorizontal();
        }

        public static void DrawLabel(string text, string tooltip = null, Color? color = null, bool bold = false, bool wordwrap = true, bool richText = true, Texture icon = null, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (!color.HasValue)
                color = GUI.skin.label.normal.textColor;

            if (style == null)
                style = new GUIStyle(GUI.skin.label);
            if (bold)
                style.fontStyle = FontStyle.Bold;
            else
                style.fontStyle = FontStyle.Normal;

            style.normal.textColor = color.Value;
            style.hover.textColor = color.Value;
            style.wordWrap = wordwrap;
            style.richText = richText;
            style.imagePosition = ImagePosition.ImageLeft;

            var content = new GUIContent(text);
            if (tooltip != null)
                content.tooltip = tooltip;
            if (icon != null)
            {
                GUILayout.Space(16);
                var position = GUILayoutUtility.GetRect(content, style, options);
                GUI.DrawTexture(new Rect(position.x - 16, position.y, 16, 16), icon);
                GUI.Label(position, content, style);
            }
            else
            {
                GUILayout.Label(content, style, options);
            }
        }
#endregion
    }
}
