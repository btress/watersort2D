using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BottleController : MonoBehaviour
{
    public Color[] bottleColors;
    public SpriteRenderer bottleMaskSR;

    public AnimationCurve ScaleAndRontationMultiplierCurve;
    public AnimationCurve FillAmountCurve;

    public AnimationCurve RotationSpeedMultiplier;

    public float[] fillAmounts;
    public float[] rotationValues;

    private int rotationIndex = 0;

    [Range(0, 4)]
    public int numberOfColorsInBottle = 4;

    public Color topColor;
    public int numberOfTopColorLayers = 1;

    public BottleController BottleControllerRef;
    public bool justThiBottle = false;
    public int numberOfColorsToTransfer = 0;

    public Transform leftRotationPoint;
    public Transform rightRotationPoint;
    private Transform chosenRotationPoint;

    private float directionMultiplier = 1.0f;

    Vector3 originalPosition;
    Vector3 StartPosition;
    Vector3 endPosition;

    public LineRenderer lineRenderer;

    public float timeToRotate = 1.0f;

    // ====== MỚI ======
    [HideInInspector] public bool isLocked = false;   // ống đã hoàn thành -> không cho bấm nữa

    int originalBottleOrder;
    int originalMaskOrder;

    SpriteRenderer bottleSR;
    SpriteRenderer glowSR;
    static readonly Color SelectedColor = new Color(1f, 0.93f, 0.25f, 1f);

    void Awake()
    {
        originalPosition = transform.position;
        bottleSR = GetComponent<SpriteRenderer>();
        originalBottleOrder = bottleSR.sortingOrder;
        originalMaskOrder = bottleMaskSR.sortingOrder;
        CreateGlow();
    }

    // Tạo 1 bản sao sprite viền, to hơn một chút, đặt phía sau để làm hiệu ứng "sáng viền"
    void CreateGlow()
    {
        GameObject glow = new GameObject("SelectGlow");
        glow.transform.SetParent(transform, false);
        glow.transform.localScale = Vector3.one * 1f;

        glowSR = glow.AddComponent<SpriteRenderer>();
        glowSR.sprite = bottleSR.sprite;
        glowSR.color = new Color(1f, 0.93f, 0.25f, 0.45f);
        glowSR.sortingLayerID = bottleSR.sortingLayerID;
        glow.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        bottleSR.color = selected ? SelectedColor : Color.white;

        if (selected)
        {
            glowSR.sortingOrder = Mathf.Min(bottleSR.sortingOrder, bottleMaskSR.sortingOrder) - 1;
        }
        glowSR.gameObject.SetActive(selected);
    }

    // GameController gọi hàm này để random màu / reset ống
    public void Init(Color[] colors, int count)
    {
        // Dừng mọi animation đang chạy và đưa ống về trạng thái ban đầu (cần cho nút Reset giữa chừng)
        StopAllCoroutines();
        transform.position = originalPosition;
        transform.eulerAngles = Vector3.zero;
        bottleSR.sortingOrder = originalBottleOrder;
        bottleMaskSR.sortingOrder = originalMaskOrder;
        if (lineRenderer != null) lineRenderer.enabled = false;

        bottleColors = new Color[4];
        for (int i = 0; i < 4; i++)
        {
            bottleColors[i] = i < colors.Length ? colors[i] : Color.clear;
        }

        numberOfColorsInBottle = count;
        isLocked = false;
        SetSelected(false);

        bottleMaskSR.material.SetFloat("_FillAmount", fillAmounts[numberOfColorsInBottle]);
        bottleMaskSR.material.SetFloat("_SARM", ScaleAndRontationMultiplierCurve.Evaluate(0));
        UpdateColorsOnShader();
        UpdateTopColorValues();
    }

    public bool IsComplete()
    {
        return numberOfColorsInBottle == 4
            && bottleColors[0].Equals(bottleColors[1])
            && bottleColors[1].Equals(bottleColors[2])
            && bottleColors[2].Equals(bottleColors[3]);
    }

    // Vị trí miệng ống (để bắn confetti)
    public Vector3 GetMouthPosition()
    {
        Bounds b = bottleSR.bounds;
        return new Vector3(b.center.x, b.max.y, transform.position.z);
    }
    // ==================

    public void StartColorTransfer()
    {
        ChosenRotationPointAndDiretion();

        numberOfColorsToTransfer = Mathf.Min(numberOfTopColorLayers, 4 - BottleControllerRef.numberOfColorsInBottle);

        for (int i = 0; i < numberOfColorsToTransfer; i++)
        {
            BottleControllerRef.bottleColors[BottleControllerRef.numberOfColorsInBottle + i] = topColor;
        }
        BottleControllerRef.UpdateColorsOnShader();

        CalculateRotationIndex(4 - BottleControllerRef.numberOfColorsInBottle);

        bottleSR.sortingOrder += 2;
        bottleMaskSR.sortingOrder += 2;

        StartCoroutine(MoveBottle());
    }

    IEnumerator MoveBottle()
    {
        StartPosition = transform.position;
        if (chosenRotationPoint == leftRotationPoint)
        {
            endPosition = BottleControllerRef.rightRotationPoint.position;
        }
        else
        {
            endPosition = BottleControllerRef.leftRotationPoint.position;
        }

        float t = 0;

        while (t <= 1)
        {
            transform.position = Vector3.Lerp(StartPosition, endPosition, t);
            t += Time.deltaTime * 2;

            yield return new WaitForEndOfFrame();
        }

        transform.position = endPosition;

        StartCoroutine(RotateBottle());
    }

    IEnumerator MoveBottleBack()
    {
        StartPosition = transform.position;
        endPosition = originalPosition;

        float t = 0;

        while (t <= 1)
        {
            transform.position = Vector3.Lerp(StartPosition, endPosition, t);
            t += Time.deltaTime * 2;

            yield return new WaitForEndOfFrame();
        }

        transform.position = endPosition;

        // FIX: bản gốc là "+= 2" nên sortingOrder cứ tăng mãi
        bottleSR.sortingOrder -= 2;
        bottleMaskSR.sortingOrder -= 2;

        // Báo cho GameController: animation xong -> mở khóa input, kiểm tra ống hoàn thành / thắng
        if (GameController.Instance != null)
        {
            GameController.Instance.OnTransferFinished(this, BottleControllerRef);
        }
    }

    void UpdateColorsOnShader()
    {
        bottleMaskSR.material.SetColor("_C1", bottleColors[0]);
        bottleMaskSR.material.SetColor("_C2", bottleColors[1]);
        bottleMaskSR.material.SetColor("_C3", bottleColors[2]);
        bottleMaskSR.material.SetColor("_C4", bottleColors[3]);
    }

    IEnumerator RotateBottle()
    {
        float t = 0;
        float lerpValue;
        float angleValue;

        float lastAngleValue = 0;

        while (t < timeToRotate)
        {
            lerpValue = t / timeToRotate;
            angleValue = Mathf.Lerp(0.0f, directionMultiplier * rotationValues[rotationIndex], lerpValue);

            transform.RotateAround(chosenRotationPoint.position, Vector3.forward, lastAngleValue - angleValue);

            bottleMaskSR.material.SetFloat("_SARM", ScaleAndRontationMultiplierCurve.Evaluate(angleValue));

            if (fillAmounts[numberOfColorsInBottle] > FillAmountCurve.Evaluate(angleValue) + 0.005f)
            {
                if (lineRenderer.enabled == false)
                {
                    lineRenderer.startColor = topColor;
                    lineRenderer.endColor = topColor;

                    lineRenderer.SetPosition(0, chosenRotationPoint.position);
                    lineRenderer.SetPosition(1, chosenRotationPoint.position - Vector3.up * 1.45f);

                    lineRenderer.enabled = true;

                    // SFX rót nước: phát đúng lúc dòng nước bắt đầu chảy
                    if (GameController.Instance != null) GameController.Instance.PlayPour();
                }

                bottleMaskSR.material.SetFloat("_FillAmount", FillAmountCurve.Evaluate(angleValue));

                BottleControllerRef.FillUp(FillAmountCurve.Evaluate(lastAngleValue) - FillAmountCurve.Evaluate(angleValue));
            }

            t += Time.deltaTime * RotationSpeedMultiplier.Evaluate(angleValue);

            lastAngleValue = angleValue;

            yield return new WaitForEndOfFrame();
        }
        angleValue = directionMultiplier * rotationValues[rotationIndex];

        bottleMaskSR.material.SetFloat("_SARM", ScaleAndRontationMultiplierCurve.Evaluate(angleValue));
        bottleMaskSR.material.SetFloat("_FillAmount", FillAmountCurve.Evaluate(angleValue));

        numberOfColorsInBottle -= numberOfColorsToTransfer;
        BottleControllerRef.numberOfColorsInBottle += numberOfColorsToTransfer;

        lineRenderer.enabled = false;

        StartCoroutine(RotateBottleBack());
    }

    IEnumerator RotateBottleBack()
    {
        float t = 0;
        float lerpValue;
        float angleValue;

        float lastAngleValue = directionMultiplier * rotationValues[rotationIndex];

        while (t < timeToRotate)
        {
            lerpValue = t / timeToRotate;
            angleValue = Mathf.Lerp(directionMultiplier * rotationValues[rotationIndex], 0.0f, lerpValue);

            transform.RotateAround(chosenRotationPoint.position, Vector3.forward, lastAngleValue - angleValue);

            bottleMaskSR.material.SetFloat("_SARM", ScaleAndRontationMultiplierCurve.Evaluate(angleValue));

            lastAngleValue = angleValue;

            t += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }
        UpdateTopColorValues();
        angleValue = 0;
        transform.eulerAngles = new Vector3(0, 0, angleValue);
        bottleMaskSR.material.SetFloat("_SARM", ScaleAndRontationMultiplierCurve.Evaluate(angleValue));

        StartCoroutine(MoveBottleBack());
    }

    public void UpdateTopColorValues()
    {
        if (numberOfColorsInBottle != 0)
        {
            numberOfTopColorLayers = 1;

            topColor = bottleColors[numberOfColorsInBottle - 1];

            if (numberOfColorsInBottle == 4)
            {
                if (bottleColors[3].Equals(bottleColors[2]))
                {
                    numberOfTopColorLayers = 2;
                    if (bottleColors[2].Equals(bottleColors[1]))
                    {
                        numberOfTopColorLayers = 3;
                        if (bottleColors[1].Equals(bottleColors[0]))
                        {
                            numberOfTopColorLayers = 4;
                        }
                    }
                }
            }
            else if (numberOfColorsInBottle == 3)
            {
                if (bottleColors[2].Equals(bottleColors[1]))
                {
                    numberOfTopColorLayers = 2;
                    if (bottleColors[1].Equals(bottleColors[0]))
                    {
                        numberOfTopColorLayers = 3;
                    }
                }
            }
            else if (numberOfColorsInBottle == 2)
            {
                if (bottleColors[1].Equals(bottleColors[0]))
                {
                    numberOfTopColorLayers = 2;
                }
            }

            rotationIndex = 3 - (numberOfColorsInBottle - numberOfTopColorLayers);
        }
    }

    public bool FillBottleCheck(Color colorToCheck)
    {
        if (numberOfColorsInBottle == 0)
        {
            return true;
        }
        else
        {
            if (numberOfColorsInBottle == 4)
            {
                return false;
            }
            else
            {
                return topColor.Equals(colorToCheck);
            }
        }
    }

    private void CalculateRotationIndex(int numberOfEmptySpacesInSecondBottle)
    {
        rotationIndex = 3 - (numberOfColorsInBottle - Mathf.Min(numberOfEmptySpacesInSecondBottle, numberOfTopColorLayers));
    }

    private void FillUp(float fillAmountToAdd)
    {
        bottleMaskSR.material.SetFloat("_FillAmount", bottleMaskSR.material.GetFloat("_FillAmount") + fillAmountToAdd);
    }

    private void ChosenRotationPointAndDiretion()
    {
        if (transform.position.x > BottleControllerRef.transform.position.x)
        {
            chosenRotationPoint = leftRotationPoint;
            directionMultiplier = -1.0f;
        }
        else
        {
            chosenRotationPoint = rightRotationPoint;
            directionMultiplier = 1.0f;
        }
    }
}