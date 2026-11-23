using System.Collections.Generic;
using GateVR.Application;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GateVR.Presentation
{
    /// <summary>
    /// Panel de lección en World Space. Las filas se crean según la lista real de tareas.
    /// </summary>
    public sealed class LessonTaskPanel
    {
        private const float Width = 820f;
        private const float RowHeight = 84f;
        private const float BaseHeight = 1054f;

        private readonly RectTransform _canvas;
        private readonly TMP_FontAsset _font;
        private readonly TextMeshProUGUI _moduleText;
        private readonly TextMeshProUGUI _truthText;
        private readonly TextMeshProUGUI _progressText;
        private readonly RectTransform _fill;
        private readonly RectTransform _rows;
        private readonly LayoutElement _progressElement;
        private readonly LayoutElement _actionsElement;
        private readonly RectTransform _moduleRow;
        private readonly Dictionary<GateType, Button> _gateButtons = new Dictionary<GateType, Button>();
        private readonly Dictionary<string, Button> _navButtons = new Dictionary<string, Button>();
        private readonly List<GameObject> _rowObjects = new List<GameObject>();
        private readonly List<GameObject> _moduleButtons = new List<GameObject>();
        private int _taskCount = 3;
        private int _moduleGeneration;
        private float _moduleExtra;

        private LessonTaskPanel(
            RectTransform canvas,
            TMP_FontAsset font,
            TextMeshProUGUI moduleText,
            TextMeshProUGUI truthText,
            TextMeshProUGUI progressText,
            RectTransform fill,
            RectTransform rows,
            LayoutElement progressElement,
            LayoutElement actionsElement,
            RectTransform moduleRow,
            IEnumerable<KeyValuePair<GateType, Button>> gateButtons)
        {
            _canvas = canvas;
            _font = font;
            _moduleText = moduleText;
            _truthText = truthText;
            _progressText = progressText;
            _fill = fill;
            _rows = rows;
            _progressElement = progressElement;
            _actionsElement = actionsElement;
            _moduleRow = moduleRow;
            if (gateButtons == null)
                return;

            foreach (KeyValuePair<GateType, Button> pair in gateButtons)
                _gateButtons[pair.Key] = pair.Value;
        }

        public static LessonTaskPanel Mount(
            RectTransform canvas,
            TMP_FontAsset font,
            TextMeshProUGUI title,
            TextMeshProUGUI objective,
            TextMeshProUGUI condition,
            TextMeshProUGUI feedback,
            Button checkButton,
            Button nextButton,
            Button resetButton,
            ref Button andButton,
            ref Button orButton,
            ref Button notButton)
        {
            if (canvas == null)
                return null;

            Transform previous = canvas.Find("PanelContent");
            if (previous != null)
                Object.Destroy(previous.gameObject);

            Transform oldSurface = canvas.Find("PanelSurface");
            if (oldSurface != null)
                Object.Destroy(oldSurface.gameObject);

            DestroyIfPresent(canvas, "Btn_Entendido");
            DestroyIfPresent(canvas, "Txt_GateSelector");
            DestroyIfPresent(canvas, "LessonTasks");

            Place(canvas);

            Image surface = CreateStretch(canvas, "PanelSurface", new Color(0.07f, 0.1f, 0.15f, 0.96f));
            surface.raycastTarget = true;

            RectTransform content = CreateChild(canvas, "PanelContent");
            Stretch(content);
            VerticalLayoutGroup column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(28, 28, 24, 24);
            column.spacing = 16f;
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            RectTransform header = CreateSection(content, "SeccionEncabezado", 304f, new Color(0.13f, 0.18f, 0.26f, 0.98f));
            TextMeshProUGUI module = CreateLabel(header, "Txt_Module", "Fundamentos", 24f, font, FontStyles.Normal, new Color(0.72f, 0.84f, 0.95f));
            RectTransform nav = CreateRow(header, "FilaSubmodulos", 68f);
            Button fundamentals = EnsureGateButton(nav, null, "Btn_Fundamentos", "Fundamentos", font);
            Button application = EnsureGateButton(nav, null, "Btn_Aplicacion", "Aplicación", font);
            Button diagnosis = EnsureGateButton(nav, null, "Btn_Diagnostico", "Diagnóstico", font);
            FitNavCaption(fundamentals);
            FitNavCaption(application);
            FitNavCaption(diagnosis);
            AdoptText(header, title, 36f, font, FontStyles.Bold, 48f);
            AdoptText(header, objective, 24f, font, FontStyles.Normal, 96f);

            RectTransform instructions = CreateSection(content, "SeccionInstrucciones", 290f, new Color(0.1f, 0.15f, 0.22f, 0.98f));
            AdoptText(instructions, condition, 30f, font, FontStyles.Bold, 40f);
            TextMeshProUGUI truth = CreateLabel(instructions, "Txt_Truth", string.Empty, 22f, font, FontStyles.Normal, Color.white);
            truth.alignment = TextAlignmentOptions.TopLeft;
            LayoutElement truthElement = truth.GetComponent<LayoutElement>();
            truthElement.preferredHeight = 118f;
            truthElement.minHeight = 118f;
            AdoptText(instructions, feedback, 22f, font, FontStyles.Normal, 96f);

            RectTransform progress = CreateSection(content, "SeccionTareas", 236f, new Color(0.09f, 0.14f, 0.2f, 0.98f));
            LayoutElement progressElement = progress.GetComponent<LayoutElement>();
            TextMeshProUGUI progressLabel = CreateLabel(progress, "Txt_TaskProgress", "Tareas 0/0", 26f, font, FontStyles.Bold, Color.white);
            RectTransform track = CreateBar(progress, "TaskProgressTrack", 12f, new Color(0.05f, 0.07f, 0.1f, 1f));
            RectTransform fill = CreateBar(track, "TaskProgressFill", 12f, new Color(0.32f, 0.78f, 0.48f, 1f));
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            fill.pivot = new Vector2(0f, 0.5f);

            RectTransform rows = CreateChild(progress, "TaskRows");
            VerticalLayoutGroup rowLayout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childAlignment = TextAnchor.UpperCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;
            LayoutElement rowsElement = rows.gameObject.AddComponent<LayoutElement>();
            rowsElement.flexibleWidth = 1f;
            rowsElement.preferredHeight = RowHeight;

            RectTransform actions = CreateSection(content, "SeccionAcciones", 268f, new Color(0.12f, 0.16f, 0.23f, 0.98f));
            LayoutElement actionsElement = actions.GetComponent<LayoutElement>();
            RectTransform lessonButtons = CreateRow(actions, "FilaLeccion", 68f);
            AdoptButton(lessonButtons, checkButton, font);
            AdoptButton(lessonButtons, nextButton, font);
            AdoptButton(lessonButtons, resetButton, font);

            CreateLabel(actions, "Txt_GateSelector", "Compuerta", 22f, font, FontStyles.Normal, new Color(0.75f, 0.84f, 0.92f));
            RectTransform gateRow = CreateRow(actions, "FilaCompuertas", 56f);
            RectTransform gateRowExtra = CreateRow(actions, "FilaCompuertasExtra", 56f);
            andButton = EnsureGateButton(gateRow, andButton, "Btn_AND", "AND", font);
            orButton = EnsureGateButton(gateRow, orButton, "Btn_OR", "OR", font);
            notButton = EnsureGateButton(gateRow, notButton, "Btn_NOT", "NOT", font);
            Button nandButton = EnsureGateButton(gateRow, null, "Btn_NAND", "NAND", font);
            Button norButton = EnsureGateButton(gateRowExtra, null, "Btn_NOR", "NOR", font);
            Button xorButton = EnsureGateButton(gateRowExtra, null, "Btn_XOR", "XOR", font);
            Button xnorButton = EnsureGateButton(gateRowExtra, null, "Btn_XNOR", "XNOR", font);
            var gateButtons = new Dictionary<GateType, Button>
            {
                { GateType.AND, andButton },
                { GateType.OR, orButton },
                { GateType.NOT, notButton },
                { GateType.NAND, nandButton },
                { GateType.NOR, norButton },
                { GateType.XOR, xorButton },
                { GateType.XNOR, xnorButton }
            };

            RectTransform moduleRow = CreateRow(actions, "FilaModulos", 0f);
            moduleRow.gameObject.SetActive(false);

            LessonTaskPanel panel = new LessonTaskPanel(
                canvas,
                font,
                module,
                truth,
                progressLabel,
                fill,
                rows,
                progressElement,
                actionsElement,
                moduleRow,
                gateButtons);
            panel.RememberNav("Fundamentos", fundamentals);
            panel.RememberNav("Aplicación Real", application);
            panel.RememberNav("Diagnóstico", diagnosis);
            panel.Resize(3);
            return panel;
        }

        public Button NavButton(string moduleName)
        {
            Button button;
            if (moduleName != null && _navButtons.TryGetValue(moduleName, out button))
                return button;

            return null;
        }

        private static void FitNavCaption(Button button)
        {
            if (button == null)
                return;

            TextMeshProUGUI caption = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (caption == null)
                return;

            caption.fontSize = 20f;
            caption.alignment = TextAlignmentOptions.Center;
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            caption.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void RememberNav(string moduleName, Button button)
        {
            if (button != null)
                _navButtons[moduleName] = button;
        }

        public Button GateButton(GateType type)
        {
            Button button;
            if (_gateButtons.TryGetValue(type, out button))
                return button;

            return null;
        }

        public void SetStationModules(IReadOnlyList<string> captions, int focusedIndex, System.Action<int> onSelect)
        {
            for (int i = _moduleButtons.Count - 1; i >= 0; i--)
            {
                if (_moduleButtons[i] != null)
                    Object.Destroy(_moduleButtons[i]);
            }

            _moduleButtons.Clear();
            int count = captions == null ? 0 : captions.Count;
            bool show = count > 1 && _moduleRow != null;
            if (_moduleRow != null)
                _moduleRow.gameObject.SetActive(show);

            _moduleExtra = show ? 84f : 0f;
            if (_actionsElement != null)
                _actionsElement.preferredHeight = 268f + _moduleExtra;

            LayoutElement rowElement = _moduleRow != null ? _moduleRow.GetComponent<LayoutElement>() : null;
            if (rowElement != null)
            {
                rowElement.preferredHeight = show ? 76f : 0f;
                rowElement.minHeight = show ? 76f : 0f;
            }

            if (show)
            {
                int safeFocus = focusedIndex < 0 ? 0 : focusedIndex;
                _moduleGeneration++;
                for (int i = 0; i < count; i++)
                {
                    int moduleIndex = i;
                    string caption = string.IsNullOrEmpty(captions[i]) ? "Comp. " + (i + 1) : captions[i];
                    Button button = EnsureGateButton(_moduleRow, null, "Btn_Modulo_" + _moduleGeneration + "_" + (i + 1), caption, _font);
                    TextMeshProUGUI buttonLabel = button.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (buttonLabel != null)
                        buttonLabel.fontSize = 18f;

                    if (onSelect != null)
                        button.onClick.AddListener(delegate { onSelect(moduleIndex); });

                    Image image = button.targetGraphic as Image;
                    if (image != null)
                    {
                        image.color = i == safeFocus
                            ? new Color(0.95f, 0.72f, 0.2f, 1f)
                            : new Color(0.16f, 0.24f, 0.36f, 1f);
                    }

                    _moduleButtons.Add(button.gameObject);
                }
            }

            Resize(_taskCount);
        }

        public void SetModule(string moduleName)
        {
            if (_moduleText != null)
                _moduleText.text = string.IsNullOrEmpty(moduleName) ? "Fundamentos" : moduleName;
        }

        public void SetTruthTable(string text)
        {
            if (_truthText != null)
                _truthText.text = text ?? string.Empty;
        }

        public void Bind(IReadOnlyList<TaskStatus> tasks, int completed, int total)
        {
            int safeTotal = total < 0 ? 0 : total;
            int safeCompleted = completed < 0 ? 0 : completed;
            if (_progressText != null)
                _progressText.text = "Tareas  " + safeCompleted + "/" + safeTotal;

            if (_fill != null)
            {
                float ratio = safeTotal == 0 ? 0f : Mathf.Clamp01((float)safeCompleted / safeTotal);
                _fill.anchorMax = new Vector2(ratio, 1f);
            }

            for (int i = _rowObjects.Count - 1; i >= 0; i--)
            {
                if (_rowObjects[i] != null)
                    Object.Destroy(_rowObjects[i]);
            }

            _rowObjects.Clear();
            if (tasks == null || _rows == null)
            {
                Resize(0);
                return;
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i] == null)
                    continue;

                _rowObjects.Add(CreateRow(tasks[i]));
            }

            LayoutElement rowsElement = _rows.GetComponent<LayoutElement>();
            if (rowsElement != null)
                rowsElement.preferredHeight = Mathf.Max(RowHeight, _rowObjects.Count * (RowHeight + 8f));

            Resize(_rowObjects.Count);
        }

        private void Resize(int taskCount)
        {
            _taskCount = taskCount < 0 ? 0 : taskCount;
            if (_progressElement != null)
                _progressElement.preferredHeight = 96f + _taskCount * (RowHeight + 8f);

            if (_canvas == null)
                return;

            float height = BaseHeight + _moduleExtra + _taskCount * (RowHeight + 8f);
            _canvas.sizeDelta = new Vector2(Width, height);
            _canvas.localScale = new Vector3(0.00115f, 0.00115f, 0.00115f);

            BoxCollider box = _canvas.GetComponent<BoxCollider>();
            if (box != null)
            {
                box.center = Vector3.zero;
                box.size = new Vector3(Width, height, 20f);
            }

            Place(_canvas);
        }

        private GameObject CreateRow(TaskStatus task)
        {
            Color background;
            string mark;
            if (task.State == LessonTaskState.Completed)
            {
                background = new Color(0.14f, 0.42f, 0.28f, 0.98f);
                mark = "Completada";
            }
            else if (task.State == LessonTaskState.Invalidated)
            {
                background = new Color(0.55f, 0.32f, 0.12f, 0.98f);
                mark = "Invalidada";
            }
            else
            {
                background = new Color(0.16f, 0.22f, 0.3f, 0.98f);
                mark = "Pendiente";
            }

            GameObject row = new GameObject("TaskRow_" + task.Id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            row.layer = _rows.gameObject.layer;
            row.transform.SetParent(_rows, false);
            LayoutElement element = row.GetComponent<LayoutElement>();
            element.preferredHeight = RowHeight;
            element.minHeight = RowHeight;
            element.flexibleWidth = 1f;

            Image image = row.GetComponent<Image>();
            image.sprite = UiSprite();
            image.color = background;
            image.raycastTarget = false;

            RectTransform rowRect = row.GetComponent<RectTransform>();
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 8, 8);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            RectTransform textColumn = CreateChild(rowRect, "Texto");
            VerticalLayoutGroup textLayout = textColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 2f;
            textLayout.childAlignment = TextAnchor.MiddleLeft;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;
            LayoutElement textElement = textColumn.gameObject.AddComponent<LayoutElement>();
            textElement.flexibleWidth = 1f;
            textElement.minWidth = 360f;

            TextMeshProUGUI title = CreateLabel(
                textColumn,
                "Titulo",
                task.Order + ".  " + task.Title,
                24f,
                _font,
                FontStyles.Bold,
                Color.white);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            LayoutElement titleElement = title.GetComponent<LayoutElement>();
            titleElement.preferredHeight = 30f;
            titleElement.minHeight = 28f;

            TextMeshProUGUI detail = CreateLabel(
                textColumn,
                "Detalle",
                task.Description,
                18f,
                _font,
                FontStyles.Normal,
                new Color(0.86f, 0.91f, 0.95f));
            detail.alignment = TextAlignmentOptions.TopLeft;
            LayoutElement detailElement = detail.GetComponent<LayoutElement>();
            detailElement.preferredHeight = 32f;
            detailElement.minHeight = 28f;

            TextMeshProUGUI status = CreateLabel(
                rowRect,
                "Estado",
                mark,
                20f,
                _font,
                FontStyles.Bold,
                Color.white);
            status.alignment = TextAlignmentOptions.Center;
            LayoutElement statusElement = status.GetComponent<LayoutElement>();
            statusElement.preferredWidth = 168f;
            statusElement.minWidth = 150f;
            statusElement.flexibleWidth = 0f;
            statusElement.preferredHeight = 56f;
            return row;
        }

        private static void Place(RectTransform canvas)
        {
            Camera camera = Camera.main;
            Vector3 right = camera != null ? camera.transform.right : Vector3.right;
            right.y = 0f;
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.right;
            right.Normalize();

            float tableEdge = 1f;
            GameObject mesa = GameObject.Find("Mesa");
            if (mesa != null)
            {
                Renderer renderer = mesa.GetComponent<Renderer>();
                if (renderer != null)
                    tableEdge = Mathf.Max(Mathf.Abs(renderer.bounds.min.x), Mathf.Abs(renderer.bounds.max.x));
            }

            float width = Mathf.Abs(canvas.sizeDelta.x * canvas.lossyScale.x);
            if (width < 0.2f)
                width = Width * 0.00115f;

            // El borde cercano del panel queda 12 cm fuera de la mesa,
            // para no tapar el LED ni el extremo derecho de la placa.
            float center = tableEdge + 0.12f + width * 0.5f;
            Vector3 position = right * center;
            position.y = 1.22f;
            position.z = 0.02f;
            canvas.position = position;
            if (camera == null)
                return;

            Vector3 away = position - camera.transform.position;
            if (away.sqrMagnitude > 0.001f)
                canvas.rotation = Quaternion.LookRotation(away, Vector3.up);
        }

        private static RectTransform CreateSection(RectTransform parent, string name, float height, Color color)
        {
            RectTransform section = CreateChild(parent, name);
            Image image = section.gameObject.AddComponent<Image>();
            image.sprite = UiSprite();
            image.color = color;
            image.raycastTarget = false;
            VerticalLayoutGroup layout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 12, 12);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            LayoutElement element = section.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleWidth = 1f;
            return section;
        }

        private static RectTransform CreateRow(RectTransform parent, string name, float height)
        {
            RectTransform row = CreateChild(parent, name);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleWidth = 1f;
            return row;
        }

        private static void AdoptText(RectTransform parent, TextMeshProUGUI text, float size, TMP_FontAsset font, FontStyles style, float height)
        {
            if (text == null)
                return;

            text.transform.SetParent(parent, false);
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            if (font != null)
                text.font = font;

            LayoutElement element = text.GetComponent<LayoutElement>();
            if (element == null)
                element = text.gameObject.AddComponent<LayoutElement>();

            element.preferredHeight = height;
            element.minHeight = height * 0.6f;
            element.flexibleWidth = 1f;
        }

        private static void AdoptButton(RectTransform parent, Button button, TMP_FontAsset font)
        {
            if (button == null)
                return;

            button.transform.SetParent(parent, false);
            Image image = button.targetGraphic as Image;
            if (image == null)
                image = button.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                if (image.color.a < 0.2f)
                    image.color = new Color(0.18f, 0.32f, 0.48f, 1f);
            }

            LayoutElement element = button.GetComponent<LayoutElement>();
            if (element == null)
                element = button.gameObject.AddComponent<LayoutElement>();

            element.preferredHeight = 64f;
            element.minHeight = 64f;
            element.flexibleWidth = 1f;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.fontSize = 24f;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
                if (font != null)
                    label.font = font;
            }
        }

        private static Button EnsureGateButton(RectTransform parent, Button current, string name, string label, TMP_FontAsset font)
        {
            Button button = current;
            if (button == null)
            {
                Transform existing = parent.Find(name);
                if (existing != null)
                    button = existing.GetComponent<Button>();
            }

            if (button == null)
            {
                GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                buttonObject.layer = parent.gameObject.layer;
                buttonObject.transform.SetParent(parent, false);
                Image image = buttonObject.GetComponent<Image>();
                image.sprite = UiSprite();
                image.color = new Color(0.16f, 0.24f, 0.36f, 1f);
                image.raycastTarget = true;
                button = buttonObject.GetComponent<Button>();
                button.targetGraphic = image;
                TextMeshProUGUI caption = CreateLabel(buttonObject.GetComponent<RectTransform>(), "Text", label, 24f, font, FontStyles.Bold, Color.white);
                Stretch(caption.rectTransform);
            }
            else
            {
                button.transform.SetParent(parent, false);
            }

            AdoptButton(parent, button, font);
            return button;
        }

        private static RectTransform CreateBar(RectTransform parent, string name, float height, Color color)
        {
            RectTransform bar = CreateChild(parent, name);
            Image image = bar.gameObject.AddComponent<Image>();
            image.sprite = UiSprite();
            image.color = color;
            image.raycastTarget = false;
            LayoutElement element = bar.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleWidth = 1f;
            return bar;
        }

        private static TextMeshProUGUI CreateLabel(
            RectTransform parent,
            string name,
            string text,
            float size,
            TMP_FontAsset font,
            FontStyles style,
            Color color)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.layer = parent.gameObject.layer;
            labelObject.transform.SetParent(parent, false);
            LayoutElement element = labelObject.GetComponent<LayoutElement>();
            element.preferredHeight = size + 16f;
            element.flexibleWidth = 1f;
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            if (font != null)
                label.font = font;
            return label;
        }

        private static Image CreateStretch(RectTransform parent, string name, Color color)
        {
            RectTransform rect = CreateChild(parent, name);
            Stretch(rect);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiSprite();
            image.color = color;
            return image;
        }

        private static RectTransform CreateChild(RectTransform parent, string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.layer = parent.gameObject.layer;
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void DestroyIfPresent(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
                Object.Destroy(child.gameObject);
        }

        private static Sprite _sprite;

        private static Sprite UiSprite()
        {
            if (_sprite != null)
                return _sprite;

            _sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            if (_sprite != null)
                return _sprite;

            Texture2D texture = Texture2D.whiteTexture;
            _sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return _sprite;
        }
    }
}
