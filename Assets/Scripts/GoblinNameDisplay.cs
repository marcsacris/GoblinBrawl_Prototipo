using UnityEngine;
using TMPro;

/// <summary>
/// Script para mostrar el nombre y clase del goblin como texto 3D/UI debajo del sprite.
/// Asigna este script a cada GameObject de Goblin en la escena (Goblin_Player_0, Goblin_Enemy_0, etc).
/// Creará automáticamente objetos de texto con Canvas si no existen.
/// </summary>
public class GoblinNameDisplay : MonoBehaviour
{
    private Unit unit;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI classText;

    private void Start()
    {
        unit = GetComponent<Unit>();
        if (unit == null)
        {
            Debug.LogWarning($"GoblinNameDisplay: No Unit component found on {gameObject.name}");
            return;
        }

        // Crear o buscar Canvas para los textos
        CreateNameDisplay();
    }

    private void CreateNameDisplay()
    {
        // Buscar si ya existe un Canvas child
        Canvas existingCanvas = GetComponentInChildren<Canvas>();
        if (existingCanvas != null)
        {
            // Usar el canvas existente
            nameText = existingCanvas.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
                classText = existingCanvas.transform.Find("ClassText")?.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            // Crear Canvas local
            GameObject canvasObj = new GameObject("NameCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.transform.localPosition = new Vector3(0, -1.5f, 0);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2, 1);

            // Crear texto de nombre
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(canvasObj.transform, false);
            nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = unit.unitName;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontSize = 4;
            
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(2, 0.5f);
            nameRect.anchoredPosition = new Vector2(0, 0.25f);

            // Crear texto de clase
            GameObject classObj = new GameObject("ClassText");
            classObj.transform.SetParent(canvasObj.transform, false);
            classText = classObj.AddComponent<TextMeshProUGUI>();
            classText.text = GetClassLabel();
            classText.alignment = TextAlignmentOptions.Center;
            classText.fontSize = 3;
            
            RectTransform classRect = classObj.GetComponent<RectTransform>();
            classRect.sizeDelta = new Vector2(2, 0.4f);
            classRect.anchoredPosition = new Vector2(0, -0.25f);
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (nameText != null)
            nameText.text = unit.unitName;

        if (classText != null)
            classText.text = GetClassLabel();
    }

    private string GetClassLabel()
    {
        if (unit.classData != null)
            return $"{unit.classData.classEmoji} {unit.classData.className}".TrimStart();
        return "?";
    }
}
