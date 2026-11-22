using System.Collections.Generic;
using GateVR.Application;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GateVR.Presentation
{
    /// <summary>
    /// Lista de tareas creada según la práctica activa. No tiene filas fijas.
    /// </summary>
    public sealed class LessonTaskPanel
    {
        private readonly RectTransform _canvas;
        private readonly TMP_FontAsset _font;
        private readonly TextMeshProUGUI _progressText;
        private readonly RectTransform _fill;
        private readonly RectTransform _rows;
        private readonly List<GameObject> _rowObjects = new List<GameObject>();

        private LessonTaskPanel(
            RectTransform canvas,
            TMP_FontAsset font,
            Button understoodButton,
            TextMeshProUGUI progressText,
            RectTransform fill,
            RectTransform rows)
        {
            _canvas = canvas;
            _font = font;
            UnderstoodButton = understoodButton;
            _progressText = progressText;
            _fill = fill;
            _rows = rows;
        }

        public Button UnderstoodButton { get; private set; }

        public static LessonTaskPanel Create(Transform parent, TMP_FontAsset font)
        {
            Transform existing = parent.Find("LessonTasks");
            if (existing != null)
                Object.Destroy(existing.gameObject);

            RectTransform canvas = parent as RectTransform;
            GameObject root = new GameObject("LessonTasks", typeof(RectTransform));
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);

            Button understood = CreateButton(parent, "Btn_Entendido", "Entendido", new Vector2(300f, 4f), font);
            TextMeshProUGUI progress = CreateLabel(root.transform, "Txt_TaskProgress", "Tareas 0/0", new Vector2(0f, 0f), 22f, font, 280f);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(280f, 40f);
            rootRect.anchoredPosition = new Vector2(300f, -48f);
            progress.rectTransform.anchoredPosition = Vector2.zero;

            RectTransform track = CreateBar(root.transform, "TaskProgressTrack", new Vector2(0f, -36f), 280f, 14f, new Color(0.08f, 0.1f, 0.14f, 0.95f));
            RectTransform fill = CreateBar(track, "TaskProgressFill", Vector2.zero, 280f, 14f, new Color(0.35f, 0.82f, 0.42f, 1f));
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            fill.pivot = new Vector2(0f, 0.5f);

            GameObject rowsObject = new GameObject("TaskRows", typeof(RectTransform));
            rowsObject.layer = parent.gameObject.layer;
            rowsObject.transform.SetParent(root.transform, false);
            RectTransform rows = rowsObject.GetComponent<RectTransform>();
            rows.anchorMin = new Vector2(0.5f, 1f);
            rows.anchorMax = new Vector2(0.5f, 1f);
            rows.pivot = new Vector2(0.5f, 1f);
            rows.sizeDelta = new Vector2(280f, 0f);
            rows.anchoredPosition = new Vector2(0f, -56f);

            return new LessonTaskPanel(canvas, font, understood, progress, fill, rows);
        }

        public void Bind(IReadOnlyList<TaskStatus> tasks, int completed, int total)
        {
            int safeTotal = total < 0 ? 0 : total;
            int safeCompleted = completed < 0 ? 0 : completed;
            if (_progressText != null)
                _progressText.text = "Tareas " + safeCompleted + "/" + safeTotal;

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
                return;

            bool waitingRead = false;
            for (int i = 0; i < tasks.Count; i++)
            {
                TaskStatus task = tasks[i];
                if (task == null)
                    continue;

                if (task.Condition == TaskConditionKind.ReadRequirement && task.State == LessonTaskState.Pending)
                    waitingRead = true;

                _rowObjects.Add(CreateRow(task, i));
            }

            if (UnderstoodButton != null)
                UnderstoodButton.interactable = waitingRead;

            FitCanvas(tasks.Count);
        }

        private GameObject CreateRow(TaskStatus task, int index)
        {
            bool done = task.State == LessonTaskState.Completed;
            Color background = done
                ? new Color(0.12f, 0.42f, 0.24f, 0.96f)
                : new Color(0.1f, 0.14f, 0.2f, 0.94f);

            GameObject row = new GameObject("TaskRow_" + task.Id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            row.layer = _rows.gameObject.layer;
            row.transform.SetParent(_rows, false);
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(280f, 38f);
            rect.anchoredPosition = new Vector2(0f, -index * 42f);

            Image image = row.GetComponent<Image>();
            image.sprite = UiSprite();
            image.color = background;
            image.raycastTarget = false;

            string mark = done ? "Lista" : "Pendiente";
            CreateLabel(
                row.transform,
                "Label",
                task.Order + ". " + task.Title + "   " + mark,
                Vector2.zero,
                18f,
                _font,
                264f);
            return row;
        }

        private void FitCanvas(int taskCount)
        {
            if (_canvas == null)
                return;

            float height = Mathf.Max(_canvas.sizeDelta.y, 700f + Mathf.Max(0, taskCount - 5) * 42f);
            float width = Mathf.Max(_canvas.sizeDelta.x, 980f);
            if (_canvas.sizeDelta.x < width || _canvas.sizeDelta.y < height)
                _canvas.sizeDelta = new Vector2(width, height);

            BoxCollider box = _canvas.GetComponent<BoxCollider>();
            if (box != null)
                box.size = new Vector3(_canvas.sizeDelta.x, _canvas.sizeDelta.y, Mathf.Max(box.size.z, 20f));
        }

        private static RectTransform CreateBar(Transform parent, string name, Vector2 position, float width, float height, Color color)
        {
            GameObject bar = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bar.layer = parent.gameObject.layer;
            bar.transform.SetParent(parent, false);
            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = position;
            Image image = bar.GetComponent<Image>();
            image.sprite = UiSprite();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static TextMeshProUGUI CreateLabel(
            Transform parent,
            string name,
            string text,
            Vector2 position,
            float fontSize,
            TMP_FontAsset font,
            float width)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.layer = parent.gameObject.layer;
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, 32f);
            rect.anchoredPosition = position;
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            if (font != null)
                label.font = font;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, TMP_FontAsset font)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Button found = existing.GetComponent<Button>();
                if (found != null)
                    return found;
            }

            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.layer = parent.gameObject.layer;
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(180f, 36f);
            rect.anchoredPosition = position;

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = UiSprite();
            image.color = new Color(0.18f, 0.34f, 0.48f, 0.96f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            CreateLabel(buttonObject.transform, "Text", label, Vector2.zero, 20f, font, 168f);
            return button;
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
