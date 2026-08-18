using UnityEngine;
using System.Collections;

public class CubeSpawner : MonoBehaviour
{
    [Header("Hand Settings")]
    public Transform hand;
    public float handPushDistance = 0.3f;
    public float handPushDuration = 0.2f;

    [Header("References")]
    public DiceManager diceManager;

    // Убрали Update! Теперь игрок не может бросить кости в обход правил GameManager.

    public IEnumerator ThrowWithHand()
    {
        // Анимация руки
        if (hand != null)
            yield return StartCoroutine(PushHand());

        // Создание и бросок кубиков
        diceManager.SpawnAndThrow(transform);
    }

    IEnumerator PushHand()
    {
        Vector3 startPos = hand.localPosition;
        Vector3 endPos = startPos + transform.forward * handPushDistance;

        float elapsed = 0f;
        while (elapsed < handPushDuration)
        {
            hand.localPosition = Vector3.Lerp(startPos, endPos, elapsed / handPushDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < handPushDuration)
        {
            hand.localPosition = Vector3.Lerp(endPos, startPos, elapsed / handPushDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        hand.localPosition = startPos;
    }
}